using System;
using System.Data;
using System.Diagnostics.Contracts;

namespace CodeOnlyStoredProcedure
{
    public static partial class StoredProcedureExtensions
    {
        /// <summary>
        /// Clones the given <see cref="StoredProcedure"/> with an output parameter.
        /// </summary>
        /// <typeparam name="TSP">The type of StoredProcedure to add an output parameter to.</typeparam>
        /// <typeparam name="TValue">The type of output parameter.</typeparam>
        /// <param name="sp">The <see cref="StoredProcedure"/> to add an output parameter to.</param>
        /// <param name="name">The name of the output parameter.</param>
        /// <param name="setter">The setter to call when the stored procedure returns the parameter.</param>
        /// <param name="size">The size expected for the Sql data type. This can normally be omitted.</param>
        /// <param name="scale">The scale expected for the Sql data type. This can normally be omitted.</param>
        /// <param name="precision">The precision expected for the Sql data type. This can normally be omitted.</param>
        /// <returns>A copy of the <see cref="StoredProcedure"/> with the parameter setup.</returns>
        /// <remarks>StoredProcedures are immutable, so all the Fluent API methods return copies.</remarks>
        /// <example>
        /// <code language="cs">
        /// int count;
        /// StoredProcedure.Create("usp_getWidgetCount")
        ///                .WithOutputParameter("count", i => count = i)
        ///                .Execute(db);
        /// // count will now be set to the value returned by the stored procedure in the @count parameter
        /// </code>
        /// </example>
        public static TSP WithOutputParameter<TSP, TValue>(this TSP sp,
            string         name,
            Action<TValue> setter,
            int?           size      = null,
            byte?          scale     = null,
            byte?          precision = null)
            where TSP : StoredProcedure
        {
            Contract.Requires(sp                     != null);
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
            Contract.Requires(setter                 != null);
            Contract.Ensures (Contract.Result<TSP>() != null);

            return (TSP)sp.CloneWith(new OutputParameter(name, o => setter((TValue)o), null, size, scale, precision));
        }

        /// <summary>
        /// Clones the given <see cref="StoredProcedure"/> with an output parameter.
        /// </summary>
        /// <typeparam name="TSP">The type of StoredProcedure to add an output parameter to.</typeparam>
        /// <typeparam name="TValue">The type of output parameter.</typeparam>
        /// <param name="sp">The <see cref="StoredProcedure"/> to add an output parameter to.</param>
        /// <param name="name">The name of the output parameter.</param>
        /// <param name="setter">The setter to call when the stored procedure returns the parameter.</param>
        /// <param name="dbType">The <see cref="DbType"/> that the StoredProcedure returns.</param>
        /// <param name="size">The size expected for the Sql data type. This can normally be omitted.</param>
        /// <param name="scale">The scale expected for the Sql data type. This can normally be omitted.</param>
        /// <param name="precision">The precision expected for the Sql data type. This can normally be omitted.</param>
        /// <returns>A copy of the <see cref="StoredProcedure"/> with the parameter setup.</returns>
        /// <remarks>StoredProcedures are immutable, so all the Fluent API methods return copies.</remarks>
        /// <example>
        /// <code language="cs">
        /// int count;
        /// StoredProcedure.Create("usp_getWidgetCount")
        ///                .WithOutputParameter("count", i => count = i, DbType.Int16)
        ///                .Execute(db);
        /// // count will now be set to the value returned by the stored procedure in the @count parameter
        /// </code>
        /// </example>
        public static TSP WithOutputParameter<TSP, TValue>(this TSP sp,
            string         name,
            Action<TValue> setter,
            DbType         dbType,
            int?           size      = null,
            byte?          scale     = null,
            byte?          precision = null)
            where TSP : StoredProcedure
        {
            Contract.Requires(sp                     != null);
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
            Contract.Requires(setter                 != null);
            Contract.Ensures (Contract.Result<TSP>() != null);

            return (TSP)sp.CloneWith(new OutputParameter(name, o => setter((TValue)o), dbType, size, scale, precision));
        }

        internal static TSP WithOutputParameter<TSP>(this TSP sp, string name, Action<object> setter)
            where TSP : StoredProcedure
        {
            Contract.Requires(sp                     != null);
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
            Contract.Requires(setter                 != null);
            Contract.Ensures (Contract.Result<TSP>() != null);

            return (TSP)sp.CloneWith(new OutputParameter(name, setter));
        }
    }
}
