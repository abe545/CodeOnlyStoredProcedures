using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using CodeOnlyStoredProcedure.RowFactory;

namespace CodeOnlyStoredProcedure
{
    [ContractClass(typeof(RowFactoryContract<>))]
    internal abstract class RowFactory<T> : IRowFactory<T>
    {
        protected static readonly ParameterExpression dataReaderExpression = Expression.Parameter(typeof(IDataReader));
        private Func<IDataReader, T> parser;

        public static IRowFactory<T> Create(bool generateHierarchicals = true)
        {
            Contract.Ensures(Contract.Result<IRowFactory<T>>() != null);

            var itemType = typeof(T);
            
            if (itemType == typeof(object))
                return new ExpandoObjectRowFactory<T>();

            if (itemType.IsGenericType && itemType.GetGenericTypeDefinition() == typeof(Nullable<>))
                itemType = itemType.GetGenericArguments().Single();

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
            // default behavior is that if there are any columns, this factory will work, but will only parse one of them
            // SimpleTypeRowFactory, and EnumRowFactory follow this convention. Other RowFactory implementations will have
            // more involved logic for determining what columns are leftover, and if these column names are enough to 
            // satisfy all required columns of the objects they create.
            leftoverColumns = Math.Max(0, columnNames.Count() - 1);
            return columnNames.Any();
        }

        public virtual IEnumerable<T> ParseRows(IDataReader reader, IEnumerable<IDataTransformer> dataTransformers, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            if (parser == null)
                parser = CreateRowFactory(reader, GlobalSettings.Instance.DataTransformers.Concat(dataTransformers));

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
                parser = CreateRowFactory(reader, GlobalSettings.Instance.DataTransformers.Concat(dataTransformers));

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

    [ContractClassFor(typeof(RowFactory<>))]
    abstract class RowFactoryContract<T> : RowFactory<T>
    {
        protected override Func<IDataReader, T> CreateRowFactory(IDataReader reader, IEnumerable<IDataTransformer> xFormers)
        {
            Contract.Requires(reader   != null);
            Contract.Requires(xFormers != null && Contract.ForAll(xFormers, x => x != null));
            Contract.Ensures (Contract.Result<Func<IDataReader, T>>() != null);

            return null;
        }
    }

}
