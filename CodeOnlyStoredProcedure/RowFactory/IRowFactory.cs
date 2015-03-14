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
        IEnumerable ParseRows(IDataReader reader, IEnumerable<IDataTransformer> DataTransformers, CancellationToken token);
    }

    internal interface IRowFactory<T> : IRowFactory
    {
        new IEnumerable<T> ParseRows(IDataReader reader, IEnumerable<IDataTransformer> DataTransformers, CancellationToken token);
#if !NET40
        Task<IEnumerable<T>> ParseRowsAsync(DbDataReader reader, IEnumerable<IDataTransformer> DataTransformers, CancellationToken token);
#endif
    }
}
