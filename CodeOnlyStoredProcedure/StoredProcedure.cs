using CodeOnlyStoredProcedure.DataTransformation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Data.Common;

namespace CodeOnlyStoredProcedure
{
    /// <summary>
    /// Calls a stored procedure in the database that doesn't return results.
    /// </summary>
    /// <remarks> This type will not return a result set. To get results from the Stored Procedure,
    /// use the <see cref="StoredProcedure{T}"/>class. If your procedure returns more than one
    /// result set, you can use the <see cref="StoredProcedure{T1,T2}"/>, <see cref="StoredProcedure{T1,T2,T3}"/>,
    /// <see cref="StoredProcedure{T1,T2,T3,T4}"/>, <see cref="StoredProcedure{T1,T2,T3,T4,T5}"/>,
    /// <see cref="StoredProcedure{T1,T2,T3,T4,T5,T6}"/>, or <see cref="StoredProcedure{T1,T2,T3,T4,T5,T6,T7}"/>
    /// classes.</remarks>
    public partial class StoredProcedure
    {
        internal const int defaultTimeout = 30;

        #region Private Fields
        private readonly IStoredProcedureParameter[] parameters;
        private readonly IDataTransformer[]          dataTransformers;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the schema where the stored procedure is defined. 
        /// </summary>
        /// <remarks>This property is readonly, but the value is passed in the 
        /// <see cref="StoredProcedure(string, string)"/> constructor.</remarks>
        public string Schema { get; }

        /// <summary>
        /// Gets the name of the stored procedure in the database.
        /// </summary>
        /// <remarks>This property is readonly, but the value is passed in either constructor.</remarks>
        public string Name { get; }

        /// <summary>
        /// Gets the schema qualified name of the stored procedure.
        /// </summary>
        internal string FullName => $"[{Schema}].[{Name}]";

        /// <summary>
        /// Gets the string representation of the arguments that will be passed to the StoredProcedure.
        /// </summary>
        internal string Arguments
        {
            get
            {
                if (parameters.Length == 0)
                    return string.Empty;

                return string.Join(", ", parameters.Select(p => p.ToString()));
            }
        }

        /// <summary>
        /// Gets the <see cref="IStoredProcedureParameter"/>s to pass to the stored procedure.
        /// </summary>
        protected internal IEnumerable<IStoredProcedureParameter> Parameters => parameters;

        /// <summary>
        /// Gets the <see cref="IDataTransformer"/>s that will be used to transform the results.
        /// </summary>
        /// <remarks>These are only used in one of the StoredProcedure classes that return results.</remarks>
        protected internal IEnumerable<IDataTransformer> DataTransformers => dataTransformers;
        #endregion

        #region ctors
        /// <summary>
        /// Creates a <see cref="StoredProcedure"/> with the given <paramref name="name"/>
        /// in the dbo schema.
        /// </summary>
        /// <param name="name">The name of the stored procedure.</param>
        public StoredProcedure(string name)
            : this("dbo", name)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
        }

        /// <summary>
        /// Creates a <see cref="StoredProcedure"/> with the given <paramref name="name"/>
        /// in the <paramref name="schema"/> schema.
        /// </summary>
        /// <param name="schema">The schema of the stored procedure.</param>
        /// <param name="name">The name of the stored procedure.</param>
        public StoredProcedure(string schema, string name)
            : this(schema, name,
                   new IStoredProcedureParameter[0],
                   new IDataTransformer[0])
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
        }
        /// <summary>
        /// Creates a <see cref="StoredProcedure"/> with the given <paramref name="name"/>
        /// in the <paramref name="schema"/> schema, with the <see cref="IStoredProcedureParameter"/>s
        /// to pass and the <see cref="IDataTransformer"/>s to 
        /// use to transform the results.
        /// </summary>
        /// <param name="schema">The schema of the stored procedure.</param>
        /// <param name="name">The name of the stored procedure.</param>
        /// <param name="parameters">The <see cref="IStoredProcedureParameter"/>s to pass to the stored procedure.</param>
        /// <param name="dataTransformers">The <see cref="IDataTransformer"/>s to transform the results.</param>
        protected StoredProcedure(string                                 schema,
                                  string                                 name,
                                  IEnumerable<IStoredProcedureParameter> parameters,
                                  IEnumerable<IDataTransformer>          dataTransformers)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
            Contract.Requires(parameters             != null);
            Contract.Requires(dataTransformers       != null);

            this.Schema                 = schema;
            this.Name                   = name;
            this.parameters             = parameters      .ToArray();
            this.dataTransformers       = dataTransformers.ToArray();
        } 
        #endregion

        #region Create
        /// <summary>
        /// Creates a new StoredProcedure. Useful for using the Fluent API.
        /// </summary>
        /// <param name="name">The name of the stored procedure in the database.</param>
        /// <returns>A StoredProcedure that can call the sp named <paramref name="name"/>.</returns>
        public static StoredProcedure Create(string name)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
            Contract.Ensures(Contract.Result<StoredProcedure>() != null);

            return new StoredProcedure(name);
        }

        /// <summary>
        /// Creates a new StoredProcedure. Useful for using the Fluent API.
        /// </summary>
        /// <param name="name">The name of the stored procedure in the database.</param>
        /// <param name="schema">The schema of the stored procedure.</param>
        /// <returns>A StoredProcedure that can call the sp named <paramref name="name"/> in the <paramref name="schema"/>.</returns>
        public static StoredProcedure Create(string schema, string name)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
            Contract.Ensures(Contract.Result<StoredProcedure>() != null);

            return new StoredProcedure(schema, name);
        } 
        #endregion

        #region Cloning
        /// <summary>
        /// Clones the StoredProcedure, and gives it the passed parameters.
        /// </summary>
        /// <param name="parameters">The <see cref="IStoredProcedureParameter"/>s to pass to the stored procedure.</param>
        /// <param name="dataTransformers">The <see cref="IDataTransformer"/>s to transform the results.</param>
        /// <returns>A clone of the stored procedure.</returns>
        protected internal virtual StoredProcedure CloneCore(
            IEnumerable<IStoredProcedureParameter> parameters,
            IEnumerable<IDataTransformer>          dataTransformers)
        {
            Contract.Requires(parameters                        != null);
            Contract.Requires(dataTransformers                  != null);
            Contract.Ensures(Contract.Result<StoredProcedure>() != null);

            return new StoredProcedure(Schema, Name, parameters, dataTransformers);
        }

        /// <summary>
        /// Clones the current stored procedure, and adds the <paramref name="parameter"/> as 
        /// an input parameter.
        /// </summary>
        /// <param name="parameter">The <see cref="IStoredProcedureParameter"/> to pass to the stored procedure.</param>
        /// <returns>A copy of the current <see cref="StoredProcedure"/> with the additional input parameter.</returns>
        protected internal StoredProcedure CloneWith(IStoredProcedureParameter parameter)
        {
            Contract.Requires(parameter != null);
            Contract.Ensures(Contract.Result<StoredProcedure>() != null);

            return CloneCore(parameters.Concat(new[] { parameter }), dataTransformers);
        } 

        /// <summary>
        /// Clones the current stored procedure, and adds the <paramref name="transformer"/> that will attempt
        /// to transform every value in the result set(s) (for every column).
        /// </summary>
        /// <param name="transformer">The <see cref="IDataTransformer"/> to use to transform values in the results.</param>
        /// <returns>A copy of the current <see cref="StoredProcedure"/> with the additional transformer.</returns>
        protected internal StoredProcedure CloneWith(IDataTransformer transformer)
        {
            Contract.Requires(transformer != null);
            Contract.Ensures(Contract.Result<StoredProcedure>() != null);

            return CloneCore(parameters, dataTransformers.Concat(new[] { transformer }));
        }
        #endregion

        #region Execute
        /// <summary>
        /// Executes the stored procedure.
        /// </summary>
        /// <param name="connection">The connection to use to execute the StoredProcedure.</param>
        /// <param name="timeout">The number of seconds to wait before aborting the 
        /// stored procedure's execution.</param>
        /// <example>If using from an Entity Framework DbContext, the connection can be passed:
        /// <code language='cs'>
        /// storedProcedure.Execute(this.Database.Connection);
        /// </code>
        /// </example>
        public void Execute(IDbConnection connection, int timeout = defaultTimeout)
        {
            Contract.Requires(connection != null);

            Execute(connection, CancellationToken.None, timeout);
        }

        private void Execute(IDbConnection connection, CancellationToken  token, int timeout)
        {
            Contract.Requires(connection != null);

            token.ThrowIfCancellationRequested();
            using (var cmd = connection.CreateCommand(Schema, Name, timeout, out connection))
            {
                var dbParameters = AddParameters(cmd);

                token.ThrowIfCancellationRequested();
                cmd.ExecuteNonQuery();

                token.ThrowIfCancellationRequested();
                TransferOutputParameters(CancellationToken.None, dbParameters);
            }

            connection?.Close();
        }
        #endregion

        #region ExecuteAsync
        /// <summary>
        /// Executes the StoredProcedure asynchronously.
        /// </summary>
        /// <param name="connection">The connection to use to execute the StoredProcedure.</param>
        /// <param name="timeout">The number of seconds to wait before aborting the 
        /// stored procedure's execution.</param>
        /// <returns>A <see cref="Task"/> that will be completed when the StoredProcedure is finished executing.</returns>
        /// <example>If using from an Entity Framework DbContext, the connection can be passed:
        /// <code language='cs'>
        /// await storedProcedure.ExecuteAsync(this.Database.Connection);
        /// </code>
        /// </example>
        public Task ExecuteAsync(IDbConnection connection, int timeout = defaultTimeout)
        {
            Contract.Requires(connection != null);
            Contract.Ensures (Contract.Result<Task>() != null);

            return ExecuteAsync(connection, CancellationToken.None, timeout);
        }

        /// <summary>
        /// Executes the StoredProcedure asynchronously.
        /// </summary>
        /// <param name="connection">The connection to use to execute the StoredProcedure.</param>
        /// <param name="token">The <see cref="CancellationToken"/> to use to cancel the execution of the StoredProcedure.</param>
        /// <param name="timeout">The number of seconds to wait before aborting the 
        /// stored procedure's execution.</param>
        /// <returns>A <see cref="Task"/> that will be completed when the StoredProcedure is finished executing.</returns>
        /// <example>If using from an Entity Framework DbContext, the connection can be passed:
        /// <code language='cs'>
        /// var cts = new CancellationTokenSource();
        /// await storedProcedure.ExecuteAsync(this.Database.Connection, cts.Token);
        /// </code>
        /// </example>
        public Task ExecuteAsync(IDbConnection connection, CancellationToken token, int timeout = defaultTimeout)
        {
            Contract.Requires(connection != null);
            Contract.Ensures (Contract.Result<Task>() != null);

#if !NET40
            var baseClass = connection as DbConnection;
            if (baseClass != null)
            {
                IDbConnection toClose;
                using (var cmd = connection.CreateCommand(Schema, Name, timeout, out toClose) as DbCommand)
                {
                    var dbParameters = AddParameters(cmd);
                    return cmd.ExecuteNonQueryAsync(token)
                              .ContinueWith(r =>
                              {
                                  if (r.Status == TaskStatus.RanToCompletion)
                                      TransferOutputParameters(token, dbParameters);

                                  toClose?.Close();

                                  if (!r.IsCanceled && r.IsFaulted)
                                      throw r.Exception;
                              }, token);
                }
            }
#endif

            return Task.Factory.StartNew(() => Execute(connection, token, timeout), token, TaskCreationOptions.None, TaskScheduler.Default);
        }
        #endregion

        #region Global Settings
        /// <summary>
        /// Adds a mapping for a given interface to an implementation. After doing so,
        /// any StoredProcedure that returns TInterface will return instances of TImpl
        /// </summary>
        /// <typeparam name="TInterface">The interface to map to a concrete type.</typeparam>
        /// <typeparam name="TImpl">The implementation that implements the interface.</typeparam>
        public static void MapResultType<TInterface, TImpl>()
            where TImpl : TInterface, new()
        {
            GlobalSettings.Instance.InterfaceMap.AddOrUpdate(typeof(TInterface),
                                                             typeof(TImpl),
                                                             (_, __) => typeof(TImpl));
        }

        /// <summary>
        /// Adds a <see cref="IDataTransformer"/> that will be run for all StoredProcedures.
        /// There is no way to remove these once they have been set, so do not add them unless
        /// you really want them to transform all your data. If this is a <see cref="IDataTransformer{T}"/>,
        /// it will only be run for columns of type T.
        /// </summary>
        /// <param name="transformer">The <see cref="IDataTransformer"/> to apply to all results.</param>
        public static void AddGlobalTransformer(IDataTransformer transformer)
        {
            GlobalSettings.Instance.DataTransformers.Add(transformer);
        }

        /// <summary>
        /// Will automatically convert all values returned from the database into the proper type to set
        /// on the model properties for every StoredProcedure that executes.
        /// </summary>
        public static void EnableConvertOnAllNumericValues() => GlobalSettings.Instance.ConvertAllNumericValues = true;

        /// <summary>
        /// Will prevent connections from being cloned when calling a stored procedure. The default behavior will clone
        /// and dispose connections for every call (so long as the connection implements <see cref="ICloneable"/>, 
        /// as there are fewer conflicts with other frameworks who share the connection. If you have full control
        /// of the connection, we recommend disabling connection cloning. Especially if you make a lot of stored
        /// procedure calls.
        /// </summary>
        /// <remarks>Make sure your connection supports multiple active result sets, or concurrent calls will throw
        /// an exception.</remarks>
        public static void DisableConnectionCloningForEachCall() => GlobalSettings.Instance.CloneConnectionForEachCall = false;

        /// <summary>
        /// Allows you to change the default behavior of the object quoting syntax. If not set, the default style is
        /// <see cref="ObjectQuoteStyle.Brackets"/>.
        /// </summary>
        /// <param name="style">THe <see cref="ObjectQuoteStyle"/> to use to define the object quote style.</param>
        public static void SetObjectQuoteStyle(ObjectQuoteStyle style) => GlobalSettings.Instance.SetObjectQuoteStyle(style);

        /// <summary>
        /// Allows you to change the default behavior of the object quoting syntax. If not set, the open quote is "[",
        /// and the close quote is "]".
        /// </summary>
        /// <param name="openQuote">The string to use to start an object quote.</param>
        /// <param name="closeQuote">The string to use to close an object quote.</param>
        public static void SetObjectQuoteStyle(string openQuote, string closeQuote) => GlobalSettings.Instance.SetObjectQuoteStyle(openQuote, closeQuote);
        #endregion

        /// <summary>
        /// Gets the string representation of this StoredProcedure.
        /// </summary>
        /// <remarks>This will be the fully qualified name, and all the parameters passed into it.</remarks>
        /// <returns>The string representation of this StoredProcedure.</returns>
        public override string ToString()
        {
            if (parameters.Length == 0)
                return FullName;

            return $"{FullName} {Arguments}";
        }

        /// <summary>
        /// Gets the hash code that represents the StoredProcedure.
        /// </summary>
        /// <returns>The hashcode for this StoredProcedure.</returns>
        public override int GetHashCode()
        {
            return FullName.GetHashCode();
        }

        /// <summary>
        /// Adds the parameters that are defined for this stored procedure to the <see cref="IDbCommand"/> used
        /// to execute the stored procedure.
        /// </summary>
        /// <param name="cmd">The <see cref="IDbCommand"/> to add the parameters to.</param>
        /// <returns>The <see cref="IDbDataParameter"/>s that were created.</returns>
        protected IDbDataParameter[] AddParameters(IDbCommand cmd)
        {
            Contract.Requires(cmd != null);
            Contract.Ensures (Contract.Result<IDbDataParameter[]>() != null);

            var dbParameters = new List<IDbDataParameter>();
            for (int i = 0; i < parameters.Length; i++)
            {
                var dbP = parameters[i].CreateDbDataParameter(cmd);
                cmd.Parameters.Add(dbP);
                dbParameters.Add(dbP);
            }

            return dbParameters.ToArray();
        }

        /// <summary>
        /// Transfers any output values after the stored procedure has finished executing.
        /// </summary>
        /// <param name="token">The <see cref="CancellationToken"/> to use to cancel the operation.</param>
        /// <param name="dbParameters">The <see cref="IDbDataParameter"/>s that were passed to the stored procedure.</param>
        protected void TransferOutputParameters(CancellationToken token, IDbDataParameter[] dbParameters)
        {
            Contract.Requires(dbParameters != null);

            foreach (var parm in dbParameters.Where(p => p.Direction != ParameterDirection.Input))
            {
                token.ThrowIfCancellationRequested();

                parameters.OfType<IOutputStoredProcedureParameter>()
                          .FirstOrDefault(p => p.ParameterName == parm.ParameterName)
                         ?.TransferOutputValue(parm.Value);
            }
        }
    }
}
