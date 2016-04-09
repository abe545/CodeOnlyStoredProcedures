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
        private static readonly IRowFactory mainResultFactory = RowFactory<T>.Create(false);
        private static readonly IEnumerable<HierarchicalRowInfo> rowInfos;
        private        readonly ReadOnlyCollection<Type> resultTypesInOrder;

        static HierarchicalTypeRowFactory()
        {
            object falseObj = false;

            var infos = new List<HierarchicalRowInfo>();
            var types = new Queue<Type>();
            var added = new HashSet<Type>();
            types.Enqueue(typeof(T));

            var whereMethod = typeof(Enumerable).GetMethods()
                                                .Where(mi => mi.Name == "Where" && 
                                                             mi.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(Func<,>))
                                                .Single();
            var cast    = typeof(Enumerable).GetMethod("Cast");
            var toArray = typeof(Enumerable).GetMethod("ToArray");
            var toList  = typeof(Enumerable).GetMethod("ToList");

            infos.Add(new HierarchicalRowInfo(
                childType: typeof(T),
                rowFactory: RowFactory<T>.Create(false)
                ));

            while (types.Count > 0)
            {
                var t = types.Dequeue();
                if (!added.Add(t))
                    continue;

                Type implType;
                IEnumerable<PropertyInfo> interfaceProperties = null;
                if (GlobalSettings.Instance.InterfaceMap.TryGetValue(t, out implType))
                {
                    interfaceProperties = t.GetMappedProperties();
                    t = implType;
                }

                var props = t.GetMappedProperties();
                var key   = GetKeyProperty(t, props, interfaceProperties);

                foreach (var child in props)
                {
                    if (child.CanWrite && child.PropertyType.IsEnumeratedType())
                    {
                        if (key == null)
                            throw new NotSupportedException("Can not generate a hierarchy for children of type " + t.Name + " because a key could not be determined. You should decorate a property with a Key attribute to designate it as such, or mark the properties that are IEnumerables as NotMapped, to prevent this error.");

                        var childType = child.PropertyType.GetEnumeratedType();
                        types.Enqueue(childType);

                        var foreignKeyName = GetForeignKeyPropertyName(t, interfaceProperties, child);

                        implType = null;
                        if (!GlobalSettings.Instance.InterfaceMap.TryGetValue(childType, out implType))
                            implType = childType;

                        var fk = implType.GetMappedProperties(requireReadable: true).FirstOrDefault(p => p.Name == foreignKeyName);
                        if (fk == null)
                            throw new NotSupportedException("Could not find the foreign key property on " + implType.Name + ". Expected property named " + foreignKeyName + ", but was not found.");
                        else if (fk.PropertyType != key.PropertyType)
                            throw new NotSupportedException("Key types are not matched for " + implType.Name + ". Key on parent type: " + key.PropertyType + ".\nForeign key type on child: " + fk.PropertyType);

                        var childEnumerable = typeof(IEnumerable<>).MakeGenericType(childType);
                        var parent = Expression.Parameter(t);
                        var possible = Expression.Parameter(childEnumerable);
                        var keyExpr = Expression.Property(parent, key);

                        Expression children;
                        if (implType == childType)
                        {
                            // possible.Where(c => c.ParentKey == key)
                            var c = Expression.Parameter(childType);
                            var funcType = typeof(Func<,>).MakeGenericType(childType, typeof(bool));
                            children = Expression.Call(whereMethod.MakeGenericMethod(childType),
                                                       possible,
                                                       Expression.Lambda(funcType, Expression.Equal(keyExpr, Expression.Property(c, fk)), c));
                        }
                        else
                        {
                            // possible.Cast<TImpl>().Where(c => c.ParentKey == key)
                            var c = Expression.Parameter(implType);
                            var funcType = typeof(Func<,>).MakeGenericType(implType, typeof(bool));
                            children = Expression.Call(cast.MakeGenericMethod(implType), possible);
                            children = Expression.Call(whereMethod.MakeGenericMethod(implType),
                                                       children,
                                                       Expression.Lambda(funcType, Expression.Equal(keyExpr, Expression.Property(c, fk)), c));
                        }

                        if (child.PropertyType.IsArray) // .ToArray()
                            children = Expression.Call(toArray.MakeGenericMethod(childType), children);
                        else // .ToList()
                            children = Expression.Call(toList.MakeGenericMethod(childType), children);

                        var assign = Expression.Assign(Expression.Property(parent, child), children);

                        infos.Add(new HierarchicalRowInfo
                        (
                            parentType: t,
                            childType: implType,
                            childKeyColumnName: key.GetSqlColumnName(),
                            parentKeyColumnName: foreignKeyName,
                            isOptional: child.IsOptional(),
                            isArray: child.PropertyType.IsArray,
                            assigner: Expression.Lambda(assign, parent, possible).Compile(),
                            rowFactory: typeof(RowFactory<>).MakeGenericType(implType)
                                                            .GetMethod("Create", BindingFlags.Public | BindingFlags.Static)
                                                            .Invoke(null, new[] { falseObj }) as IRowFactory
                        ));
                    }
                }
            }

            rowInfos = new ReadOnlyCollection<HierarchicalRowInfo>(infos);
        }

        public HierarchicalTypeRowFactory() { }

        public HierarchicalTypeRowFactory(IEnumerable<Type> resultTypesInOrder)
        {
            Contract.Requires(resultTypesInOrder != null);

            this.resultTypesInOrder = new ReadOnlyCollection<Type>(resultTypesInOrder.ToArray());
        }

        private static PropertyInfo GetKeyProperty(Type type, IEnumerable<PropertyInfo> props, IEnumerable<PropertyInfo> interfaceProperties)
        {
            Contract.Requires(type != null);
            Contract.Requires(props != null && Contract.ForAll(props, p => p != null));

            var key = props.Where(p => p.CanRead && p.GetCustomAttributes(typeof(KeyAttribute), true).Any()).SingleOrDefault();
            if (key != null)
                return key;

            if (interfaceProperties != null)
            {
                key = interfaceProperties.Where(p => p.CanRead && p.GetCustomAttributes(typeof(KeyAttribute), true).Any()).SingleOrDefault();
                if (key != null)
                    return key;
            }

            var idWithClassName = type.Name + "Id";
            key = props.SingleOrDefault(p => p.CanRead && p.Name == "Id") ?? props.SingleOrDefault(p => p.CanRead && p.Name == idWithClassName);
            if (key != null)
                return key;

            var allInterfaceProps = type.GetInterfaces()
                                        .SelectMany(i => i.GetProperties())
                                        .Where(p => p.CanRead && p.GetCustomAttributes(typeof(KeyAttribute), true).Any())
                                        .ToArray();

            foreach (var p in allInterfaceProps)
            {
                key = props.FirstOrDefault(tp => tp.Name == p.Name && tp.CanRead);
                if (key != null)
                    return key;
            }

            return null;
        }

        private static string GetForeignKeyPropertyName(Type t, IEnumerable<PropertyInfo> interfaceProperties, PropertyInfo child)
        {
            var fkAttr = child.GetCustomAttributes(typeof(ForeignKeyAttribute), true).OfType<ForeignKeyAttribute>().FirstOrDefault();
            if (fkAttr != null)
                return fkAttr.Name;
            else if (interfaceProperties != null)
            {
                var interfaceProp = interfaceProperties.FirstOrDefault(p => p.Name == child.Name);
                fkAttr = interfaceProp.GetCustomAttributes(typeof(ForeignKeyAttribute), true).OfType<ForeignKeyAttribute>().FirstOrDefault();
                if (fkAttr != null)
                    return fkAttr.Name;
            }

            fkAttr = t.GetInterfaces()
                      .SelectMany(i => i.GetProperties())
                      .Where(p => p.Name == child.Name && p.PropertyType.IsAssignableFrom(child.PropertyType))
                      .Select(p => p.GetCustomAttributes(typeof(ForeignKeyAttribute), true).OfType<ForeignKeyAttribute>().FirstOrDefault())
                      .FirstOrDefault();
            if (fkAttr != null)
                return fkAttr.Name;

            return t.Name + "Id";
        }

        public override IEnumerable<T> ParseRows(IDataReader reader, IEnumerable<IDataTransformer> dataTransformers, CancellationToken token)
        {
            var toRead  = rowInfos.ToList();
            var results = new Dictionary<Type, IEnumerable>();
            int index   = -1;

            while (toRead.Count > 0)
            {
                index++;
                var hri = GetNextBestRowInfo(reader, toRead, token, index);

                // should this throw? I'm leaning toward no, because as long as the hierarchy is built, who cares if there
                // are extra result sets
                if (hri == null)
                    continue;
                else if (hri == HierarchicalRowInfo.Empty)
                    break;

                token.ThrowIfCancellationRequested();

                toRead.Remove(hri);
                results[hri.ChildType] = hri.RowFactory.ParseRows(reader, dataTransformers, token);
            }

            BuildHierarchy(results);

            return (IEnumerable<T>)results[typeof(T)];
        }

#if !NET40
        public override async Task<IEnumerable<T>> ParseRowsAsync(DbDataReader reader, IEnumerable<IDataTransformer> dataTransformers, CancellationToken token)
        {
            var toRead  = rowInfos.ToList();
            var results = new Dictionary<Type, IEnumerable>();
            int index   = -1;

            while (toRead.Count > 0)
            {
                index++;
                var hri = GetNextBestRowInfo(reader, toRead, token, index);

                // should this throw? I'm leaning toward no, because as long as the hierarchy is built, who cares if there
                // are extra result sets
                if (hri == null)
                    continue;
                else if (hri == HierarchicalRowInfo.Empty)
                    break;

                token.ThrowIfCancellationRequested();

                toRead.Remove(hri);
                results[hri.ChildType] = await hri.RowFactory.ParseRowsAsync(reader, dataTransformers, token);
            }

            BuildHierarchy(results);

            return (IEnumerable<T>)results[typeof(T)];
        }
#endif

        private HierarchicalRowInfo GetNextBestRowInfo(IDataReader reader, List<HierarchicalRowInfo> toRead, CancellationToken token, int index)
        {
            Contract.Requires(reader != null);
            Contract.Requires(toRead != null && Contract.ForAll(toRead, t => t != null));

            token.ThrowIfCancellationRequested();

            if (index > 0 && !reader.NextResult())
            {
                if (toRead.All(f => f.IsOptional))
                    return HierarchicalRowInfo.Empty;

                throw new StoredProcedureResultsException(typeof(T), toRead.Select(f => f.ChildType).ToArray());
            }

            token.ThrowIfCancellationRequested();
            
            if (resultTypesInOrder != null)
            {
                if (index >= resultTypesInOrder.Count)
                    return null;

                var type = resultTypesInOrder[index];
                return toRead.First(f => f.ChildType == type);
            }

            HierarchicalRowInfo result          = null;
            var                 colNames        = Enumerable.Range(0, reader.FieldCount).Select(i => reader.GetName(i)).ToArray();
            var                 fewestRemaining = Int32.MaxValue;

            foreach (var hri in toRead)
            {
                int i;
                if (hri.MatchesColumns(colNames, out i))
                {
                    if (i < fewestRemaining)
                    {
                        fewestRemaining = i;
                        result          = hri;
                    }
                }
            }

            return result;
        }

        private static void BuildHierarchy(Dictionary<Type, IEnumerable> results)
        {
            // the first one is the actual result row, so it won't have the parent/child relationship
            // defined
            foreach (var hri in rowInfos.Skip(1))
            {
                IEnumerable children;

                var parents  = results[hri.ParentType];
                if (!results.TryGetValue(hri.ChildType, out children))
                {
                    if (hri.IsOptional)
                    {
                        object empty;
                        
                        if (hri.IsArray)
                            empty = Array.CreateInstance(hri.ChildType, 0);
                        else // this needs to be an empty generic list
                            empty = Activator.CreateInstance(typeof(List<>).MakeGenericType(hri.ChildType));

                        foreach (var o in parents)
                            hri.Assigner.DynamicInvoke(o, empty);
                    }
                }
                else
                {
                    foreach (var o in parents)
                        hri.Assigner.DynamicInvoke(o, children);
                }
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

        private class HierarchicalRowInfo
        {
            public static readonly HierarchicalRowInfo Empty = new HierarchicalRowInfo();

            public Type        ParentType          { get; }
            public Type        ChildType           { get; }
            public string      ParentKeyColumnName { get; }
            public string      ChildKeyColumnName  { get; }
            public IRowFactory RowFactory          { get; }
            public Delegate    Assigner            { get; }
            public bool        IsOptional          { get; }
            public bool        IsArray             { get; }

            public HierarchicalRowInfo(
                Type        parentType          = null,
                Type        childType           = null,
                string      parentKeyColumnName = null,
                string      childKeyColumnName  = null,
                IRowFactory rowFactory          = null,
                Delegate    assigner            = null,
                bool        isOptional          = false,
                bool        isArray             = false)
            {
                ParentType          = parentType;
                ChildType           = childType;
                ParentKeyColumnName = parentKeyColumnName;
                ChildKeyColumnName  = childKeyColumnName;
                RowFactory          = rowFactory;
                Assigner            = assigner;
                IsOptional          = isOptional;
                IsArray             = isArray;
            }

            public bool MatchesColumns(IEnumerable<string> columnNames, out int matchedColumnCount)
            {
                //matchedColumnCount = 0;
                //if (!string.IsNullOrEmpty(ParentKeyColumnName) && !columnNames.Contains(ParentKeyColumnName))
                //    return false;
                //if (!string.IsNullOrEmpty(ChildKeyColumnName) && !columnNames.Contains(ChildKeyColumnName))
                //    return false;

                return RowFactory.MatchesColumns(columnNames, out matchedColumnCount);
            }
        }
    }
}
