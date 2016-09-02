using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;
using CodeOnlyStoredProcedure.Dynamic;

namespace CodeOnlyStoredProcedure
{
    /// <summary>
    /// Contains extension methods for dynamically calling stored procedures on an IDbConnection
    /// </summary>
    public static class IDbConnectionExtensions
    {
        /// <summary>
        /// Executes a StoredProcedure using a dynamic syntax synchronously.
        /// </summary>
        /// <param name="connection">The IDbConnection to use to execute the Stored Procedure.</param>
        /// <param name="transformers">The <see cref="IDataTransformer"/>s to use to massage the data in the result set.</param>
        /// <returns>A dynamic object that represents the results from the StoredProcedure execution.</returns>
        /// <remarks>All parameters must be named. However, they can be marked as ref (InputOutput SQL Parameter) or
        /// out (Output SQL Parameter).</remarks>
        /// <example>Since this uses a dynamic syntax, you can execute StoredProcedures with a much cleaner style:
        ///     IEnumerable&lt;Person&gt; people = connection.Execute()
        ///                                                  .data_schema // if omitted, defaults to dbo
        ///                                                  .usp_GetPeople(minimumAge: 20);
        /// </example>
        public static dynamic Execute(this IDbConnection connection, params IDataTransformer[] transformers)
        {
            Contract.Requires(connection   != null);
            Contract.Requires(transformers != null && Contract.ForAll(transformers, t => t != null));
            Contract.Ensures(Contract.Result<object>() != null);

            return connection.Execute(StoredProcedure.defaultTimeout, transformers);
        }

        /// <summary>
        /// Executes a StoredProcedure using a dynamic syntax synchronously.
        /// </summary>
        /// <param name="connection">The IDbConnection to use to execute the Stored Procedure.</param>
        /// <param name="timeout">The amount of time in seconds before the stored procedure will be aborted.</param>
        /// <param name="transformers">The <see cref="IDataTransformer"/>s to use to massage the data in the result set.</param>
        /// <returns>A dynamic object that represents the results from the StoredProcedure execution.</returns>
        /// <remarks>All parameters must be named. However, they can be marked as ref (InputOutput SQL Parameter) or
        /// out (Output SQL Parameter).</remarks>
        /// <example>Since this uses a dynamic syntax, you can execute StoredProcedures with a much cleaner style:
        ///     IEnumerable&lt;Person&gt; people = connection.Execute(30)
        ///                                                  .data_schema // if omitted, defaults to dbo
        ///                                                  .usp_GetPeople(minimumAge: 20);
        /// </example>
        public static dynamic Execute(this IDbConnection connection, int timeout, params IDataTransformer[] transformers)
        {
            Contract.Requires(connection   != null);
            Contract.Requires(transformers != null && Contract.ForAll(transformers, t => t != null));
            Contract.Ensures (Contract.Result<object>() != null);

            return new DynamicStoredProcedure(connection,
                                              transformers,
                                              CancellationToken.None, 
                                              timeout, 
                                              DynamicExecutionMode.Synchronous,
                                              true);
        }

        /// <summary>
        /// Calls a StoredProcedure using a dynamic syntax asynchronously. You can await the result,
        /// or if you cast it to a Task to get the results.
        /// </summary>
        /// <param name="connection">The IDbConnection to use to execute the Stored Procedure.</param>
        /// <param name="transformers">The <see cref="IDataTransformer"/>s to use to massage the data in the result set.</param>
        /// <returns>A dynamic object that represents the results from the StoredProcedure execution.</returns>
        /// <remarks>All parameters must be named. Since this is an asynchronous call, they can not be marked as ref (InputOutput SQL Parameter) or
        /// out (Output SQL Parameter). If your stored procedure returns multiple result sets, cast the result to a <see cref="Tuple"/>
        /// whose type parameters are <see cref="IEnumerable{T}"/> values.</remarks>
        /// <example>Since this uses a dynamic syntax, you can execute StoredProcedures with a much cleaner style:
        ///     IEnumerable&lt;Person&gt; people = await connection.ExecuteAsync(token)
        ///                                                        .data_schema // if omitted, defaults to dbo
        ///                                                        .usp_GetPeople(minimumAge: 20);
        /// </example>
        public static dynamic ExecuteAsync(this IDbConnection connection, params IDataTransformer[] transformers)
        {
            Contract.Requires(connection   != null);
            Contract.Requires(transformers != null && Contract.ForAll(transformers, t => t != null));
            Contract.Ensures (Contract.Result<object>() != null);

            return connection.ExecuteAsync(CancellationToken.None, StoredProcedure.defaultTimeout, transformers);
        }

        /// <summary>
        /// Calls a StoredProcedure using a dynamic syntax asynchronously. You can await the result,
        /// or if you cast it to a Task to get the results.
        /// </summary>
        /// <param name="connection">The IDbConnection to use to execute the Stored Procedure.</param>
        /// <param name="timeout">The amount of time in seconds before the stored procedure will be aborted.</param>
        /// <param name="transformers">The <see cref="IDataTransformer"/>s to use to massage the data in the result set.</param>
        /// <returns>A dynamic object that represents the results from the StoredProcedure execution.</returns>
        /// <remarks>All parameters must be named. Since this is an asynchronous call, they can not be marked as ref (InputOutput SQL Parameter) or
        /// out (Output SQL Parameter). If your stored procedure returns multiple result sets, cast the result to a <see cref="Tuple"/>
        /// whose type parameters are <see cref="IEnumerable{T}"/> values.</remarks>
        /// <example>Since this uses a dynamic syntax, you can execute StoredProcedures with a much cleaner style:
        ///     IEnumerable&lt;Person&gt; people = await connection.ExecuteAsync(142)
        ///                                                        .data_schema // if omitted, defaults to dbo
        ///                                                        .usp_GetPeople(minimumAge: 20);
        /// </example>
        public static dynamic ExecuteAsync(this IDbConnection connection, int timeout, params IDataTransformer[] transformers)
        {
            Contract.Requires(connection   != null);
            Contract.Requires(transformers != null && Contract.ForAll(transformers, t => t != null));
            Contract.Ensures (Contract.Result<object>() != null);

            return connection.ExecuteAsync(CancellationToken.None, timeout, transformers);
        }

        /// <summary>
        /// Calls a StoredProcedure using a dynamic syntax asynchronously. You can await the result,
        /// or if you cast it to a Task to get the results.
        /// </summary>
        /// <param name="connection">The IDbConnection to use to execute the Stored Procedure.</param>
        /// <param name="token">The <see cref="CancellationToken"/> to use to cancel the execution of the Stored Procedure.</param>
        /// <param name="transformers">The <see cref="IDataTransformer"/>s to use to massage the data in the result set.</param>
        /// <returns>A dynamic object that represents the results from the StoredProcedure execution.</returns>
        /// <remarks>All parameters must be named. Since this is an asynchronous call, they can not be marked as ref (InputOutput SQL Parameter) or
        /// out (Output SQL Parameter). If your stored procedure returns multiple result sets, cast the result to a <see cref="Tuple"/>
        /// whose type parameters are <see cref="IEnumerable{T}"/> values.</remarks>
        /// <example>Since this uses a dynamic syntax, you can execute StoredProcedures with a much cleaner style:
        ///     IEnumerable&lt;Person&gt; people = await connection.ExecuteAsync(token)
        ///                                                        .data_schema // if omitted, defaults to dbo
        ///                                                        .usp_GetPeople(minimumAge: 20);
        /// </example>
        public static dynamic ExecuteAsync(this IDbConnection connection, CancellationToken token, params IDataTransformer[] transformers)
        {
            Contract.Requires(connection   != null);
            Contract.Requires(transformers != null && Contract.ForAll(transformers, t => t != null));
            Contract.Ensures (Contract.Result<object>() != null);

            return connection.ExecuteAsync(token, StoredProcedure.defaultTimeout, transformers);
        }

        /// <summary>
        /// Calls a StoredProcedure using a dynamic syntax asynchronously. You can await the result,
        /// or if you cast it to a Task to get the results.
        /// </summary>
        /// <param name="connection">The IDbConnection to use to execute the Stored Procedure.</param>
        /// <param name="token">The <see cref="CancellationToken"/> to use to cancel the execution of the Stored Procedure.</param>
        /// <param name="timeout">The amount of time in seconds before the stored procedure will be aborted.</param>
        /// <param name="transformers">The <see cref="IDataTransformer"/>s to use to massage the data in the result set.</param>
        /// <returns>A dynamic object that represents the results from the StoredProcedure execution.</returns>
        /// <remarks>All parameters must be named. Since this is an asynchronous call, they can not be marked as ref (InputOutput SQL Parameter) or
        /// out (Output SQL Parameter). If your stored procedure returns multiple result sets, cast the result to a <see cref="Tuple"/>
        /// whose type parameters are <see cref="IEnumerable{T}"/> values.</remarks>
        /// <example>Since this uses a dynamic syntax, you can execute StoredProcedures with a much cleaner style:
        ///     IEnumerable&lt;Person&gt; people = await connection.ExecuteAsync(token, 42)
        ///                                                        .data_schema // if omitted, defaults to dbo
        ///                                                        .usp_GetPeople(minimumAge: 20);
        /// </example>
        public static dynamic ExecuteAsync(this IDbConnection connection, CancellationToken token, int timeout, params IDataTransformer[] transformers)
        {
            Contract.Requires(connection   != null);
            Contract.Requires(transformers != null && Contract.ForAll(transformers, t => t != null));
            Contract.Ensures (Contract.Result<object>() != null);

            return new DynamicStoredProcedure(connection,
                                              transformers,
                                              token,
                                              timeout,
                                              DynamicExecutionMode.Asynchronous,
                                              true);
        }

        /// <summary>
        /// Calls a StoredProcedure that does not return a result set using a dynamic syntax synchronously. Since it returns no
        /// results, there will be nothing to cast to.
        /// </summary>
        /// <param name="connection">The IDbConnection to use to execute the Stored Procedure.</param>
        /// <param name="transformers">The <see cref="IDataTransformer"/>s to use to massage the data in the result set.</param>
        /// <returns>A dynamic object that can be used to easily call your stored procedure.</returns>
        /// <remarks>All parameters must be named. However, they can be marked as ref (InputOutput SQL Parameter) or
        /// out (Output SQL Parameter).</remarks>
        /// <example>Since this uses a dynamic syntax, you can execute StoredProcedures with a much cleaner style:
        ///     connection.ExecuteNonQuery()
        ///         .data_schema // if omitted, defaults to dbo
        ///         .usp_SaveAge(personId: 16, age: 34);
        /// </example>
        public static dynamic ExecuteNonQuery(this IDbConnection connection, params IDataTransformer[] transformers)
        {
            Contract.Requires(connection   != null);
            Contract.Requires(transformers != null && Contract.ForAll(transformers, t => t != null));
            Contract.Ensures (Contract.Result<object>() != null);

            return connection.ExecuteNonQuery(StoredProcedure.defaultTimeout, transformers);
        }

        /// <summary>
        /// Calls a StoredProcedure that does not return a result set using a dynamic syntax synchronously. Since it returns no
        /// results, there will be nothing to cast to.
        /// </summary>
        /// <param name="connection">The IDbConnection to use to execute the Stored Procedure.</param>
        /// <param name="timeout">The amount of time in seconds before the stored procedure will be aborted.</param>
        /// <param name="transformers">The <see cref="IDataTransformer"/>s to use to massage the data in the result set.</param>
        /// <returns>A dynamic object that can be used to easily call your stored procedure.</returns>
        /// <remarks>All parameters must be named. However, they can be marked as ref (InputOutput SQL Parameter) or
        /// out (Output SQL Parameter).</remarks>
        /// <example>Since this uses a dynamic syntax, you can execute StoredProcedures with a much cleaner style:
        ///     connection.ExecuteNonQuery(45)
        ///         .data_schema // if omitted, defaults to dbo
        ///         .usp_SaveAge(personId: 16, age: 34);
        /// </example>
        public static dynamic ExecuteNonQuery(this IDbConnection connection, int timeout, params IDataTransformer[] transformers)
        {
            Contract.Requires(connection != null);
            Contract.Requires(transformers != null && Contract.ForAll(transformers, t => t != null));
            Contract.Ensures(Contract.Result<object>() != null);

            return new DynamicStoredProcedure(connection,
                                              transformers,
                                              CancellationToken.None,
                                              timeout,
                                              DynamicExecutionMode.Synchronous,
                                              false);
        }

        /// <summary>
        /// Calls a StoredProcedure that does not return a result set using a dynamic syntax asynchronously. Since it returns no
        /// results, there will be nothing to cast to.
        /// </summary>
        /// <param name="connection">The IDbConnection to use to execute the Stored Procedure.</param>
        /// <param name="transformers">The <see cref="IDataTransformer"/>s to use to massage the data in the result set.</param>
        /// <returns>A dynamic object that can be used to easily call your stored procedure.</returns>
        /// <remarks>All parameters must be named. Since this is an asynchronous call, they can not be marked as ref
        /// (InputOutput SQL Parameter) or out (Output SQL Parameter).</remarks>
        /// <example>Since this uses a dynamic syntax, you can execute StoredProcedures with a much cleaner style:
        ///     await connection.ExecuteNonQueryAsync()
        ///         .data_schema // if omitted, defaults to dbo
        ///         .usp_SaveAge(personId: 16, age: 34);
        /// </example>
        public static dynamic ExecuteNonQueryAsync(this IDbConnection connection, params IDataTransformer[] transformers)
        {
            Contract.Requires(connection != null);
            Contract.Requires(transformers != null && Contract.ForAll(transformers, t => t != null));
            Contract.Ensures(Contract.Result<object>() != null);

            return connection.ExecuteNonQueryAsync(CancellationToken.None, StoredProcedure.defaultTimeout, transformers);
        }

        /// <summary>
        /// Calls a StoredProcedure that does not return a result set using a dynamic syntax asynchronously. Since it returns no
        /// results, there will be nothing to cast to.
        /// </summary>
        /// <param name="connection">The IDbConnection to use to execute the Stored Procedure.</param>
        /// <param name="token">The <see cref="CancellationToken"/> to use to cancel the execution of the Stored Procedure.</param>
        /// <param name="transformers">The <see cref="IDataTransformer"/>s to use to massage the data in the result set.</param>
        /// <returns>A dynamic object that can be used to easily call your stored procedure.</returns>
        /// <remarks>All parameters must be named. Since this is an asynchronous call, they can not be marked as ref
        /// (InputOutput SQL Parameter) or out (Output SQL Parameter).</remarks>
        /// <example>Since this uses a dynamic syntax, you can execute StoredProcedures with a much cleaner style:
        ///     await connection.ExecuteNonQueryAsync(token)
        ///         .data_schema // if omitted, defaults to dbo
        ///         .usp_SaveAge(personId: 16, age: 34);
        /// </example>
        public static dynamic ExecuteNonQueryAsync(this IDbConnection connection, CancellationToken token, params IDataTransformer[] transformers)
        {
            Contract.Requires(connection != null);
            Contract.Requires(transformers != null && Contract.ForAll(transformers, t => t != null));
            Contract.Ensures(Contract.Result<object>() != null);

            return connection.ExecuteNonQueryAsync(token, StoredProcedure.defaultTimeout, transformers);
        }

        /// <summary>
        /// Calls a StoredProcedure that does not return a result set using a dynamic syntax asynchronously. Since it returns no
        /// results, there will be nothing to cast to.
        /// </summary>
        /// <param name="connection">The IDbConnection to use to execute the Stored Procedure.</param>
        /// <param name="timeout">The amount of time in seconds before the stored procedure will be aborted.</param>
        /// <param name="transformers">The <see cref="IDataTransformer"/>s to use to massage the data in the result set.</param>
        /// <returns>A dynamic object that can be used to easily call your stored procedure.</returns>
        /// <remarks>All parameters must be named. Since this is an asynchronous call, they can not be marked as ref
        /// (InputOutput SQL Parameter) or out (Output SQL Parameter).</remarks>
        /// <example>Since this uses a dynamic syntax, you can execute StoredProcedures with a much cleaner style:
        ///     await connection.ExecuteNonQueryAsync(token, 30)
        ///         .data_schema // if omitted, defaults to dbo
        ///         .usp_SaveAge(personId: 16, age: 34);
        /// </example>
        public static dynamic ExecuteNonQueryAsync(this IDbConnection connection, int timeout, params IDataTransformer[] transformers)
        {
            Contract.Requires(connection != null);
            Contract.Requires(transformers != null && Contract.ForAll(transformers, t => t != null));
            Contract.Ensures(Contract.Result<object>() != null);

            return connection.ExecuteNonQueryAsync(CancellationToken.None, timeout, transformers);
        }

        /// <summary>
        /// Calls a StoredProcedure that does not return a result set using a dynamic syntax asynchronously. Since it returns no
        /// results, there will be nothing to cast to.
        /// </summary>
        /// <param name="connection">The IDbConnection to use to execute the Stored Procedure.</param>
        /// <param name="token">The <see cref="CancellationToken"/> to use to cancel the execution of the Stored Procedure.</param>
        /// <param name="timeout">The amount of time in seconds before the stored procedure will be aborted.</param>
        /// <param name="transformers">The <see cref="IDataTransformer"/>s to use to massage the data in the result set.</param>
        /// <returns>A dynamic object that can be used to easily call your stored procedure.</returns>
        /// <remarks>All parameters must be named. Since this is an asynchronous call, they can not be marked as ref
        /// (InputOutput SQL Parameter) or out (Output SQL Parameter).</remarks>
        /// <example>Since this uses a dynamic syntax, you can execute StoredProcedures with a much cleaner style:
        ///     await connection.ExecuteNonQueryAsync(token, 30)
        ///         .data_schema // if omitted, defaults to dbo
        ///         .usp_SaveAge(personId: 16, age: 34);
        /// </example>
        public static dynamic ExecuteNonQueryAsync(this IDbConnection connection, CancellationToken token, int timeout, params IDataTransformer[] transformers)
        {
            Contract.Requires(connection != null);
            Contract.Requires(transformers != null && Contract.ForAll(transformers, t => t != null));
            Contract.Ensures(Contract.Result<object>() != null);

            return new DynamicStoredProcedure(connection,
                                              transformers,
                                              token,
                                              timeout,
                                              DynamicExecutionMode.Asynchronous,
                                              false);
        }

        /// <summary>
        /// Creates an <see cref="IDbCommand"/> to execute a stored procedure.
        /// </summary>
        /// <param name="connection">The <see cref="IDbConnection"/> to use to execute the
        /// stored procedure.</param>
        /// <param name="schema">The schema of the stored procedure to execute.</param>
        /// <param name="name">The name of the stored procedure to execute.</param>
        /// <param name="timeout">The amount of time to wait before cancelling the execution.</param>
        /// <param name="closeAfterExecute">If not null, this <see cref="IDbConnection"/> needs to
        /// be closed after the command executes.</param>
        /// <returns>The <see cref="IDbCommand"/> to execute</returns>
        internal static IDbCommand CreateCommand(
            this IDbConnection connection, 
                 string        schema,
                 string        name,
                 int           timeout,
            out  IDbConnection closeAfterExecute)
        {
            Contract.Requires(connection != null);
            Contract.Ensures (Contract.Result<IDbCommand>() != null);

            // if we don't create a new connection, connection.Open may throw
            // an exception in multi-threaded scenarios. If we don't Open it first,
            // then the connection may be closed, and it will throw an exception. 
            // We could track the connection state ourselves, but if any other code
            // uses the connection (like an EF DbSet), we could possibly close
            // the connection while a transaction is in process.
            // By only opening a clone of the connection, we avoid this issue.
            if (GlobalSettings.Instance.CloneConnectionForEachCall && connection is ICloneable)
            {
                connection = closeAfterExecute = (IDbConnection)((ICloneable)connection).Clone();
                connection.Open();
            }
            else
                closeAfterExecute = null;

            var cmd            = connection.CreateCommand();
            cmd.CommandText    = $"[{schema}].[{name}]";
            cmd.CommandType    = CommandType.StoredProcedure;
            cmd.CommandTimeout = timeout;

            return cmd;
        }
    }
}
