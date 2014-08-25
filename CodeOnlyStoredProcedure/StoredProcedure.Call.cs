using System.Data;
using System.Diagnostics.Contracts;
using System.Threading;

namespace CodeOnlyStoredProcedure
{
    public partial class StoredProcedure
    {
        /// <summary>
        /// Calls a StoredProcedure using a dynamic syntax
        /// </summary>
        /// <param name="connection">The IDbConnection to use to execute the Stored Procedure.</param>
        /// <param name="timeout">The amount of time in seconds before the stored procedure will be aborted.</param>
        /// <returns>A dynamic object that represents the results from the StoredProcedure execution.</returns>
        /// <remarks>All parameters must be named. However, they can be marked as ref (InputOutput SQL Parameter) or
        /// out (Output SQL Parameter).</remarks>
        /// <example>Since this uses a dynamic syntax, you can execute StoredProcedures with a much cleaner style:
        ///     IEnumerable&lt;Person&gt; people = StoredProcedure.Call(connection)
        ///                                                       .data_schema // if omitted, defaults to dbo
        ///                                                       .usp_GetPeople&lt;Person&gt;(minimumAge: 20);
        /// </example>
        public static dynamic Call(IDbConnection connection, int timeout = 30)
        {
            Contract.Requires(connection != null);
            Contract.Ensures (Contract.Result<object>() != null);

            return new DynamicStoredProcedure(connection, false, CancellationToken.None, timeout);
        }

        /// <summary>
        /// Calls a StoredProcedure using a dynamic syntax asynchronously.
        /// </summary>
        /// <param name="connection">The IDbConnection to use to execute the Stored Procedure.</param>
        /// <param name="timeout">The amount of time in seconds before the stored procedure will be aborted.</param>
        /// <returns>A dynamic object that represents the results from the StoredProcedure execution.</returns>
        /// <remarks>All parameters must be named. However, they can be marked as ref (InputOutput SQL Parameter) or
        /// out (Output SQL Parameter).</remarks>
        /// <example>Since this uses a dynamic syntax, you can execute StoredProcedures with a much cleaner style:
        ///     IEnumerable&lt;Person&gt; people = StoredProcedure.CallAsync(connection)
        ///                                                       .data_schema // if omitted, defaults to dbo
        ///                                                       .usp_GetPeople&lt;Person&gt;(minimumAge: 20);
        /// </example>
        public static dynamic CallAsync(IDbConnection connection, int timeout = 30)
        {
            Contract.Requires(connection != null);
            Contract.Ensures(Contract.Result<object>() != null);

            return new DynamicStoredProcedure(connection, true, CancellationToken.None, timeout);
        }

        /// <summary>
        /// Calls a StoredProcedure using a dynamic syntax asynchronously.
        /// </summary>
        /// <param name="connection">The IDbConnection to use to execute the Stored Procedure.</param>
        /// <param name="token">The <see cref="CancellationToken"/> to use to cancel the execution of the Stored Procedure.</param>
        /// <param name="timeout">The amount of time in seconds before the stored procedure will be aborted.</param>
        /// <returns>A dynamic object that represents the results from the StoredProcedure execution.</returns>
        /// <remarks>All parameters must be named. However, they can be marked as ref (InputOutput SQL Parameter) or
        /// out (Output SQL Parameter).</remarks>
        /// <example>Since this uses a dynamic syntax, you can execute StoredProcedures with a much cleaner style:
        ///     IEnumerable&lt;Person&gt; people = StoredProcedure.CallAsync(connection, token)
        ///                                                       .data_schema // if omitted, defaults to dbo
        ///                                                       .usp_GetPeople&lt;Person&gt;(minimumAge: 20);
        /// </example>
        public static dynamic CallAsync(IDbConnection connection, CancellationToken token, int timeout = 30)
        {
            Contract.Requires(connection != null);
            Contract.Ensures(Contract.Result<object>() != null);

            return new DynamicStoredProcedure(connection, true, token, timeout);
        }
    }
}
