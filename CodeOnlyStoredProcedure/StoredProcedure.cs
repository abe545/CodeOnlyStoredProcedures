using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeOnlyStoredProcedure
{
    public class StoredProcedure
    {
        #region Private Fields
        private readonly string schema;
        private readonly string name;
        private readonly ImmutableList<SqlParameter> parameters;
        private readonly ImmutableDictionary<string, Action<object>> outputParameterSetters; 
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

        protected internal ImmutableList<SqlParameter> Parameters
        {
            get
            {
                Contract.Ensures(Contract.Result<ImmutableList<SqlParameter>>() != null);
                return parameters;
            }
        }

        protected internal ImmutableDictionary<string, Action<object>> OutputParameterSetters
        {
            get
            {
                Contract.Ensures(Contract.Result<ImmutableDictionary<string, Action<object>>>() != null);
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

        public StoredProcedure(string schema, string name)
            : this(schema, name,
                   ImmutableList<SqlParameter>.Empty,
                   ImmutableDictionary<string, Action<object>>.Empty)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
        }

        protected StoredProcedure(string schema,
            string name,
            ImmutableList<SqlParameter> parameters,
            ImmutableDictionary<string, Action<object>> outputParameterSetters)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
            Contract.Requires(parameters != null);
            Contract.Requires(outputParameterSetters != null);

            this.schema = schema;
            this.name = name;
            this.parameters = parameters;
            this.outputParameterSetters = outputParameterSetters;
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
            return CloneCore(parameters.Add(parameter), outputParameterSetters);
        }

        protected internal StoredProcedure CloneWith(SqlParameter parameter, Action<object> setter)
        {
            return CloneCore(parameters.Add(parameter), 
                outputParameterSetters.Add(parameter.ParameterName, setter));
        }

        protected virtual StoredProcedure CloneCore(ImmutableList<SqlParameter> parameters,
            ImmutableDictionary<string, Action<object>> outputParameterSetters)
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
            Contract.Ensures(Contract.Result<Task>() != null);

            return ExecuteAsync(connection, CancellationToken.None, timeout);
        }

        public Task ExecuteAsync(IDbConnection connection, CancellationToken token, int? timeout = null)
        {
            Contract.Requires(connection != null);
            Contract.Ensures(Contract.Result<Task>() != null);

            return Task.Run(() => Execute(connection, token, timeout));
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
