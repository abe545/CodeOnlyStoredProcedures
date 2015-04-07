using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Common;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace CodeOnlyStoredProcedure.RowFactory
{
    internal class HierarchicalTypeRowFactory<T> : RowFactory<T>
    {
        private static readonly IEnumerable<IRowFactory> rowFactories;
        private static readonly IEnumerable<Tuple<Type, Type, Delegate>> childAssigners;

        static HierarchicalTypeRowFactory()
        {
            var factories = new List<IRowFactory>();
            object falseObj = false;

            var assigners = new List<Tuple<Type, Type, Delegate>>();
            var types = new Queue<Type>();
            var added = new HashSet<Type>();
            types.Enqueue(typeof(T));

            var whereMethod = typeof(Enumerable).GetMethods()
                                                .Where(mi => mi.Name == "Where" && 
                                                             mi.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(Func<,>))
                                                .Single();
            var toArray = typeof(Enumerable).GetMethod("ToArray");
            var toList  = typeof(Enumerable).GetMethod("ToList");

            while (types.Count > 0)
            {
                var t = types.Dequeue();
                if (!added.Add(t))
                    continue;

                var props = t.GetMappedProperties();
                var key = GetKeyProperty(t.Name, props);

                foreach (var child in props)
                {
                    if (child.PropertyType.IsEnumeratedType())
                    {
                        if (key == null)
                            throw new NotSupportedException("Can not generate a hierarchy for children of type " + t.Name + " because a key could not be determined. You should decorate a property with a Key attribute to designate it as such, or mark the properties that are IEnumerables as NotMapped, to prevent this error.");

                        var childType = child.PropertyType.GetEnumeratedType();
                        types.Enqueue(childType);

                        var foreignKeyName = t.Name + "Id";
                        var fkAttr = (ForeignKeyAttribute)Attribute.GetCustomAttribute(child, typeof(ForeignKeyAttribute));
                        if (fkAttr != null)
                            foreignKeyName = fkAttr.Name;

                        var fk = childType.GetMappedProperties().FirstOrDefault(p => p.Name == foreignKeyName);
                        if (fk == null)
                            throw new NotSupportedException("Could not find the foreign key property on " + childType.Name + ". Expected property named " + foreignKeyName + ", but was not found.");
                        else if (fk.PropertyType != key.PropertyType)
                            throw new NotSupportedException("Key types are not matched for " + childType.Name + ". Key on parent type: " + key.PropertyType + ".\nForeign key type on child: " + fk.PropertyType);

                        var childEnumerable = typeof(IEnumerable<>).MakeGenericType(childType);
                        var parent = Expression.Parameter(t);
                        var possible = Expression.Parameter(childEnumerable);
                        var keyExpr = Expression.Property(parent, key);

                        // possible.Where(c => c.ParentKey == key)
                        var c = Expression.Parameter(childType);
                        var funcType = typeof(Func<,>).MakeGenericType(childType, typeof(bool));
                        var children = Expression.Call(whereMethod.MakeGenericMethod(childType),
                                                        possible,
                                                        Expression.Lambda(funcType, Expression.Equal(keyExpr, Expression.Property(c, fk)), c));
                        if (child.PropertyType.IsArray) // .ToArray()
                            children = Expression.Call(toArray.MakeGenericMethod(childType), children);
                        else // .ToList()
                            children = Expression.Call(toList.MakeGenericMethod(childType), children);

                        var assign = Expression.Assign(Expression.Property(parent, child), children);
                        assigners.Add(Tuple.Create(t, childType, Expression.Lambda(assign, parent, possible).Compile()));
                    }
                }

                factories.Add(typeof(RowFactory<>).MakeGenericType(t)
                                                  .GetMethod("Create", BindingFlags.NonPublic | BindingFlags.Static)
                                                  .Invoke(null, new [] { falseObj })
                              as IRowFactory);
            }

            rowFactories   = new ReadOnlyCollection<IRowFactory>(factories);
            childAssigners = new ReadOnlyCollection<Tuple<Type, Type, Delegate>>(assigners);
        }

        private static PropertyInfo GetKeyProperty(string className, IEnumerable<PropertyInfo> props)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(className));
            Contract.Requires(props != null && Contract.ForAll(props, p => p != null));

            var explicitKey = props.Where(p => Attribute.GetCustomAttribute(p, typeof(KeyAttribute)) != null).SingleOrDefault();
            if (explicitKey != null)
                return explicitKey;

            var idWithClassName = className + "Id";
            return props.SingleOrDefault(p => p.Name == "Id") ?? props.SingleOrDefault(p => p.Name == idWithClassName);
        }

        public override IEnumerable<T> ParseRows(IDataReader reader, IEnumerable<IDataTransformer> dataTransformers, CancellationToken token)
        {
            var toRead  = rowFactories.ToList();
            var results = new Dictionary<Type, IEnumerable>();
            var first   = true;

            while (toRead.Count > 0)
            {
                var factory = GetNextBestFactory(reader, toRead, token, ref first);

                // should this throw? I'm leaning toward no, because as long as the hierarchy is built, who cares if there
                // are extra result sets
                if (factory == null)
                    continue;

                token.ThrowIfCancellationRequested();

                toRead.Remove(factory);
                results[factory.RowType] = factory.ParseRows(reader, dataTransformers, token);
            }

            BuildHierarchy(results);

            return (IEnumerable<T>)results[typeof(T)];
        }

#if !NET40
        public override async Task<IEnumerable<T>> ParseRowsAsync(DbDataReader reader, IEnumerable<IDataTransformer> dataTransformers, CancellationToken token)
        {
            var toRead  = rowFactories.ToList();
            var results = new Dictionary<Type, IEnumerable>();
            var first   = true;

            while (toRead.Count > 0)
            {
                var factory = GetNextBestFactory(reader, toRead, token, ref first);

                // should this throw? I'm leaning toward no, because as long as the hierarchy is built, who cares if there
                // are extra result sets
                if (factory == null)
                    continue;

                token.ThrowIfCancellationRequested();

                toRead.Remove(factory);
                results[factory.RowType] = await factory.ParseRowsAsync(reader, dataTransformers, token);
            }

            BuildHierarchy(results);

            return (IEnumerable<T>)results[typeof(T)];
        }
#endif

        private static IRowFactory GetNextBestFactory(IDataReader reader, List<IRowFactory> toRead, CancellationToken token, ref bool isFirst)
        {
            Contract.Requires(reader != null);
            Contract.Requires(toRead != null && Contract.ForAll(toRead, f => f != null));

            token.ThrowIfCancellationRequested();

            if (isFirst)
                isFirst = false;
            else if (!reader.NextResult())
                throw new StoredProcedureResultsException(typeof(T), toRead.Select(f => f.RowType).ToArray());

            token.ThrowIfCancellationRequested();

            IRowFactory factory         = null;
            var         colNames        = Enumerable.Range(0, reader.FieldCount).Select(i => reader.GetName(i)).ToArray();
            int         fewestRemaining = Int32.MaxValue;

            foreach (var f in toRead)
            {
                int i;
                if (f.MatchesColumns(colNames, out i))
                {
                    if (i < fewestRemaining)
                    {
                        fewestRemaining = i;
                        factory         = f;
                    }
                }
            }

            return factory;
        }

        private static void BuildHierarchy(Dictionary<Type, IEnumerable> results)
        {
            foreach (var tuple in childAssigners)
            {
                var parents  = results[tuple.Item1];
                var children = results[tuple.Item2];

                foreach (var o in parents)
                    tuple.Item3.DynamicInvoke(o, children);
            }
        }

        public override bool MatchesColumns(IEnumerable<string> columnNames, out int leftoverColumns)
        {
            // this code can only be executed through reflection, since the factory won't call it. 
            throw new NotSupportedException("MatchesColumns should not be called for an HiearchicalTypeRowFactory.");
        }

        protected override Func<IDataReader, T> CreateRowFactory(IDataReader reader, IEnumerable<IDataTransformer> xFormers)
        {
            // this code can only be executed through reflection, since the factory won't call it. 
            throw new NotSupportedException("CreateRowFactory should not be called for an HiearchicalTypeRowFactory.");
        }
    }
}
