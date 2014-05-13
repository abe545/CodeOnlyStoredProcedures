using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.Contracts;

namespace CodeOnlyStoredProcedure
{
    public static partial class StoredProcedureExtensions
    {
        public static TSP WithTableValuedParameter<TSP, TRow>(this TSP sp,
            string name,
            IEnumerable<TRow> table,
            string tableTypeName)
            where TSP : StoredProcedure
        {
            Contract.Requires(sp != null);
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
            Contract.Requires(table != null);
            Contract.Requires(!string.IsNullOrWhiteSpace(tableTypeName));
            Contract.Ensures(Contract.Result<TSP>() != null);

            var p = new SqlParameter
            {
                ParameterName = name,
                SqlDbType = SqlDbType.Structured,
                TypeName = "[dbo].[" + tableTypeName + "]",
                Value = table.ToTableValuedParameter(typeof(TRow))
            };

            return (TSP)sp.CloneWith(p);
        }

        public static TSP WithTableValuedParameter<TSP, TRow>(this TSP sp,
            string name,
            IEnumerable<TRow> table,
            string tableTypeSchema,
            string tableTypeName)
            where TSP : StoredProcedure
        {
            Contract.Requires(sp != null);
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
            Contract.Requires(table != null);
            Contract.Requires(!string.IsNullOrWhiteSpace(tableTypeSchema));
            Contract.Requires(!string.IsNullOrWhiteSpace(tableTypeName));
            Contract.Ensures(Contract.Result<TSP>() != null);

            var p = new SqlParameter
            {
                ParameterName = name,
                SqlDbType = SqlDbType.Structured,
                TypeName = string.Format("[{0}].[{1}]", tableTypeSchema, tableTypeName),
                Value = table.ToTableValuedParameter(typeof(TRow))
            };

            return (TSP)sp.CloneWith(p);
        }
    }
}
