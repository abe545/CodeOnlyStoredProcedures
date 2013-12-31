using System;
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

            return new StoredProcedure(name);
        }

        public static StoredProcedure Create(string schema, string name)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));

            return new StoredProcedure(schema, name);
        } 
        #endregion

        protected internal StoredProcedure CloneWith(SqlParameter parameter)
        {
#if NET40
            return CloneCore(parameters.Concat(new[] { parameter }), outputParameterSetters);
#else
            return CloneCore(parameters.Add(parameter), outputParameterSetters);
#endif
        }

        protected internal StoredProcedure CloneWith(SqlParameter parameter, Action<object> setter)
        {
#if NET40
            return CloneCore(parameters            .Concat(new[] { parameter }),
                             outputParameterSetters.Concat(new[] 
                                                          {
                                                              new KeyValuePair<string, Action<object>>(parameter.ParameterName, setter) 
                                                          }));

#else
            return CloneCore(parameters            .Add(parameter), 
                             outputParameterSetters.Add(parameter.ParameterName, setter));
#endif
        }

        protected virtual StoredProcedure CloneCore(IEnumerable<SqlParameter> parameters,
            IEnumerable<KeyValuePair<string, Action<object>>> outputParameterSetters)
        {
            return new StoredProcedure(schema, name, parameters, outputParameterSetters);
        }

        #region Execute
        public void Execute(IDbConnection connection, int? timeout = null)
        {
            Contract.Requires(connection != null);

            Execute(connection, CancellationToken.None, timeout);
        }

        void Execute(IDbConnection connection, CancellationToken token, int? timeout = null)
        {
            Contract.Requires(connection != null);

            connection.Execute(FullName,
                               token,
                               timeout,
                               parameters,
                               Enumerable.Empty<Type>());

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

            return Task.Factory.StartNew(() => Execute(connection, token, timeout));
        }
        #endregion

        protected void TransferOutputParameters()
        {
            foreach (var parm in parameters.Where(p => p.Direction != ParameterDirection.Input))
                outputParameterSetters[parm.ParameterName](parm.Value);
        }

        [ContractInvariantMethod]
        private void Invariants()
        {
            Contract.Invariant(!string.IsNullOrWhiteSpace(schema));
            Contract.Invariant(!string.IsNullOrWhiteSpace(name));
            Contract.Invariant(parameters != null);
            Contract.Invariant(outputParameterSetters != null);
        }
    }
}
