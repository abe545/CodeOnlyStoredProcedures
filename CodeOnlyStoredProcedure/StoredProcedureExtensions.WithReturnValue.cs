using System;
using System.Data;
using System.Diagnostics.Contracts;

namespace CodeOnlyStoredProcedure
{
    public static partial class StoredProcedureExtensions
    {
        /// <summary>
        /// Clones the given <see cref="StoredProcedure"/> with an action to be called with the return value after execution finishes.
        /// </summary>
        /// <typeparam name="TSP">The <see cref="StoredProcedure"/> type.</typeparam>
        /// <param name="sp">The <see cref="StoredProcedure"/> to register for the return value.</param>
        /// <param name="returnValue">The <see href="http://msdn.microsoft.com/en-us/library/018hxwa8.aspx" alt="Action"/> to call with the return value.</param>
        /// <returns>A copy of the <see cref="StoredProcedure"/> with the return value associated.</returns>
        /// <remarks>StoredProcedures are immutable, so all the Fluent API methods return copies.</remarks>
        /// <example>
        /// <code language="cs">
        /// int returnValue;
        /// StoredProcedure.Create("usp_getWidgetCount")
        ///                .WithReturnValue(i => returnValue = i)
        ///                .Execute(db);
        /// // returnValue will now be set to the value returned by the stored procedure
        /// </code>
        /// </example>
        public static TSP WithReturnValue<TSP>(this TSP sp, Action<int> returnValue)
            where TSP : StoredProcedure
        {
            Contract.Requires(sp != null);
            Contract.Requires(returnValue != null);
            Contract.Ensures(Contract.Result<TSP>() != null);

            return (TSP)sp.CloneWith(new ReturnValueParameter(returnValue));
        }
    }
}
