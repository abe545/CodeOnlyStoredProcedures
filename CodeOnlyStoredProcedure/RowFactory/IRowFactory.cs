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
    /// <summary>
    /// Base interface that all row factories implement in order to be easily referenced without a generic parameter.
    /// </summary>
    /// <remarks>If you're advanced enough to want to create your own IRowFactory implementation, you should actually implement
    /// <see cref="IRowFactory{T}"/>, since you'll get to return a <see cref="IEnumerable{T}"/> from <see cref="ParseRows"/>.</remarks>
    [ContractClass(typeof(IRowFactoryContract))]
    public interface IRowFactory
    {
        /// <summary>
        /// The type of the rows this factory generates. 
        /// </summary>
        Type RowType { get; }

        /// <summary>
        /// Determines if this row factory can generate a row with the given column names, and how many of the column names are unused,
        /// if all required column names are in the set.
        /// </summary>
        /// <param name="columnNames">The column names returned from the database.</param>
        /// <param name="leftoverColumns">The number of column names that are not used from the result set. This value is used to break ties
        /// when building hierarchical result sets, and the order of results hasn't been specified by the programmer.</param>
        /// <returns>True if all required columns are in columnNames; false otherwise.</returns>
        bool MatchesColumns(IEnumerable<string> columnNames, out int leftoverColumns);

        /// <summary>
        /// Parses the results from the database into concrete rows.
        /// </summary>
        /// <param name="reader">The <see cref="IDataReader"/> that is used to communicate with the database.</param>
        /// <param name="dataTransformers">The <see cref="IDataTransformer"/>s to use to transform columns.</param>
        /// <param name="token">The <see cref="CancellationToken"/> that can be used to cancel the Stored Procedure's execution.</param>
        /// <returns>The rows returned from the database.</returns>
        IEnumerable ParseRows(IDataReader reader, IEnumerable<IDataTransformer> dataTransformers, CancellationToken token);

#if !NET40
        /// <summary>
        /// Parses the results from the database into concrete rows asynchronously.
        /// </summary>
        /// <param name="reader">The <see cref="IDataReader"/> that is used to communicate with the database.</param>
        /// <param name="dataTransformers">The <see cref="IDataTransformer"/>s to use to transform columns.</param>
        /// <param name="token">The <see cref="CancellationToken"/> that can be used to cancel the Stored Procedure's execution.</param>
        /// <returns>A <see cref="Task"/>, that when complete, will contain the rows returned from the database.</returns>
        Task<IEnumerable> ParseRowsAsync(DbDataReader reader, IEnumerable<IDataTransformer> dataTransformers, CancellationToken token);
#endif
    }

    /// <summary>
    /// A row factory is used to generate concrete models from a database.
    /// </summary>
    /// <typeparam name="T">The type of rows to create.</typeparam>
    [ContractClass(typeof(IRowFactoryContract<>))]
    public interface IRowFactory<T> : IRowFactory
    {
        /// <summary>
        /// Parses the results from the database into concrete rows.
        /// </summary>
        /// <param name="reader">The <see cref="IDataReader"/> that is used to communicate with the database.</param>
        /// <param name="dataTransformers">The <see cref="IDataTransformer"/>s to use to transform columns.</param>
        /// <param name="token">The <see cref="CancellationToken"/> that can be used to cancel the Stored Procedure's execution.</param>
        /// <returns>The rows returned from the database.</returns>
        new IEnumerable<T> ParseRows(IDataReader reader, IEnumerable<IDataTransformer> dataTransformers, CancellationToken token);

#if !NET40
        /// <summary>
        /// Parses the results from the database into concrete rows asynchronously.
        /// </summary>
        /// <param name="reader">The <see cref="IDataReader"/> that is used to communicate with the database.</param>
        /// <param name="dataTransformers">The <see cref="IDataTransformer"/>s to use to transform columns.</param>
        /// <param name="token">The <see cref="CancellationToken"/> that can be used to cancel the Stored Procedure's execution.</param>
        /// <returns>A <see cref="Task"/>, that when complete, will contain the rows returned from the database.</returns>
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
