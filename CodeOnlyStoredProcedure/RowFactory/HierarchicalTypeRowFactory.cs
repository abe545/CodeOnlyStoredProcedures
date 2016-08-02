using System;
using System.Collections;
using System.Collections.Concurrent;
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
        private static readonly MethodInfo toArray = typeof(Enumerable).GetMethod(nameof(Enumerable.ToArray));
        private static readonly MethodInfo toList  = typeof(Enumerable).GetMethod(nameof(Enumerable.ToList));
        private static readonly IRowFactory mainResultFactory = RowFactory<T>.Create(false);
        private static readonly IEnumerable<HierarchicalRowInfo> rowInfos;
        private static readonly Type resultType;
        private        readonly ReadOnlyCollection<Type> resultTypesInOrder;

        static HierarchicalTypeRowFactory()
        {
            object falseObj = false;

            if (!GlobalSettings.Instance.InterfaceMap.TryGetValue(typeof(T), out resultType))
                resultType = typeof(T);

            var infos = new List<HierarchicalRowInfo>();
            var types = new Queue<Tuple<Type, HierarchicalRowInfo>>();
            var added = new HashSet<Type>();
            var info  = Activator.CreateInstance(typeof(RootHierarchicalRowInfo<>).MakeGenericType(typeof(T), resultType)) as HierarchicalRowInfo;

            types.Enqueue(Tuple.Create(typeof(T), info));
            infos.Add(info);

            while (types.Count > 0)
            {
                var tuple = types.Dequeue();
                var t = tuple.Item1;
                info = tuple.Item2;

                if (!added.Add(t))
                    continue;

                IEnumerable<PropertyInfo> interfaceProperties = null;
                Type implType;
                if (GlobalSettings.Instance.InterfaceMap.TryGetValue(t, out implType))
                {
                    interfaceProperties = t.GetMappedProperties();
                    t = implType;
                }

                // get all the properties, not just the writeable ones, that way the key property can be a calculated key
                var props = t.GetMappedProperties();
                var key   = GetKeyProperty(t, props, interfaceProperties);

                foreach (var child in props)
                {
                    if (child.CanWrite && child.PropertyType.IsEnumeratedType())
                    {
                        if (key == null)
                            throw new NotSupportedException("Can not generate a hierarchy for children of type " + t.Name + " because a key could not be determined. You should decorate a property with a Key attribute to designate it as such, or mark the properties that are IEnumerables as NotMapped, to prevent this error.");

                        var childType = child.PropertyType.GetEnumeratedType();
                        var foreignKeyName = GetForeignKeyPropertyName(t, interfaceProperties, child);

                        implType = null;
                        if (!GlobalSettings.Instance.InterfaceMap.TryGetValue(childType, out implType))
                            implType = childType;

                        var fk = implType.GetMappedProperties(requireReadable: true).FirstOrDefault(p => p.Name == foreignKeyName);
                        if (fk == null)
                            throw new NotSupportedException("Could not find the foreign key property on " + implType.Name + ". Expected property named " + foreignKeyName + ", but was not found.");
                        else if (fk.PropertyType != key.PropertyType)
                            throw new NotSupportedException("Key types are not matched for " + implType.Name + ". Key on parent type: " + key.PropertyType + ".\nForeign key type on child: " + fk.PropertyType);

                        var hriType = typeof(HierarchicalRowInfo<,,>).MakeGenericType(typeof(T), t, implType, key.PropertyType);
                        var childInfo = Activator.CreateInstance(hriType, key, fk, child, info) as HierarchicalRowInfo;
                        types.Enqueue(Tuple.Create(childType, childInfo));
                        infos.Add(childInfo);
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
            var results = new ResultHolder();

            foreach (var t in ParseRows(reader, token, rf => rf.ParseRows(reader, dataTransformers, token)))
                results.AddResults(t.Item1.ItemType, t.Item2);

            return BuildHierarchy(results);
        }

#if !NET40
        public override async Task<IEnumerable<T>> ParseRowsAsync(DbDataReader reader, IEnumerable<IDataTransformer> dataTransformers, CancellationToken token)
        {
            var results = new ResultHolder();

            foreach (var t in ParseRows(reader, token, rf => rf.ParseRowsAsync(reader, dataTransformers, token)))
                results.AddResults(t.Item1.ItemType, await t.Item2);

            return BuildHierarchy(results);
        }
#endif

        private IEnumerable<Tuple<HierarchicalRowInfo, TParseResult>> ParseRows<TParseResult>(
            IDataReader reader, 
            CancellationToken token,
            Func<IRowFactory, TParseResult> parser)
        {
            var toRead = rowInfos.ToList();
            int index = -1;

            while (toRead.Count > 0)
            {
                index++;
                var hri = GetNextBestRowInfo(reader, toRead, token, index);

                // should this throw? I'm leaning toward no, because as long as the hierarchy is built, who cares if there
                // are extra result sets
                if (hri == null)
                    continue;

                // this value means that there are no more required result sets
                if (hri == HierarchicalRowInfo.Empty)
                    break;

                token.ThrowIfCancellationRequested();
                
                toRead.Remove(hri);
                yield return Tuple.Create(hri, parser(hri.RowFactory));
            }
        }

        private HierarchicalRowInfo GetNextBestRowInfo(IDataReader reader, List<HierarchicalRowInfo> toRead, CancellationToken token, int index)
        {
            Contract.Requires(reader != null);
            Contract.Requires(toRead != null && Contract.ForAll(toRead, t => t != null));

            token.ThrowIfCancellationRequested();

            if (index > 0 && !reader.NextResult())
            {
                if (toRead.Any(f => f.ShouldThrowIfNotFound(toRead)))
                    throw new StoredProcedureResultsException(typeof(T), toRead.Select(f => f.ItemType).ToArray());

                return HierarchicalRowInfo.Empty;
            }

            token.ThrowIfCancellationRequested();
            
            if (resultTypesInOrder != null)
            {
                if (index >= resultTypesInOrder.Count)
                    return null;

                var type = resultTypesInOrder[index];
                return toRead.First(f => f.ItemType == type);
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

        private static IEnumerable<T> BuildHierarchy(ResultHolder results)
        {
            Contract.Requires(results != null);
            Contract.Ensures(Contract.Result<IEnumerable<T>>() != null);

            // the first one is the actual result row, so it won't have the parent/child relationship
            // defined
            foreach (var hri in rowInfos.Skip(1))
                hri.AssignItemsToParents(results);

            IEnumerable<T> res;
            if (!results.TryGetValue(out res, resultType))
                throw new StoredProcedureException("Unexpected error trying to retrieve the results for the hierarchy with root type " + typeof(T));

            return res;
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
            private       readonly HierarchicalRowInfo parent;
            
            public Type        ItemType            { get; }
            public string      ParentKeyColumnName { get; }
            public string      ItemKeyColumnName   { get; }
            public IRowFactory RowFactory          { get; }
            public bool        IsArray             { get; }
            public bool        IsOptional          { get; }

            public HierarchicalRowInfo(
                HierarchicalRowInfo parent              = null,
                Type                itemType            = null,
                string              parentKeyColumnName = null,
                string              itemKeyColumnName   = null,
                IRowFactory         rowFactory          = null,
                bool                isOptional          = false,
                bool                isArray             = false)
            {
                this.parent         = parent;
                ItemType            = itemType;
                ParentKeyColumnName = parentKeyColumnName;
                ItemKeyColumnName   = itemKeyColumnName;
                RowFactory          = rowFactory;
                IsOptional          = isOptional;
                IsArray             = isArray;
            }

            public bool MatchesColumns(IEnumerable<string> columnNames, out int unMatchedColumnCount)
            {
                var success = RowFactory.MatchesColumns(columnNames, out unMatchedColumnCount);

                if (!string.IsNullOrEmpty(ParentKeyColumnName) && columnNames.Contains(ParentKeyColumnName))
                    --unMatchedColumnCount;
                if (!string.IsNullOrEmpty(ItemKeyColumnName) && columnNames.Contains(ItemKeyColumnName))
                    --unMatchedColumnCount;

                return success;
            }

            public bool ShouldThrowIfNotFound(IEnumerable<HierarchicalRowInfo> remaining)
            {
                if (IsOptional)
                    return false;

                if (parent != null && parent.IsOptional)
                    return !remaining.Contains(parent);

                return true;
            }

            public virtual void AssignItemsToParents(ResultHolder results)
            {
                // the base class shouldn't be called, as only the top level item uses the non-generic type,
                // and it is skipped when assigning the hierarchies
            }
        }

        private class RootHierarchicalRowInfo<TItem> : HierarchicalRowInfo
        {
            public RootHierarchicalRowInfo() : base(itemType: typeof(TItem), rowFactory: RowFactory<TItem>.Create(false)) { }
        }

        private class HierarchicalRowInfo<TParent, TItem, TKey> : HierarchicalRowInfo
        {
            private readonly Action<TParent, IEnumerable<TItem>> assigner;
            private readonly Func<TItem, TKey>                   keyFunc;
            private readonly Func<TParent, TKey>                 foreignKeyFunc;

            public HierarchicalRowInfo(
                PropertyInfo        parentKeyProperty,
                PropertyInfo        childParentKeyProperty,
                PropertyInfo        parentChildrenProperty,
                HierarchicalRowInfo parent)
                : base(parent:              parent,
                       itemType:            typeof(TItem),
                       parentKeyColumnName: parentKeyProperty.GetSqlColumnName(),
                       itemKeyColumnName:   childParentKeyProperty.GetSqlColumnName(),
                       rowFactory:          RowFactory<TItem>.Create(false),
                       isOptional:          parentChildrenProperty.IsOptional(),
                       isArray:             parentChildrenProperty.PropertyType.IsArray)
            {
                keyFunc        = childParentKeyProperty.CompileGetter<TItem,   TKey>();
                foreignKeyFunc = parentKeyProperty     .CompileGetter<TParent, TKey>();

                var value = Expression.Parameter(typeof(IEnumerable<TItem>), "value");
                Expression toSet = value;

                if (parentChildrenProperty.PropertyType.IsArray)
                    toSet = Expression.Call(toArray.MakeGenericMethod(typeof(TItem)), toSet);
                else
                    toSet = Expression.Call(toList.MakeGenericMethod(typeof(TItem)), toSet);

                var p = Expression.Parameter(typeof(TParent), "p");
                var assign = Expression.Assign(Expression.Property(p, parentChildrenProperty), toSet);
                assigner = Expression.Lambda<Action<TParent, IEnumerable<TItem>>>(assign, p, value).Compile();
            }

            public override void AssignItemsToParents(ResultHolder results)
            {
                IEnumerable<TParent> parents;
                IEnumerable<TItem> toSet = null;

                if (results.TryGetValue(out parents))
                {
                    if (!results.TryGetValue(out toSet))
                    {
                        if (IsOptional)
                        {
                            if (IsArray)
                                toSet = new TItem[0];
                            else // this needs to be an empty generic list
                                toSet = new List<TItem>();
                        }
                    }
                }

                if (toSet != null)
                {
                    if (toSet.Any())
                    {
                        var lookup = toSet.ToLookup(keyFunc);
                        foreach (TParent p in parents)
                        {
                            var key = foreignKeyFunc(p);
                            var items = lookup[key];
                            assigner(p, items);
                        }
                    }
                    else
                    {
                        foreach (TParent p in parents)
                            assigner(p, toSet);
                    }
                }
            }
        }

        private class ResultHolder
        {
            private readonly ConcurrentDictionary<Type, List<IEnumerable>> resultMap = new ConcurrentDictionary<Type, List<IEnumerable>>();
            
            public void AddResults(Type t, IEnumerable results)
            {
                resultMap.GetOrAdd(t, _ => new List<IEnumerable>())
                         .Add(results);
            }

            public bool TryGetValue<TRes>(out IEnumerable<TRes> results, Type keyType = null)
            {
                List<IEnumerable> res;
                if (resultMap.TryGetValue(keyType ?? typeof(TRes), out res))
                {
                    if (res.Count == 1)
                        results = (IEnumerable<TRes>)res[0];
                    else
                        results = YieldResults<TRes>(res);

                    return true;
                }

                results = null;
                return false;
            }

            private IEnumerable<TRes> YieldResults<TRes>(List<IEnumerable> res)
            {
                foreach (IEnumerable<TRes> i in res)
                    foreach (TRes j in i)
                        yield return j;
            }
        }
    }
}
