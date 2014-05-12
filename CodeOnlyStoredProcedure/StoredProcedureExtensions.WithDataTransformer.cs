using System.Diagnostics.Contracts;

namespace CodeOnlyStoredProcedure
{
    public static partial class StoredProcedureExtensions
    {
        public static TSP WithDataTransformer<TSP>(this TSP sp, IDataTransformer transformer)
            where TSP : StoredProcedure
        {
            Contract.Requires(sp != null);
            Contract.Requires(transformer != null);
            Contract.Ensures(Contract.Result<TSP>() != null);

            return (TSP)sp.CloneWith(transformer);
        }
    }
}
