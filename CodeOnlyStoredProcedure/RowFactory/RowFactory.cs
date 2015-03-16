using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Common;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CodeOnlyStoredProcedure.RowFactory;

namespace CodeOnlyStoredProcedure
{
    internal abstract class RowFactory<T> : IRowFactory<T>
    {
        private Func<IDataReader, T> parser;

        public static IRowFactory<T> Create()
        {
            return Create(true);
        }

        private static IRowFactory<T> Create(bool generateHierarchicals)
        {
            var itemType = typeof(T);
            
            if (itemType == typeof(object))
                return new ExpandoObjectRowFactory();

            if (itemType.IsGenericType && itemType.GetGenericTypeDefinition() == typeof(Nullable<>))
                itemType = itemType.GetGenericArguments()[0];

            if (itemType.IsEnum)
                return new EnumRowFactory();
            if (itemType.IsSimpleType())
                return new SimpleTypeRowFactory();
            if (generateHierarchicals && itemType.GetMappedProperties().Any(p => p.PropertyType.IsEnumeratedType()))
                return new HiearchicalTypeRowFactory();
            
            return new ComplexTypeRowFactory();
        }

        public Type RowType { get { return typeof(T); } }

        protected abstract Func<IDataReader, T> CreateRowFactory(IDataReader reader, IEnumerable<IDataTransformer> xFormers);

        public virtual bool MatchesColumns(IEnumerable<string> columnNames, out int leftoverColumns)
        {
            leftoverColumns = Math.Max(0, columnNames.Count() - 1);
            return columnNames.Any();
        }

        public virtual IEnumerable<T> ParseRows(IDataReader reader, IEnumerable<IDataTransformer> dataTransformers, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            if (parser == null)
                parser = CreateRowFactory(reader, dataTransformers);

            var res = new List<T>();
            while (reader.Read())
            {
                token.ThrowIfCancellationRequested();
                res.Add(parser(reader));
            }

            return res;
        }

#if !NET40
        public virtual async Task<IEnumerable<T>> ParseRowsAsync(DbDataReader reader, IEnumerable<IDataTransformer> dataTransformers, CancellationToken token)
        {
            if (parser == null)
                parser = CreateRowFactory(reader, dataTransformers);

            var res = new List<T>();
            while (await reader.ReadAsync(token))
            {
                token.ThrowIfCancellationRequested();
                res.Add(parser(reader));
            }

            return res;
        }

        async Task<IEnumerable> IRowFactory.ParseRowsAsync(DbDataReader reader, IEnumerable<IDataTransformer> transformers, CancellationToken token)
        {
            return await ParseRowsAsync(reader, transformers, token);
        }
#endif

        IEnumerable IRowFactory.ParseRows(IDataReader reader, IEnumerable<IDataTransformer> transformers, CancellationToken token)
        {
            return ParseRows(reader, transformers, token);
        }

        private class SimpleTypeRowFactory : RowFactory<T>
        {
            static readonly ParameterExpression dataReaderExpression = Expression.Parameter(typeof(IDataReader));
            static readonly ValueAccessorFactory<T> accessor = 
                new ValueAccessorFactory<T>(dataReaderExpression, Expression.Constant(0), null, null);

            protected override Func<IDataReader, T> CreateRowFactory(IDataReader reader, IEnumerable<IDataTransformer> xFormers)
            {
                return Expression.Lambda<Func<IDataReader, T>>(accessor.CreateExpressionToGetValueFromReader(reader, xFormers, reader.GetFieldType(0)),
                                                               dataReaderExpression)
                                 .Compile();
            }
        }

        private class EnumRowFactory : RowFactory<T>
        {
            static readonly ParameterExpression dataReaderExpression = Expression.Parameter(typeof(IDataReader));
            static readonly EnumAccessorFactory<T> accessor =
                new EnumAccessorFactory<T>(dataReaderExpression, Expression.Constant(0), null, null);

            protected override Func<IDataReader, T> CreateRowFactory(IDataReader reader, IEnumerable<IDataTransformer> xFormers)
            {
                return Expression.Lambda<Func<IDataReader, T>>(accessor.CreateExpressionToGetValueFromReader(reader, xFormers, reader.GetFieldType(0)),
                                                               dataReaderExpression)
                                 .Compile();
            }
        }

        private class ComplexTypeRowFactory : RowFactory<T>
        {
            private static readonly Type                                                          resultType           = typeof(T);
            private static readonly ParameterExpression                                           dataReaderExpression = Expression.Parameter(typeof(IDataReader));
            private static readonly ParameterExpression                                           indexExpression      = Expression.Variable (typeof(int));
            private static readonly IEnumerable<Tuple<string, PropertyInfo, AccessorFactoryBase>> accessorsByColumnName;
            private static readonly Type                                                          implType;
            private static readonly ISet<string>                                                  dbColumnNames;

            static ComplexTypeRowFactory()
            {
                if (!TypeExtensions.interfaceMap.TryGetValue(resultType, out implType))
                    implType = resultType;

                var props = implType.GetResultPropertiesBySqlName();
                dbColumnNames = new HashSet<string>(props.Keys.ToArray());
                accessorsByColumnName = props.Where(kv => !kv.Value.PropertyType.IsEnumeratedType())
                                             .Select(kv =>
                                             {
                                                 var type = kv.Value.PropertyType;
                                                 if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                                                     type = type.GetGenericArguments().Single();

                                                 if (type.IsEnum)
                                                     return Tuple.Create(kv.Key, kv.Value, (AccessorFactoryBase)Activator.CreateInstance(typeof(EnumAccessorFactory<>).MakeGenericType(kv.Value.PropertyType), dataReaderExpression, indexExpression, kv.Value, kv.Key));

                                                 return Tuple.Create(kv.Key, kv.Value, (AccessorFactoryBase)Activator.CreateInstance(typeof(ValueAccessorFactory<>).MakeGenericType(kv.Value.PropertyType), dataReaderExpression, indexExpression, kv.Value, kv.Key));
                                             })
                                             .ToArray();
            }

            protected override Func<IDataReader, T> CreateRowFactory(IDataReader reader, IEnumerable<IDataTransformer> xFormers)
            {
                var exprs   = new List<Expression>();
                var row     = Expression.Variable(implType);
                var byIndex = accessorsByColumnName.Select(t => Tuple.Create(reader.GetOrdinal(t.Item1), t.Item1, t.Item2, t.Item3))
                                                   .OrderBy(t => t.Item1)
                                                   .ToArray();

                var notFound = byIndex.Where(t => t.Item1 < 0 && !t.Item3.GetCustomAttributes(false).OfType<OptionalResultAttribute>().Any());
                if (notFound.Any())
                    throw new StoredProcedureResultsException(resultType, notFound.Select(t => t.Item2).ToArray());

                exprs.Add(Expression.Assign(row, Expression.New(implType)));

                foreach (var t in byIndex)
                {
                    if (t.Item1 < 0) continue;

                    exprs.Add(Expression.Assign(indexExpression, Expression.Constant(t.Item1)));
                    exprs.Add(Expression.Assign(Expression.Property(row, t.Item3),
                                                t.Item4.CreateExpressionToGetValueFromReader(reader, xFormers, reader.GetFieldType(t.Item1))));
                }

                if (implType == resultType)
                    exprs.Add(row);
                else
                    exprs.Add(Expression.Convert(row, resultType));

                return Expression.Lambda<Func<IDataReader, T>>(
                    Expression.Block(resultType, new[] { row, indexExpression }, exprs),
                    dataReaderExpression).Compile();
            }

            public override bool MatchesColumns(IEnumerable<string> columnNames, out int leftoverColumns)
            {
                leftoverColumns = columnNames.Except(dbColumnNames).Count();
                return columnNames.All(dbColumnNames.Contains);
            }
        }

        class ExpandoObjectRowFactory : RowFactory<T>
        {
            protected override Func<IDataReader, T> CreateRowFactory(IDataReader reader, IEnumerable<IDataTransformer> xFormers)
            {
                var all = new List<Expression>();
                var rdr = Expression.Parameter(typeof(IDataReader));
                var exp = Expression.Variable(typeof(ExpandoObject));
                var add = typeof(IDictionary<string, object>).GetMethod("Add");
                var val = Expression.Variable(typeof(object[]));

                all.Add(Expression.Assign(exp, Expression.New(typeof(ExpandoObject))));
                all.Add(Expression.Assign(val, Expression.NewArrayBounds(typeof(object), Expression.Constant(reader.FieldCount))));
                all.Add(Expression.Call(rdr, typeof(IDataRecord).GetMethod("GetValues"), val));

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var v = Expression.ArrayIndex(val, Expression.Constant(i));
                    all.Add(Expression.Call(exp, add, Expression.Constant(reader.GetName(i)), v));
                }

                all.Add(Expression.Convert(exp, typeof(T)));

                return Expression.Lambda<Func<IDataReader, T>>(Expression.Block(typeof(T), new[] { exp, val }, all.ToArray()), rdr)
                                 .Compile();
            }

            public override bool MatchesColumns(IEnumerable<string> columnNames, out int leftoverColumns)
            {
                leftoverColumns = 0;
                return true;
            }
        }

        private class HiearchicalTypeRowFactory : RowFactory<T>
        {
            private static readonly IEnumerable<IRowFactory> rowFactories;
            private static readonly IEnumerable<Tuple<Type, Type, Delegate>> childAssigners;

            static HiearchicalTypeRowFactory()
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
                var toList = typeof(Enumerable).GetMethod("ToList");

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

                            var childType = child.PropertyType.GetGenericArguments()[0]; 
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
                                                      .Invoke(null, new [] { falseObj }) as IRowFactory);
                }

                rowFactories = new ReadOnlyCollection<IRowFactory>(factories);
                childAssigners = new ReadOnlyCollection<Tuple<Type, Type, Delegate>>(assigners);
            }

            private static PropertyInfo GetKeyProperty(string className, IEnumerable<PropertyInfo> props)
            {
                var exp = props.Where(p => Attribute.GetCustomAttribute(p, typeof(KeyAttribute)) != null).SingleOrDefault();
                if (exp != null)
                    return exp;

                var id = props.SingleOrDefault(p => p.Name == "Id");
                if (id != null)
                    return id;

                var idName = className + "Id";
                return props.SingleOrDefault(p => p.Name == idName);
            }

            public override IEnumerable<T> ParseRows(IDataReader reader, IEnumerable<IDataTransformer> dataTransformers, CancellationToken token)
            {
                var toRead = rowFactories.ToList();
                var results = new Dictionary<Type, IEnumerable>();
                var first = true;

                while (toRead.Count > 0)
                {
                    token.ThrowIfCancellationRequested();

                    if (first)
                        first = false;
                    else if (!reader.NextResult())
                        throw new StoredProcedureResultsException(typeof(T), toRead.Select(f => f.RowType).ToArray());

                    token.ThrowIfCancellationRequested();

                    var factory = GetBestFactory(reader, toRead);

                    // should this throw? probably.
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
                var toRead = rowFactories.ToList();
                var results = new Dictionary<Type, IEnumerable>();
                var first = true;

                while (toRead.Count > 0)
                {
                    token.ThrowIfCancellationRequested();

                    if (first)
                        first = false;
                    else if (!await reader.NextResultAsync())
                        throw new StoredProcedureResultsException(typeof(T), toRead.Select(f => f.RowType).ToArray());

                    token.ThrowIfCancellationRequested();

                    var factory = GetBestFactory(reader, toRead);

                    // should this throw? probably.
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

            private static IRowFactory GetBestFactory(IDataReader reader, List<IRowFactory> toRead)
            {
                var colNames = Enumerable.Range(0, reader.FieldCount).Select(i => reader.GetName(i)).ToArray();
                IRowFactory factory = null;
                int fewestRemaining = Int32.MaxValue;
                foreach (var f in toRead)
                {
                    int i;
                    if (f.MatchesColumns(colNames, out i))
                    {
                        if (i < fewestRemaining)
                        {
                            fewestRemaining = i;
                            factory = f;
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

            protected override Func<IDataReader, T> CreateRowFactory(IDataReader reader, IEnumerable<IDataTransformer> xFormers)
            {
                throw new NotSupportedException("How did this code execute?");
            }
        }
    }
}
