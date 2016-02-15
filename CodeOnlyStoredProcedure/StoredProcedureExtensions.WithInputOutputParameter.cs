using System;
using System.Data;
using System.Diagnostics.Contracts;

namespace CodeOnlyStoredProcedure
{
    public static partial class StoredProcedureExtensions
    {
        /// <summary>
        /// Clones the given <see cref="StoredProcedure"/> with an input/output parameter.
        /// </summary>
        /// <typeparam name="TSP">The type of StoredProcedure to add an input/output parameter to.</typeparam>
        /// <typeparam name="TValue">The type of output parameter.</typeparam>
        /// <param name="sp">The <see cref="StoredProcedure"/> to add an output parameter to.</param>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="value">The initial value of the parameter.</param>
        /// <param name="setter">The setter to call when the stored procedure returns the parameter.</param>
        /// <param name="size">The size expected for the Sql data type. This can normally be omitted.</param>
        /// <param name="scale">The scale expected for the Sql data type. This can normally be omitted.</param>
        /// <param name="precision">The precision expected for the Sql data type. This can normally be omitted.</param>
        /// <returns>A clone of the <see cref="StoredProcedure"/> with the parameter setup.</returns>
        /// <remarks>StoredProcedures are immutable, so all the Fluent API methods return copies.</remarks>
        /// <example>
        /// <code language="cs">
        /// int count;
        /// StoredProcedure.Create("usp_updateWidgetCount")
        ///                .WithInputOutputParameter("count", 4, i => count = i)
        ///                .Execute(db);
        /// // count will now be set to the value returned by the stored procedure in the @count parameter
        /// </code>
        /// </example>
        public static TSP WithInputOutputParameter<TSP, TValue>(this TSP sp,
            string         name,
            TValue         value,
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

            return sp.WithInputOutputParameter(name, value, setter, typeof(TValue).InferDbType(), size, scale, precision);
        }

        /// <summary>
        /// Clones the given <see cref="StoredProcedure"/> with an input/output parameter.
        /// </summary>
        /// <typeparam name="TSP">The type of StoredProcedure to add an input/output parameter to.</typeparam>
        /// <typeparam name="TValue">The type of output parameter.</typeparam>
        /// <param name="sp">The <see cref="StoredProcedure"/> to add an output parameter to.</param>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="value">The initial value of the parameter.</param>
        /// <param name="setter">The setter to call when the stored procedure returns the parameter.</param>
        /// <param name="dbType">The <see cref="DbType"/> that the StoredProcedure expects.</param>
        /// <param name="size">The size expected for the Sql data type. This can normally be omitted.</param>
        /// <param name="scale">The scale expected for the Sql data type. This can normally be omitted.</param>
        /// <param name="precision">The precision expected for the Sql data type. This can normally be omitted.</param>
        /// <returns>A clone of the <see cref="StoredProcedure"/> with the parameter setup.</returns>
        /// <remarks>StoredProcedures are immutable, so all the Fluent API methods return copies.</remarks>
        /// <example>
        /// <code language="cs">
        /// int count;
        /// StoredProcedure.Create("usp_updateWidgetCount")
        ///                .WithInputOutputParameter("count", 4, i => count = i, DbType.Int16)
        ///                .Execute(db);
        /// // count will now be set to the value returned by the stored procedure in the @count parameter
        /// </code>
        /// </example>
        public static TSP WithInputOutputParameter<TSP, TValue>(this TSP sp,
            string         name,
            TValue         value,
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

            return (TSP)sp.CloneWith(new InputOutputParameter(name, o => setter((TValue)o), value, dbType, size, scale, precision));
        }
    }
}
