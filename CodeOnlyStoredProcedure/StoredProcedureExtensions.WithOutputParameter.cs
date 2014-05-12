using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.Contracts;

namespace CodeOnlyStoredProcedure
{
    public static partial class StoredProcedureExtensions
    {
        public static TSP WithOutputParameter<TSP, TValue>(this TSP sp,
            string name,
            Action<TValue> setter,
            int? size = null,
            byte? scale = null,
            byte? precision = null)
            where TSP : StoredProcedure
        {
            Contract.Requires(sp != null);
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
            Contract.Requires(setter != null);
            Contract.Ensures(Contract.Result<TSP>() != null);

            return (TSP)sp.CloneWith(
                new SqlParameter
                {
                    ParameterName = name,
                    Direction = ParameterDirection.Output
                }.AddPrecisison(size, scale, precision),
                o => setter((TValue)o));
        }

        public static TSP WithOutputParameter<TSP, TValue>(this TSP sp,
            string name,
            Action<TValue> setter,
            SqlDbType dbType,
            int? size = null,
            byte? scale = null,
            byte? precision = null)
            where TSP : StoredProcedure
        {
            Contract.Requires(sp != null);
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
            Contract.Requires(setter != null);
            Contract.Ensures(Contract.Result<TSP>() != null);

            return (TSP)sp.CloneWith(
                new SqlParameter
                {
                    ParameterName = name,
                    Direction = ParameterDirection.Output,
                    SqlDbType = dbType
                }.AddPrecisison(size, scale, precision),
                o => setter((TValue)o));
        }
    }
}
