using System.Collections.Generic;

namespace CodeOnlyStoredProcedure
{
    internal interface IRowFactory
    {
        IEnumerable<string> UnfoundPropertyNames { get; }
        object CreateRow(string[] fieldNames, object[] values, IEnumerable<IDataTransformer> transformers);
    }
}
