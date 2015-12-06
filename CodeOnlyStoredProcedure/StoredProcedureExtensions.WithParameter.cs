using System.Data;
using System.Diagnostics.Contracts;

namespace CodeOnlyStoredProcedure
{
    public static partial class StoredProcedureExtensions
    {
        /// <summary>
        /// Clones the given <see cref="StoredProcedure"/> with the given input parameter.
        /// </summary>
        /// <typeparam name="TSP">The type of StoredProcedure. Can be a StoredProcedure with or without results.</typeparam>
        /// <typeparam name="TValue">The type of value to pass.</typeparam>
        /// <param name="sp">The StoredProcedure to add the input parameter to</param>
        /// <param name="name">The name that the StoredProcedure expects (without the @).</param>
        /// <param name="value">The value to pass.</param>
        /// <returns>A copy of the StoredProcedure with the input parameter passed.</returns>
        /// <remarks>StoredProcedures are immutable, so all the Fluent API methods return copies.</remarks>
        /// <example>
        /// <code language="cs">
        /// var sp = StoredProcedure.Create("usp_incrementWidgetCount")
        ///                         .WithParameter("widgetCount", 2);
        /// </code>
        /// </example>
        public static TSP WithParameter<TSP, TValue>(this TSP sp, string name, TValue value)
            where TSP : StoredProcedure
        {
            Contract.Requires(sp != null);
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
            Contract.Ensures(Contract.Result<TSP>() != null);

            return (TSP)sp.CloneWith(new InputParameter(name, value, typeof(TValue).InferDbType()));
        }

        /// <summary>
        /// Clones the given <see cref="StoredProcedure"/> with the given input parameter.
        /// </summary>
        /// <typeparam name="TSP">The type of StoredProcedure. Can be a StoredProcedure with or without results.</typeparam>
        /// <typeparam name="TValue">The type of value to pass.</typeparam>
        /// <param name="sp">The StoredProcedure to add the input parameter to</param>
        /// <param name="name">The name that the StoredProcedure expects (without the @).</param>
        /// <param name="value">The value to pass.</param>
        /// <param name="dbType">The <see cref="DbType"/> that the StoredProcedure expects.</param>
        /// <returns>A copy of the StoredProcedure with the input parameter passed.</returns>
        /// <remarks>StoredProcedures are immutable, so all the Fluent API methods return copies.</remarks>
        /// <example>
        /// <code language="cs">
        /// var sp = StoredProcedure.Create("usp_incrementWidgetCount")
        ///                         .WithParameter("widgetCount", -1, DbType.Int16);
        /// </code>
        /// </example>
        public static TSP WithParameter<TSP, TValue>(this TSP sp, string name, TValue value, DbType dbType)
            where TSP : StoredProcedure
        {
            Contract.Requires(sp != null);
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
            Contract.Ensures(Contract.Result<TSP>() != null);

            return (TSP)sp.CloneWith(new InputParameter(name, value, dbType));
        }
    }
}
