using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
#if NET40
using System.Collections.ObjectModel;
#else
using System.Collections.Immutable;
#endif

namespace CodeOnlyStoredProcedure
{
    public class StoredProcedure
    {
        #region Private Fields
        private readonly string schema;
        private readonly string name;

#if NET40
        private readonly IEnumerable<SqlParameter>                  parameters;
        private readonly ReadOnlyDictionary<string, Action<object>> outputParameterSetters;
#else
        private readonly ImmutableList<SqlParameter>                 parameters;
        private readonly ImmutableDictionary<string, Action<object>> outputParameterSetters; 
#endif
        #endregion

        #region Properties
        public string Schema
        {
            get
            {
                Contract.Ensures(!string.IsNullOrWhiteSpace(Contract.Result<string>())); 

                return schema;
            }
        }

        public string Name
        {
            get
            {
                Contract.Ensures(!string.IsNullOrWhiteSpace(Contract.Result<string>())); 

                return name;
            }
        }

        internal string FullName 
        {
            get
            {
                Contract.Ensures(!string.IsNullOrWhiteSpace(Contract.Result<string>()));

                return string.Format("[{0}].[{1}]", schema, name); 
            }
        }

        protected internal IEnumerable<SqlParameter> Parameters
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<SqlParameter>>() != null);

                return parameters;
            }
        }

        protected internal IDictionary<string, Action<object>> OutputParameterSetters
        {
            get
            {
                Contract.Ensures(Contract.Result<IDictionary<string, Action<object>>>() != null);

                return outputParameterSetters;
            }
        }
        #endregion

        #region ctors
        public StoredProcedure(string name)
            : this("dbo", name)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
        }

#if NET40
        public StoredProcedure(string schema, string name)
            : this(schema, name,
                   new SqlParameter[0],
                   new Dictionary<string, Action<object>>())
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
        }
#else

        public StoredProcedure(string schema, string name)
            : this(schema, name,
                   ImmutableList<SqlParameter>.Empty,
                   ImmutableDictionary<string, Action<object>>.Empty)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
        }
#endif

        protected StoredProcedure(string schema,
                                  string name,
                                  IEnumerable<SqlParameter> parameters,
                                  IEnumerable<KeyValuePair<string, Action<object>>> outputParameterSetters)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
            Contract.Requires(parameters != null);
            Contract.Requires(outputParameterSetters != null);

            this.schema                 = schema;
            this.name                   = name;
#if NET40
            this.parameters             = new ReadOnlyCollection<SqlParameter>(parameters.ToArray());
            this.outputParameterSetters = new ReadOnlyDictionary<string, Action<object>>(outputParameterSetters.ToArray());
#else
            this.parameters             = (ImmutableList<SqlParameter>)parameters;
            this.outputParameterSetters = (ImmutableDictionary<string, Action<object>>)outputParameterSetters;
#endif
        } 
        #endregion

        #region Create
        public static StoredProcedure Create(string name)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
            Contract.Ensures(Contract.Result<StoredProcedure>() != null);

            return new StoredProcedure(name);
        }

        public static StoredProcedure Create(string schema, string name)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
            Contract.Ensures(Contract.Result<StoredProcedure>() != null);

            return new StoredProcedure(schema, name);
        } 
        #endregion

        #region Cloning
        protected internal StoredProcedure CloneWith(SqlParameter parameter)
        {
            Contract.Requires(parameter != null);
            Contract.Ensures(Contract.Result<StoredProcedure>() != null);

#if NET40
            return CloneCore(parameters.Concat(new[] { parameter }), outputParameterSetters);
#else
            return CloneCore(parameters.Add(parameter), outputParameterSetters);
#endif
        }

        protected internal StoredProcedure CloneWith(SqlParameter parameter, Action<object> setter)
        {
            Contract.Requires(parameter != null);
            Contract.Requires(setter != null);
            Contract.Ensures(Contract.Result<StoredProcedure>() != null);

#if NET40
            return CloneCore(parameters            .Concat(new[] { parameter }),
                             outputParameterSetters.Concat(new[] 
                                                          {
                                                              new KeyValuePair<string, Action<object>>(parameter.ParameterName, setter) 
                                                          }));

#else
            return CloneCore(parameters.Add(parameter),
                             outputParameterSetters.Add(parameter.ParameterName, setter));
#endif
        } 

        protected virtual StoredProcedure CloneCore(IEnumerable<SqlParameter> parameters,
            IEnumerable<KeyValuePair<string, Action<object>>> outputParameterSetters)
        {
            Contract.Requires(parameters != null);
            Contract.Requires(outputParameterSetters != null);
            Contract.Ensures(Contract.Result<StoredProcedure>() != null);

            return new StoredProcedure(schema, name, parameters, outputParameterSetters);
        }
        #endregion

        #region Execute
        public void Execute(IDbConnection connection, int? timeout = null)
        {
            Contract.Requires(connection != null);

            Execute(connection, CancellationToken.None, timeout);
            TransferOutputParameters();
        }
        #endregion

        #region ExecuteAsync
        public Task ExecuteAsync(IDbConnection connection, int? timeout = null)
        {
            Contract.Requires(connection != null);
            Contract.Ensures (Contract.Result<Task>() != null);

            return ExecuteAsync(connection, CancellationToken.None, timeout);
        }

        public Task ExecuteAsync(IDbConnection connection, CancellationToken token, int? timeout = null)
        {
            Contract.Requires(connection != null);
            Contract.Ensures (Contract.Result<Task>() != null);

            return Task.Factory.StartNew(
                () =>
                {
                    Execute(connection, token, timeout);
                    TransferOutputParameters();
                }, token);
        }
        #endregion

        // Suppress this message, because the sp name is never set via user input
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        protected IDictionary<Type, IList> Execute(
            IDbConnection     connection,
            CancellationToken token,
            int?              commandTimeout = null,
            IEnumerable<Type> outputTypes    = null)
        {
            Contract.Requires(connection != null);
            Contract.Ensures(Contract.Result<IDictionary<Type, IList>>() != null);

            try
            {
                // if we don't create a new connection, connection.Open may throw
                // an exception in multi-threaded scenarios. If we don't Open it first,
                // then the connection may be closed, and it will throw an exception. 
                // We could track the connection state ourselves, but if any other code
                // uses the connection (like an EF DbSet), we could possibly close
                // the connection while a transaction is in process.
                connection = new SqlConnection(connection.ConnectionString);
                connection.Open();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = FullName;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = commandTimeout ?? 10;

                    // move parameters to command object
                    foreach (var p in parameters)
                        cmd.Parameters.Add(p);

                    token.ThrowIfCancellationRequested();

                    IDictionary<Type, IList> results;
                    if (outputTypes != null && outputTypes.Any())
                        results = cmd.Execute(token, outputTypes);
                    else
                    {
                        cmd.ExecuteNonQuery();
                        results = new Dictionary<Type, IList>();
                    }

                    TransferOutputParameters();
                    return results;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error reading from stored proc " + FullName + ":" + Environment.NewLine + ex.Message, ex);
            }
            finally
            {
                connection.Close();
            }
        }

        private void TransferOutputParameters()
        {
            foreach (var parm in parameters.Where(p => p.Direction != ParameterDirection.Input))
                outputParameterSetters[parm.ParameterName](parm.Value);
        }

        [ContractInvariantMethod]
        private void Invariants()
        {
            Contract.Invariant(!string.IsNullOrWhiteSpace(schema));
            Contract.Invariant(!string.IsNullOrWhiteSpace(name));
            Contract.Invariant(parameters             != null);
            Contract.Invariant(outputParameterSetters != null);
        }
    }
}
