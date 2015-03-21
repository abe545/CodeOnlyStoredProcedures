using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using CodeOnlyStoredProcedure.RowFactory;

namespace CodeOnlyStoredProcedure
{
    internal abstract class RowFactory<T> : IRowFactory<T>
    {
        protected static readonly ParameterExpression dataReaderExpression = Expression.Parameter(typeof(IDataReader));
        private Func<IDataReader, T> parser;

        public static IRowFactory<T> Create()
        {
            return Create(true);
        }

        private static IRowFactory<T> Create(bool generateHierarchicals)
        {
            var itemType = typeof(T);
            
            if (itemType == typeof(object))
                return new ExpandoObjectRowFactory<T>();

            if (itemType.IsGenericType && itemType.GetGenericTypeDefinition() == typeof(Nullable<>))
                itemType = itemType.GetGenericArguments()[0];

            if (itemType.IsEnum)
                return new EnumRowFactory<T>();
            if (itemType.IsSimpleType())
                return new SimpleTypeRowFactory<T>();
            if (generateHierarchicals && itemType.GetMappedProperties().Any(p => p.PropertyType.IsEnumeratedType()))
                return new HierarchicalTypeRowFactory<T>();
            
            return new ComplexTypeRowFactory<T>();
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

        IEnumerable IRowFactory.ParseRows(IDataReader reader, IEnumerable<IDataTransformer> transformers, CancellationToken token)
        {
            return ParseRows(reader, transformers, token);
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
    }
}
