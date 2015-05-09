using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.Contracts;
using System.Linq;

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
        /// <remarks>StoredProcedures are immutable, so all the Fluent API methods return copies.</remarks>
        /// <example>
        /// <code language="cs">
        /// public void AddPeople(IDbConnection conn, IEnumerable&lt;Person&gt; people)
        /// {
        ///     StoredProcedure.Create("usp_getWidgetCount")
        ///                    .WithTableValuedParameter("people", people, "PersonInput")
        ///                    .Execute(conn);
        /// }
        /// </code>
        /// </example>
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

            return (TSP)sp.CloneWith(new TableValuedParameter(name, table, typeof(TRow), tableTypeName));
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
        /// <remarks>StoredProcedures are immutable, so all the Fluent API methods return copies.</remarks>
        /// <example>
        /// <code language="cs">
        /// public void AddPeople(IDbConnection conn, IEnumerable&lt;Person&gt; people)
        /// {
        ///     StoredProcedure.Create("usp_getWidgetCount")
        ///                    .WithTableValuedParameter("people", people, "tvp", "Person")
        ///                    .Execute(conn);
        /// }
        /// </code>
        /// </example>
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
            
            return (TSP)sp.CloneWith(new TableValuedParameter(name, table, typeof(TRow), tableTypeName, tableTypeSchema));
        }
    }
}
