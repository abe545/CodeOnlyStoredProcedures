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
        private static readonly IList<IDataTransformer> globalTransformers = new List<IDataTransformer>
        {
            new EnumValueTransformer()
        };

        private readonly string schema;
        private readonly string name;

#if NET40
        private readonly IEnumerable<SqlParameter>                  parameters;
        private readonly ReadOnlyDictionary<string, Action<object>> outputParameterSetters;
        private readonly IEnumerable<IDataTransformer>              dataTransformers;
#else
        private readonly ImmutableList<SqlParameter>                 parameters;
        private readonly ImmutableDictionary<string, Action<object>> outputParameterSetters; 
        private readonly ImmutableList<IDataTransformer>             dataTransformers;
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

        protected internal IEnumerable<IDataTransformer> DataTransformers
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<IDataTransformer>>() != null);

                return dataTransformers;
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
                   new Dictionary<string, Action<object>>(),
                   new IDataTransformer[0])
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
        }
#else

        public StoredProcedure(string schema, string name)
            : this(schema, name,
                   ImmutableList<SqlParameter>.Empty,
                   ImmutableDictionary<string, Action<object>>.Empty,
                   ImmutableList<IDataTransformer>.Empty)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
        }
#endif

        protected StoredProcedure(string schema,
                                  string name,
                                  IEnumerable<SqlParameter> parameters,
                                  IEnumerable<KeyValuePair<string, Action<object>>> outputParameterSetters,
                                  IEnumerable<IDataTransformer> dataTransformers)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
            Contract.Requires(parameters != null);
            Contract.Requires(outputParameterSetters != null);
            Contract.Requires(dataTransformers != null);

            this.schema                 = schema;
            this.name                   = name;
#if NET40
            this.parameters             = new ReadOnlyCollection<SqlParameter>(parameters.ToArray());
            this.outputParameterSetters = new ReadOnlyDictionary<string, Action<object>>(outputParameterSetters.ToArray());
            this.dataTransformers       = new ReadOnlyCollection<IDataTransformer>(dataTransformers.ToArray());
#else
            this.parameters             = (ImmutableList<SqlParameter>)parameters;
            this.outputParameterSetters = (ImmutableDictionary<string, Action<object>>)outputParameterSetters;
            this.dataTransformers       = (ImmutableList<IDataTransformer>)dataTransformers;
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
            return CloneCore(parameters.Concat(new[] { parameter }), outputParameterSetters, dataTransformers);
#else
            return CloneCore(parameters.Add(parameter), outputParameterSetters, dataTransformers);
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
                                                          }), 
                             dataTransformers);

#else
            return CloneCore(parameters.Add(parameter),
                             outputParameterSetters.Add(parameter.ParameterName, setter), 
                             dataTransformers);
#endif
        } 

        protected internal StoredProcedure CloneWith(IDataTransformer transformer)
        {
            Contract.Requires(transformer != null);
            Contract.Ensures(Contract.Result<StoredProcedure>() != null);

#if NET40
            return CloneCore(parameters,
                             outputParameterSetters,
                             dataTransformers.Concat(new[] { transformer }));
#else
            return CloneCore(parameters,
                             outputParameterSetters,
                             dataTransformers.Add(transformer));
#endif
        }

        protected virtual StoredProcedure CloneCore(
            IEnumerable<SqlParameter> parameters,
            IEnumerable<KeyValuePair<string, Action<object>>> outputParameterSetters,
            IEnumerable<IDataTransformer> dataTransformers)
        {
            Contract.Requires(parameters != null);
            Contract.Requires(outputParameterSetters != null);
            Contract.Requires(dataTransformers != null);
            Contract.Ensures(Contract.Result<StoredProcedure>() != null);

            return new StoredProcedure(schema, name, parameters, outputParameterSetters, dataTransformers);
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
        public void Execute(IDbConnection connection, int? timeout = null)
        {
            Contract.Requires(connection != null);

            Execute(connection, CancellationToken.None, timeout);
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
                () => Execute(connection, token, timeout), token);
        }
        #endregion

        // Suppress this message, because the sp name is never set via user input
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        internal IDictionary<Type, IList> Execute(
            IDbConnection     connection,
            CancellationToken token,
            int?              commandTimeout = null,
            IEnumerable<Type> outputTypes    = null)
        {
            Contract.Requires(connection != null);
            Contract.Ensures(Contract.Result<IDictionary<Type, IList>>() != null);

            bool shouldClose = false;
            try
            {
                // if we don't create a new connection, connection.Open may throw
                // an exception in multi-threaded scenarios. If we don't Open it first,
                // then the connection may be closed, and it will throw an exception. 
                // We could track the connection state ourselves, but if any other code
                // uses the connection (like an EF DbSet), we could possibly close
                // the connection while a transaction is in process.
                // By only opening a clone of the connection, we avoid this issue.
                if (connection is ICloneable)
                {
                    connection = (IDbConnection)((ICloneable)connection).Clone();
                    connection.Open();
                    shouldClose = true;
                }

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText    = FullName;
                    cmd.CommandType    = CommandType.StoredProcedure;
                    cmd.CommandTimeout = commandTimeout ?? 10;

                    // move parameters to command object
                    // we must clone them first because the framework
                    // throws an exception if a parameter is passed to 
                    // more than one IDbCommand
                    var cloned = parameters.Select(Clone).ToArray();
                    foreach (var p in cloned)
                        cmd.Parameters.Add(p);

                    token.ThrowIfCancellationRequested();

                    IDictionary<Type, IList> results;
                    if (outputTypes != null && outputTypes.Any())
                        results = cmd.Execute(token, outputTypes, globalTransformers.Concat(dataTransformers));
                    else
                    {
                        cmd.DoExecute(c => c.ExecuteNonQuery(), token);
                        results = new Dictionary<Type, IList>();
                    }

                    foreach (var parm in parameters.Where(p => p.Direction != ParameterDirection.Input))
                        outputParameterSetters[parm.ParameterName](parm.Value);

                    return results;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch(TimeoutException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception("Error reading from stored proc " + FullName + ":" + Environment.NewLine + ex.Message, ex);
            }
            finally
            {
                if (shouldClose)
                    connection.Close();
            }
        }

        private SqlParameter Clone(SqlParameter p)
        {
            return new SqlParameter(p.ParameterName, 
                                    p.SqlDbType,
                                    p.Size,
                                    p.Direction,
                                    p.Precision,
                                    p.Scale, 
                                    p.SourceColumn, 
                                    p.SourceVersion,
                                    p.SourceColumnNullMapping, 
                                    p.Value,
                                    p.XmlSchemaCollectionDatabase,
                                    p.XmlSchemaCollectionOwningSchema, 
                                    p.XmlSchemaCollectionName);
        }

        [ContractInvariantMethod]
        private void Invariants()
        {
            Contract.Invariant(!string.IsNullOrWhiteSpace(schema));
            Contract.Invariant(!string.IsNullOrWhiteSpace(name));
            Contract.Invariant(parameters             != null);
            Contract.Invariant(outputParameterSetters != null);
            Contract.Invariant(dataTransformers       != null);
        }
    }
}
