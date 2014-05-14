using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.Contracts;

namespace CodeOnlyStoredProcedure
{
    public static partial class StoredProcedureExtensions
    {
        /// <summary>
        /// Clones the given <see cref="StoredProcedure"/>, and associates the <typeparamref name="TRow"/> items
        /// as a Table Valued Parameter.
        /// </summary>
        /// <typeparam name="TSP">The type of <see cref="StoredProcedure"/> to associate the Table Valued Parameter with.</typeparam>
        /// <typeparam name="TRow">The type of object to pass in the Table Valued Parameter.</typeparam>
        /// <param name="sp">The <see cref="StoredProcedure"/> to clone.</param>
        /// <param name="name">The name of the Table Valued Parameter in the stored procedure.</param>
        /// <param name="table">The items to pass in the Table Valued Parameter.</param>
        /// <param name="tableTypeName">The name of the table that the database's stored procedure expects
        /// in its Table Valued Parameter.</param>
        /// <returns>A copy of the <see cref="StoredProcedure"/> that has the Table Valued Parameter set.</returns>
        public static TSP WithTableValuedParameter<TSP, TRow>(this TSP sp,
            string            name,
            IEnumerable<TRow> table,
            string            tableTypeName)
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

        /// <summary>
        /// Clones the given <see cref="StoredProcedure"/>, and associates the <typeparamref name="TRow"/> items
        /// as a Table Valued Parameter.
        /// </summary>
        /// <typeparam name="TSP">The type of <see cref="StoredProcedure"/> to associate the Table Valued Parameter with.</typeparam>
        /// <typeparam name="TRow">The type of object to pass in the Table Valued Parameter.</typeparam>
        /// <param name="sp">The <see cref="StoredProcedure"/> to clone.</param>
        /// <param name="name">The name of the Table Valued Parameter in the stored procedure.</param>
        /// <param name="table">The items to pass in the Table Valued Parameter.</param>
        /// <param name="tableTypeSchema">The schema of the table that the database's stored procedure expects
        /// in its Table Valued Parameter.</param>
        /// <param name="tableTypeName">The name of the table that the database's stored procedure expects
        /// in its Table Valued Parameter.</param>
        /// <returns>A copy of the <see cref="StoredProcedure"/> that has the Table Valued Parameter set.</returns>
        public static TSP WithTableValuedParameter<TSP, TRow>(this TSP sp,
            string            name,
            IEnumerable<TRow> table,
            string            tableTypeSchema,
            string            tableTypeName)
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
