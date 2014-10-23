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
    /// <summary>
    /// Calls a stored procedure in the database.
    /// </summary>
    /// <remarks> This type will not return a result set. To get results from the Stored Procedure,
    /// use the <see cref="StoredProcedure{T}"/>class. If your procedure returns more than one
    /// result set, you can use the <see cref="StoredProcedure{T1,T2}"/>, <see cref="StoredProcedure{T1,T2,T3}"/>,
    /// <see cref="StoredProcedure{T1,T2,T3,T4}"/>, <see cref="StoredProcedure{T1,T2,T3,T4,T5}"/>,
    /// <see cref="StoredProcedure{T1,T2,T3,T4,T5,T6}"/>, or <see cref="StoredProcedure{T1,T2,T3,T4,T5,T6, T7}"/>
    /// classes.</remarks>
    public partial class StoredProcedure
    {
        internal const int defaultTimeout = 30;

        #region Private Fields
        private readonly string schema;
        private readonly string name;

#if NET40
        private readonly IEnumerable<IDbDataParameter>              parameters;
        private readonly ReadOnlyDictionary<string, Action<object>> outputParameterSetters;
        private readonly IEnumerable<IDataTransformer>              dataTransformers;
#else
        private readonly ImmutableList<IDbDataParameter>             parameters;
        private readonly ImmutableDictionary<string, Action<object>> outputParameterSetters; 
        private readonly ImmutableList<IDataTransformer>             dataTransformers;
#endif
        #endregion

        #region Properties
        /// <summary>
        /// Gets the schema where the stored procedure is defined. 
        /// </summary>
        /// <remarks>This property is readonly, but the value is passed in the 
        /// <see cref="StoredProcedure(string, string)"/> constructor.</remarks>
        public string Schema
        {
            get
            {
                Contract.Ensures(!string.IsNullOrWhiteSpace(Contract.Result<string>())); 

                return schema;
            }
        }

        /// <summary>
        /// Gets the name of the stored procedure in the database.
        /// </summary>
        /// <remarks>This property is readonly, but the value is passed in either constructor.</remarks>
        public string Name
        {
            get
            {
                Contract.Ensures(!string.IsNullOrWhiteSpace(Contract.Result<string>())); 

                return name;
            }
        }

        /// <summary>
        /// Gets the schema qualified name of the stored procedure.
        /// </summary>
        internal string FullName 
        {
            get
            {
                Contract.Ensures(!string.IsNullOrWhiteSpace(Contract.Result<string>()));

                return string.Format("[{0}].[{1}]", schema, name); 
            }
        }

        /// <summary>
        /// Gets the string representation of the arguments that will be passed to the StoredProcedure.
        /// </summary>
        internal string Arguments
        {
            get
            {
                if (parameters == null || !parameters.Any())
                    return string.Empty;

                return parameters.Aggregate("", (s, p) => s == "" ? Print(p) : s + ", " + Print(p));
            }
        }

#if NET40
        /// <summary>
        /// Gets the <see cref="IDbDataParameter"/>s to pass to the stored procedure.
        /// </summary>
        protected internal IEnumerable<IDbDataParameter> Parameters
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<IDbDataParameter>>() != null);

                return parameters;
            }
        }

        /// <summary>
        /// Gets a read-only map of actions to call when the stored procedure returns values
        /// as output parameters. 
        /// </summary>
        /// <remarks>If a return value setter is registered, it will be in
        /// this map as well.</remarks>
        protected internal IDictionary<string, Action<object>> OutputParameterSetters
        {
            get
            {
                Contract.Ensures(Contract.Result<IDictionary<string, Action<object>>>() != null);

                return outputParameterSetters;
            }
        }

        /// <summary>
        /// Gets the <see cref="IDataTransformer"/>s that will be used to transform the results.
        /// </summary>
        /// <remarks>These are only used in one of the StoredProcedure classes that return results.</remarks>
        protected internal IEnumerable<IDataTransformer> DataTransformers
        {
            get
            {
                Contract.Ensures(Contract.Result<IEnumerable<IDataTransformer>>() != null);

                return dataTransformers;
            }
        }
#else
        /// <summary>
        /// Gets the <see cref="IDbDataParameter"/>s to pass to the stored procedure.
        /// </summary>
        protected internal ImmutableList<IDbDataParameter> Parameters
        {
            get
            {
                Contract.Ensures(Contract.Result<ImmutableList<IDbDataParameter>>() != null);

                return parameters;
            }
        }

        /// <summary>
        /// Gets a read-only map of actions to call when the stored procedure returns values
        /// as output parameters. 
        /// </summary>
        /// <remarks>If a return value setter is registered, it will be in
        /// this map as well.</remarks>
        protected internal ImmutableDictionary<string, Action<object>> OutputParameterSetters
        {
            get
            {
                Contract.Ensures(Contract.Result<ImmutableDictionary<string, Action<object>>>() != null);

                return outputParameterSetters;
            }
        }

        /// <summary>
        /// Gets the <see cref="IDataTransformer"/>s that will be used to transform the results.
        /// </summary>
        /// <remarks>These are only used in one of the StoredProcedure classes that return results.</remarks>
        protected internal ImmutableList<IDataTransformer> DataTransformers
        {
            get
            {
                Contract.Ensures(Contract.Result<ImmutableList<IDataTransformer>>() != null);

                return dataTransformers;
            }
        }
#endif
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

#if NET40
        /// <summary>
        /// Creates a <see cref="StoredProcedure"/> with the given <paramref name="name"/>
        /// in the <paramref name="schema"/> schema.
        /// </summary>
        /// <param name="schema">The schema of the stored procedure.</param>
        /// <param name="name">The name of the stored procedure.</param>
        public StoredProcedure(string schema, string name)
            : this(schema, name,
                   new IDbDataParameter[0],
                   new Dictionary<string, Action<object>>(),
                   new IDataTransformer[0])
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
        }
        /// <summary>
        /// Creates a <see cref="StoredProcedure"/> with the given <paramref name="name"/>
        /// in the <paramref name="schema"/> schema, with the <see cref="IDbDataParameter"/>s
        /// to pass, the output action map, and the <see cref="IDataTransformer"/>s to 
        /// use to transform the results.
        /// </summary>
        /// <param name="schema">The schema of the stored procedure.</param>
        /// <param name="name">The name of the stored procedure.</param>
        /// <param name="parameters">The <see cref="IDbDataParameter"/>s to pass to the stored procedure.</param>
        /// <param name="outputParameterSetters">The map of <see cref="Action{T}"/> to call for output parameters.</param>
        /// <param name="dataTransformers">The <see cref="IDataTransformer"/>s to transform the results.</param>
        protected StoredProcedure(string                                            schema,
                                  string                                            name,
                                  IEnumerable<IDbDataParameter>                     parameters,
                                  IEnumerable<KeyValuePair<string, Action<object>>> outputParameterSetters,
                                  IEnumerable<IDataTransformer>                     dataTransformers)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
            Contract.Requires(parameters             != null);
            Contract.Requires(outputParameterSetters != null);
            Contract.Requires(dataTransformers       != null);

            this.schema                 = schema;
            this.name                   = name;
            this.parameters             = new ReadOnlyCollection<IDbDataParameter>      (parameters.ToArray());
            this.outputParameterSetters = new ReadOnlyDictionary<string, Action<object>>(outputParameterSetters.ToArray());
            this.dataTransformers       = new ReadOnlyCollection<IDataTransformer>      (dataTransformers.ToArray());
        } 
#else
        /// <summary>
        /// Creates a <see cref="StoredProcedure"/> with the given <paramref name="name"/>
        /// in the <paramref name="schema"/> schema.
        /// </summary>
        /// <param name="schema">The schema of the stored procedure.</param>
        /// <param name="name">The name of the stored procedure.</param>
        public StoredProcedure(string schema, string name)
            : this(schema, name,
                   ImmutableList<IDbDataParameter>.Empty,
                   ImmutableDictionary<string, Action<object>>.Empty,
                   ImmutableList<IDataTransformer>.Empty)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
        }

        /// <summary>
        /// Creates a <see cref="StoredProcedure"/> with the given <paramref name="name"/>
        /// in the <paramref name="schema"/> schema, with the <see cref="IDbDataParameter"/>s
        /// to pass, the output action map, and the <see cref="IDataTransformer"/>s to 
        /// use to transform the results.
        /// </summary>
        /// <param name="schema">The schema of the stored procedure.</param>
        /// <param name="name">The name of the stored procedure.</param>
        /// <param name="parameters">The <see cref="IDbDataParameter"/>s to pass to the stored procedure.</param>
        /// <param name="outputParameterSetters">The map of <see cref="Action{T}"/> to call for output parameters.</param>
        /// <param name="dataTransformers">The <see cref="IDataTransformer"/>s to transform the results.</param>
        protected StoredProcedure(string                                      schema,
                                  string                                      name,
                                  ImmutableList<IDbDataParameter>             parameters,
                                  ImmutableDictionary<string, Action<object>> outputParameterSetters,
                                  ImmutableList<IDataTransformer>             dataTransformers)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
            Contract.Requires(parameters             != null);
            Contract.Requires(outputParameterSetters != null);
            Contract.Requires(dataTransformers       != null);

            this.schema                 = schema;
            this.name                   = name;
            this.parameters             = parameters;
            this.outputParameterSetters = outputParameterSetters;
            this.dataTransformers       = dataTransformers;
        } 
#endif
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
        /// <param name="parameters">The <see cref="IDbDataParameter"/>s to pass to the stored procedure.</param>
        /// <param name="outputParameterSetters">The map of <see cref="Action{T}"/> to call for output parameters.</param>
        /// <param name="dataTransformers">The <see cref="IDataTransformer"/>s to transform the results.</param>
        /// <returns>A clone of the stored procedure.</returns>
        protected internal virtual StoredProcedure CloneCore(
#if NET40
            IEnumerable<IDbDataParameter>                     parameters,
            IEnumerable<KeyValuePair<string, Action<object>>> outputParameterSetters,
            IEnumerable<IDataTransformer>                     dataTransformers)
#else
            ImmutableList<IDbDataParameter>             parameters,
            ImmutableDictionary<string, Action<object>> outputParameterSetters,
            ImmutableList<IDataTransformer>             dataTransformers)
#endif
        {
            Contract.Requires(parameters                        != null);
            Contract.Requires(outputParameterSetters            != null);
            Contract.Requires(dataTransformers                  != null);
            Contract.Ensures(Contract.Result<StoredProcedure>() != null);

            return new StoredProcedure(schema, name, parameters, outputParameterSetters, dataTransformers);
        }

        /// <summary>
        /// Clones the current stored procedure, and adds the <paramref name="parameter"/> as 
        /// an input parameter.
        /// </summary>
        /// <param name="parameter">The <see cref="IDbDataParameter"/> to pass to the stored procedure.</param>
        /// <returns>A copy of the current <see cref="StoredProcedure"/> with the additional input parameter.</returns>
        protected internal StoredProcedure CloneWith(IDbDataParameter parameter)
        {
            Contract.Requires(parameter != null);
            Contract.Ensures(Contract.Result<StoredProcedure>() != null);

#if NET40
            return CloneCore(parameters.Concat(new[] { parameter }), outputParameterSetters, dataTransformers);
#else
            return CloneCore(parameters.Add(parameter), outputParameterSetters, dataTransformers);
#endif
        }
        
        /// <summary>
        /// Clones the current stored procedure, and adds the <paramref name="parameter"/> as 
        /// an output parameter.
        /// </summary>
        /// <param name="parameter">The <see cref="IDbDataParameter"/> to pass to the stored procedure.</param>
        /// <param name="setter">The <see cref="Action{T}"/> to call when the value is returned.</param>
        /// <returns>A copy of the current <see cref="StoredProcedure"/> with the additional output parameter.</returns>
        protected internal StoredProcedure CloneWith(IDbDataParameter parameter, Action<object> setter)
        {
            Contract.Requires(parameter != null);
            Contract.Requires(setter    != null);
            Contract.Ensures(Contract.Result<StoredProcedure>() != null);

#if NET40
            return CloneCore(parameters            .Concat(new[] { parameter }),
                             outputParameterSetters.Concat(new[] 
                                                          {
                                                              new KeyValuePair<string, Action<object>>(parameter.ParameterName, setter) 
                                                          }), 
                             dataTransformers);

#else
            return CloneCore(parameters            .Add(parameter),
                             outputParameterSetters.Add(parameter.ParameterName, setter), 
                             dataTransformers);
#endif
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

            return Task.Factory.StartNew(
                () => Execute(connection, token, timeout),
                token,
                TaskCreationOptions.None,
                TaskScheduler.Default);
        }
        #endregion

        #region MapResultType
        /// <summary>
        /// Adds a mapping for a given interface to an implementation. After doing so,
        /// any StoredProcedure that returns TInterface will return instances of TImpl
        /// </summary>
        /// <typeparam name="TInterface"></typeparam>
        /// <typeparam name="TImpl"></typeparam>
        public static void MapResultType<TInterface, TImpl>()
            where TImpl : TInterface, new()
        {
            TypeExtensions.interfaceMap.AddOrUpdate(typeof(TInterface),
                                                    typeof(TImpl),
                                                    (_, __) => typeof(TImpl));
        }
        #endregion

        /// <summary>
        /// Gets the string representation of this StoredProcedure.
        /// </summary>
        /// <remarks>This will be the fully qualified name, and all the parameters passed into it.</remarks>
        /// <returns>The string representation of this StoredProcedure.</returns>
        public override string ToString()
        {
            if (parameters == null || !parameters.Any())
                return FullName;

            return string.Format("{0}({1})", FullName, Arguments);
        }

        /// <summary>
        /// Gets the hash code that represents the StoredProcedure.
        /// </summary>
        /// <returns>The hashcode for this StoredProcedure.</returns>
        public override int GetHashCode()
        {
            return FullName.GetHashCode();
        }

        internal virtual object InternalCall(
            IDbConnection connection,
            int           commandTimeout = defaultTimeout)
        {
            Contract.Requires(connection != null);

            Execute(connection, commandTimeout);
            return null;
        }

        internal virtual object InternalCallAsync(
            IDbConnection     connection,
            CancellationToken token,
            int               commandTimeout = defaultTimeout)
        {
            Contract.Requires(connection != null);

            return ExecuteAsync(connection, token, commandTimeout);
        }

        // Suppress this message, because the sp name is never set via user input
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        internal IDictionary<Type, IList> Execute(
            IDbConnection     connection,
            CancellationToken token,
            int               commandTimeout = defaultTimeout,
            IEnumerable<Type> outputTypes    = null)
        {
            Contract.Requires(connection != null);
            Contract.Ensures(Contract.Result<IDictionary<Type, IList>>() != null);

            IDbConnection toClose = null;
            try
            {
                using (var cmd = connection.CreateCommand(schema, name, commandTimeout, out toClose))
                {
                    // move parameters to command object
                    // we must clone them first because the framework
                    // throws an exception if a parameter is passed to 
                    // more than one IDbCommand
                    var cloned = parameters.Select(p => Clone(cmd, p)).ToArray();
                    foreach (var p in cloned)
                        cmd.Parameters.Add(p);

                    token.ThrowIfCancellationRequested();

                    IDictionary<Type, IList> results;
                    if (outputTypes != null && outputTypes.Any())
                        results = cmd.Execute(token, outputTypes, dataTransformers);
                    else
                    {
                        cmd.DoExecute(c => c.ExecuteNonQuery(), token);
                        results = new Dictionary<Type, IList>();
                    }

                    foreach (var parm in cloned.Where(p => p.Direction != ParameterDirection.Input))
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
            catch (AggregateException ag)
            {
                throw new Exception("Error reading from stored proc " + FullName + ":" + Environment.NewLine + ag.InnerException.Message, ag.InnerException);
            }
            catch (Exception ex)
            {
                throw new Exception("Error reading from stored proc " + FullName + ":" + Environment.NewLine + ex.Message, ex);
            }
            finally
            {
                if (toClose != null)
                    toClose.Close();
            }
        }

        private IDbDataParameter Clone(IDbCommand cmd, IDbDataParameter p)
        {
            Contract.Requires(p != null);

            var clone = cmd.CreateParameter();

            clone.ParameterName = p.ParameterName;
            clone.DbType        = p.DbType;
            clone.Size          = p.Size;
            clone.Direction     = p.Direction;
            clone.Precision     = p.Precision;
            clone.Scale         = p.Scale;
            clone.SourceColumn  = p.SourceColumn;
            clone.SourceVersion = p.SourceVersion;
            clone.Value         = p.Value;

            if (p is SqlParameter && clone is SqlParameter)
            {
                var old = (SqlParameter)p;
                var sql = (SqlParameter)clone;

                sql.SqlDbType                       = old.SqlDbType;
                sql.SourceColumnNullMapping         = old.SourceColumnNullMapping;
                sql.XmlSchemaCollectionDatabase     = old.XmlSchemaCollectionDatabase;
                sql.XmlSchemaCollectionName         = old.XmlSchemaCollectionName;
                sql.XmlSchemaCollectionOwningSchema = old.XmlSchemaCollectionOwningSchema;
            }

            return clone;
        }

        private string Print(IDbDataParameter p)
        {
            Contract.Requires(p != null);
            Contract.Ensures(!string.IsNullOrEmpty(Contract.Result<string>()));

            return string.Format("@{0} = '{1}'", p.ParameterName, p.Value ?? "");
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
