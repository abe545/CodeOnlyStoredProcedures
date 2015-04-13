using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace CodeOnlyStoredProcedure
{
    [ContractClass(typeof(IRowFactoryContract))]
    public interface IRowFactory
    {
        Type RowType { get; }
        bool MatchesColumns(IEnumerable<string> columnNames, out int leftoverColumns);
        IEnumerable ParseRows(IDataReader reader, IEnumerable<IDataTransformer> dataTransformers, CancellationToken token);
#if !NET40
        Task<IEnumerable> ParseRowsAsync(DbDataReader reader, IEnumerable<IDataTransformer> dataTransformers, CancellationToken token);
#endif
    }

    [ContractClass(typeof(IRowFactoryContract<>))]
    public interface IRowFactory<T> : IRowFactory
    {
        new IEnumerable<T> ParseRows(IDataReader reader, IEnumerable<IDataTransformer> dataTransformers, CancellationToken token);
#if !NET40
        new Task<IEnumerable<T>> ParseRowsAsync(DbDataReader reader, IEnumerable<IDataTransformer> dataTransformers, CancellationToken token);
#endif
    }

    [ContractClassFor(typeof(IRowFactory))]
    abstract class IRowFactoryContract : IRowFactory
    {
        Type IRowFactory.RowType
        {
            get
            {
                Contract.Ensures(Contract.Result<Type>() != null);
                return null;
            }
        }

        bool IRowFactory.MatchesColumns(IEnumerable<string> columnNames, out int leftoverColumns)
        {
            Contract.Requires(columnNames != null && Contract.ForAll(columnNames, s => !string.IsNullOrWhiteSpace(s)));
            leftoverColumns = 0;
            return false;
        }

        IEnumerable IRowFactory.ParseRows(IDataReader reader, IEnumerable<IDataTransformer> dataTransformers, CancellationToken token)
        {
            Contract.Requires(reader                         != null);
            Contract.Requires(dataTransformers               != null);
            Contract.Ensures (Contract.Result<IEnumerable>() != null);

            return null;
        }
#if !NET40
        Task<IEnumerable> IRowFactory.ParseRowsAsync(DbDataReader reader, IEnumerable<IDataTransformer> dataTransformers, CancellationToken token)
        {
            Contract.Requires(reader                               != null);
            Contract.Requires(dataTransformers                     != null);
            Contract.Ensures (Contract.Result<Task<IEnumerable>>() != null);

            return null;
        }
#endif
    }

    [ContractClassFor(typeof(IRowFactory<>))]
    abstract class IRowFactoryContract<T> : IRowFactory<T>
    {
        IEnumerable<T> IRowFactory<T>.ParseRows(IDataReader reader, IEnumerable<IDataTransformer> dataTransformers, CancellationToken token)
        {
            Contract.Requires(reader                            != null);
            Contract.Requires(dataTransformers                  != null);
            Contract.Ensures (Contract.Result<IEnumerable<T>>() != null);

            return null;
        }
#if !NET40
        Task<IEnumerable<T>> IRowFactory<T>.ParseRowsAsync(DbDataReader reader, IEnumerable<IDataTransformer> dataTransformers, CancellationToken token)
        {
            Contract.Requires(reader                                  != null);
            Contract.Requires(dataTransformers                        != null);
            Contract.Ensures (Contract.Result<Task<IEnumerable<T>>>() != null);

            return null;
        }

        Task<IEnumerable> IRowFactory.ParseRowsAsync(DbDataReader reader, IEnumerable<IDataTransformer> dataTransformers, CancellationToken token)
        {
            return null;
        }
#endif
        Type IRowFactory.RowType
        {
            get { return null; }
        }

        bool IRowFactory.MatchesColumns(IEnumerable<string> columnNames, out int leftoverColumns)
        {
            leftoverColumns = 0;
            return false;
        }

        IEnumerable IRowFactory.ParseRows(IDataReader reader, IEnumerable<IDataTransformer> dataTransformers, CancellationToken token)
        {
            return null;
        }
    }
}
