using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace CodeOnlyStoredProcedure
{
    internal interface IRowFactory
    {
        Type RowType { get; }
        bool MatchesColumns(IEnumerable<string> columnNames, out int leftoverColumns);
        IEnumerable ParseRows(IDataReader reader, IEnumerable<IDataTransformer> DataTransformers, CancellationToken token);
#if !NET40
        Task<IEnumerable> ParseRowsAsync(DbDataReader reader, IEnumerable<IDataTransformer> DataTransformers, CancellationToken token);
#endif
    }

    internal interface IRowFactory<T> : IRowFactory
    {
        new IEnumerable<T> ParseRows(IDataReader reader, IEnumerable<IDataTransformer> DataTransformers, CancellationToken token);
#if !NET40
        new Task<IEnumerable<T>> ParseRowsAsync(DbDataReader reader, IEnumerable<IDataTransformer> DataTransformers, CancellationToken token);
#endif
    }
}
