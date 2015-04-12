using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace CodeOnlyStoredProcedure
{
	#region StoredProcedure<T1>
	/// <summary>Calls a StoredProcedure that returns 1 result set(s).</summary>
	/// <typeparam name="T1">The type of the first result set returned by the stored procedure.</typeparam>
	public class StoredProcedure<T1> : StoredProcedure	{
		protected bool isSingleResult = true;
		private IRowFactory<T1> factory;

		internal IRowFactory<T1> T1Factory 
		{
			get
			{
				Contract.Ensures(Contract.Result<IRowFactory<T1>>() != null);
				
				if (factory == null)
					factory = RowFactory<T1>.Create(isSingleResult);

				return factory;
			}
		}

        /// <summary>
        /// Creates a <see cref="StoredProcedure"/> with the given <paramref name="name"/>
        /// in the dbo schema.
        /// </summary>
        /// <param name="name">The name of the stored procedure.</param>
		public StoredProcedure(string name) : base(name)
		{ 
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
			Contract.Requires(typeof(T1).IsValidResultType());				
		}
		
        /// <summary>
        /// Creates a <see cref="StoredProcedure"/> with the given <paramref name="name"/>
        /// in the <paramref name="schema"/> schema.
        /// </summary>
        /// <param name="schema">The schema of the stored procedure.</param>
        /// <param name="name">The name of the stored procedure.</param>
		public StoredProcedure(string schema, string name) : base(schema, name)
		{ 
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
			Contract.Requires(typeof(T1).IsValidResultType());	
					}
		
		internal StoredProcedure(StoredProcedure toClone)
			: base(toClone.Schema, toClone.Name, toClone.Parameters, toClone.DataTransformers) 
		{ 
			Contract.Requires(toClone != null);
					}
				
        /// <summary>
        /// Creates a <see cref="StoredProcedure"/> with the given <paramref name="name"/>
        /// in the <paramref name="schema"/> schema, with the <see cref="IStoredProcedureParameter"/>s
        /// to pass, the output action map, and the <see cref="IDataTransformer"/>s to 
        /// use to transform the results.
        /// </summary>
        /// <param name="schema">The schema of the stored procedure.</param>
        /// <param name="name">The name of the stored procedure.</param>
        /// <param name="parameters">The <see cref="IStoredProcedureParameter"/>s to pass to the stored procedure.</param>
        /// <param name="dataTransformers">The <see cref="IDataTransformer"/>s to transform the results.</param>
		protected StoredProcedure(string schema, 
            string name,
            IEnumerable<IStoredProcedureParameter> parameters,
			IEnumerable<IDataTransformer> dataTransformers)
			: base(schema, name, parameters, dataTransformers)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
			Contract.Requires(parameters       != null);
			Contract.Requires(dataTransformers != null);
		}
	
        /// <summary>
        /// Executes the stored procedure.
        /// </summary>
        /// <param name="connection">The connection to use to execute the StoredProcedure.</param>
        /// <param name="timeout">The number of seconds to wait before aborting the 
        /// stored procedure's execution.</param>
		/// <returns>The results from the stored procedure.</returns>
        /// <example>If using from an Entity Framework DbContext, the connection can be passed:
        /// <code language='cs'>
        /// var results = storedProcedure.Execute(this.Database.Connection);
        /// </code>
        /// </example>
		public new IEnumerable<T1> Execute(IDbConnection connection, int timeout = defaultTimeout)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<IEnumerable<T1>>() != null);

			return Execute(connection, CancellationToken.None, timeout);
		}
		
		private IEnumerable<T1> Execute(IDbConnection connection, CancellationToken token, int timeout = defaultTimeout)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<IEnumerable<T1>>() != null);

			IEnumerable<T1> results;
			using (var cmd = connection.CreateCommand(Schema, Name, timeout, out connection))
            {
				var dbParameters = AddParameters(cmd);

				token.ThrowIfCancellationRequested();

				var reader = cmd.ExecuteReader();
				results = T1Factory.ParseRows(reader, DataTransformers, token);
            }

            if (connection != null)
                connection.Close();
			
			return results;
		}
		
        /// <summary>
        /// Executes the StoredProcedure asynchronously.
        /// </summary>
        /// <param name="connection">The connection to use to execute the StoredProcedure.</param>
        /// <param name="timeout">The number of seconds to wait before aborting the 
        /// stored procedure's execution.</param>
        /// <returns>A Task&lt;IEnumerable&lt;T1&gt;&gt; that will be completed when the StoredProcedure is finished executing.</returns>
        /// <example>If using from an Entity Framework DbContext, the connection can be passed:
        /// <code language='cs'>
        /// var results = await storedProcedure.ExecuteAsync(this.Database.Connection);
        /// </code>
        /// </example>
		public new Task<IEnumerable<T1>> ExecuteAsync(IDbConnection connection, int timeout = defaultTimeout)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Task<IEnumerable<T1>>>() != null);

			return ExecuteAsync(connection, CancellationToken.None, timeout);
		}
		
        /// <summary>
        /// Executes the StoredProcedure asynchronously.
        /// </summary>
        /// <param name="connection">The connection to use to execute the StoredProcedure.</param>
        /// <param name="token">The <see cref="CancellationToken"/> to use to cancel the execution of the StoredProcedure.</param>
        /// <param name="timeout">The number of seconds to wait before aborting the 
        /// stored procedure's execution.</param>
        /// <returns>A Task&lt;IEnumerable&lt;T1&gt;&gt; that will be completed when the StoredProcedure is finished executing.</returns>
        /// <example>If using from an Entity Framework DbContext, the connection can be passed:
        /// <code language='cs'>
		/// var cts     = new CancellationTokenSource();
        /// var results = await storedProcedure.ExecuteAsync(this.Database.Connection, cts.Token);
        /// </code>
        /// </example>
		public new Task<IEnumerable<T1>> ExecuteAsync(IDbConnection connection, CancellationToken token, int timeout = defaultTimeout)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Task<IEnumerable<T1>>>() != null);
			
#if !NET40
            var baseClass = connection as DbConnection;
            if (baseClass != null)
            {
                IDbConnection toClose;
                var cmd = connection.CreateCommand(Schema, Name, timeout, out toClose);
                var asyncCapable = cmd as DbCommand;
                return ExecuteAsync(asyncCapable, toClose, token);
            }
#endif

			return Task.Factory.StartNew(
				() => Execute(connection, token, timeout), 
				token,
                TaskCreationOptions.None,
                TaskScheduler.Default);
		}
		
#if !NET40
		async Task<IEnumerable<T1>> ExecuteAsync(DbCommand cmd, IDbConnection toClose, CancellationToken token)
		{
			Contract.Requires(cmd != null);
			Contract.Ensures(Contract.Result<Task<IEnumerable<T1>>>() != null);

			IEnumerable<T1> results;
            var dbParameters = AddParameters(cmd);
			
            token.ThrowIfCancellationRequested();
			var reader = await cmd.ExecuteReaderAsync(token);
			results = await T1Factory.ParseRowsAsync(reader, DataTransformers, token);

            token.ThrowIfCancellationRequested();
			TransferOutputParameters(token, dbParameters);

			if (toClose != null)
				toClose.Close();

			cmd.Dispose();

			return results;
		}
#endif

		
        /// <summary>
        /// Clones the StoredProcedure, and gives it the passed parameters.
        /// </summary>
        /// <param name="parameters">The <see cref="IStoredProcedureParameter"/>s to pass to the stored procedure.</param>
        /// <param name="dataTransformers">The <see cref="IDataTransformer"/>s to transform the results.</param>
        /// <returns>A clone of the stored procedure.</returns>
		protected internal override StoredProcedure CloneCore(
			IEnumerable<IStoredProcedureParameter> parameters,
			IEnumerable<IDataTransformer>          dataTransformers)
		{
			return new StoredProcedure<T1>(Schema, Name, parameters, dataTransformers);
		}	
	}
	#endregion

	#region StoredProcedure<T1, T2>
	/// <summary>Calls a StoredProcedure that returns 2 result set(s).</summary>
	/// <typeparam name="T1">The type of the first result set returned by the stored procedure.</typeparam>
	/// <typeparam name="T2">The type of the second result set returned by the stored procedure.</typeparam>
	public class StoredProcedure<T1, T2> : StoredProcedure<T1>	{
				private IRowFactory<T2> factory;

		internal IRowFactory<T2> T2Factory 
		{
			get
			{
				Contract.Ensures(Contract.Result<IRowFactory<T2>>() != null);
				
				if (factory == null)
					factory = RowFactory<T2>.Create(isSingleResult);

				return factory;
			}
		}

        /// <summary>
        /// Creates a <see cref="StoredProcedure"/> with the given <paramref name="name"/>
        /// in the dbo schema.
        /// </summary>
        /// <param name="name">The name of the stored procedure.</param>
		public StoredProcedure(string name) : base(name)
		{ 
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
			Contract.Requires(typeof(T1).IsValidResultType());
			Contract.Requires(typeof(T2).IsValidResultType());			isSingleResult = false;
	
		}
		
        /// <summary>
        /// Creates a <see cref="StoredProcedure"/> with the given <paramref name="name"/>
        /// in the <paramref name="schema"/> schema.
        /// </summary>
        /// <param name="schema">The schema of the stored procedure.</param>
        /// <param name="name">The name of the stored procedure.</param>
		public StoredProcedure(string schema, string name) : base(schema, name)
		{ 
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
			Contract.Requires(typeof(T1).IsValidResultType());
			Contract.Requires(typeof(T2).IsValidResultType());	
			isSingleResult = false;
		}
		
		internal StoredProcedure(StoredProcedure toClone)
			: base(toClone.Schema, toClone.Name, toClone.Parameters, toClone.DataTransformers) 
		{ 
			Contract.Requires(toClone != null);
			isSingleResult = false;
		}
				
        /// <summary>
        /// Creates a <see cref="StoredProcedure"/> with the given <paramref name="name"/>
        /// in the <paramref name="schema"/> schema, with the <see cref="IStoredProcedureParameter"/>s
        /// to pass, the output action map, and the <see cref="IDataTransformer"/>s to 
        /// use to transform the results.
        /// </summary>
        /// <param name="schema">The schema of the stored procedure.</param>
        /// <param name="name">The name of the stored procedure.</param>
        /// <param name="parameters">The <see cref="IStoredProcedureParameter"/>s to pass to the stored procedure.</param>
        /// <param name="dataTransformers">The <see cref="IDataTransformer"/>s to transform the results.</param>
		protected StoredProcedure(string schema, 
            string name,
            IEnumerable<IStoredProcedureParameter> parameters,
			IEnumerable<IDataTransformer> dataTransformers)
			: base(schema, name, parameters, dataTransformers)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
			Contract.Requires(parameters       != null);
			Contract.Requires(dataTransformers != null);
		}
	
        /// <summary>
        /// Executes the stored procedure.
        /// </summary>
        /// <param name="connection">The connection to use to execute the StoredProcedure.</param>
        /// <param name="timeout">The number of seconds to wait before aborting the 
        /// stored procedure's execution.</param>
		/// <returns>The results from the stored procedure.</returns>
        /// <example>If using from an Entity Framework DbContext, the connection can be passed:
        /// <code language='cs'>
        /// var results = storedProcedure.Execute(this.Database.Connection);
        /// </code>
        /// </example>
		public new Tuple<IEnumerable<T1>, IEnumerable<T2>> Execute(IDbConnection connection, int timeout = defaultTimeout)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Tuple<IEnumerable<T1>, IEnumerable<T2>>>() != null);

			return Execute(connection, CancellationToken.None, timeout);
		}
		
		private Tuple<IEnumerable<T1>, IEnumerable<T2>> Execute(IDbConnection connection, CancellationToken token, int timeout = defaultTimeout)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Tuple<IEnumerable<T1>, IEnumerable<T2>>>() != null);

			Tuple<IEnumerable<T1>, IEnumerable<T2>> results;
			using (var cmd = connection.CreateCommand(Schema, Name, timeout, out connection))
            {
				var dbParameters = AddParameters(cmd);

				token.ThrowIfCancellationRequested();

				var reader = cmd.ExecuteReader();
				var t1 = T1Factory.ParseRows(reader, DataTransformers, token);

				reader.NextResult();
				token.ThrowIfCancellationRequested();

				var t2 = T2Factory.ParseRows(reader, DataTransformers, token);
				results = Tuple.Create(t1, t2);

				token.ThrowIfCancellationRequested();

				TransferOutputParameters(CancellationToken.None, dbParameters);
            }

            if (connection != null)
                connection.Close();
			
			return results;
		}
		
        /// <summary>
        /// Executes the StoredProcedure asynchronously.
        /// </summary>
        /// <param name="connection">The connection to use to execute the StoredProcedure.</param>
        /// <param name="timeout">The number of seconds to wait before aborting the 
        /// stored procedure's execution.</param>
        /// <returns>A Task&lt;Tuple&lt;IEnumerable&lt;T1&gt;, IEnumerable&lt;T2&gt;&gt;&gt; that will be completed when the StoredProcedure is finished executing.</returns>
        /// <example>If using from an Entity Framework DbContext, the connection can be passed:
        /// <code language='cs'>
        /// var results = await storedProcedure.ExecuteAsync(this.Database.Connection);
        /// </code>
        /// </example>
		public new Task<Tuple<IEnumerable<T1>, IEnumerable<T2>>> ExecuteAsync(IDbConnection connection, int timeout = defaultTimeout)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Task<Tuple<IEnumerable<T1>, IEnumerable<T2>>>>() != null);

			return ExecuteAsync(connection, CancellationToken.None, timeout);
		}
		
        /// <summary>
        /// Executes the StoredProcedure asynchronously.
        /// </summary>
        /// <param name="connection">The connection to use to execute the StoredProcedure.</param>
        /// <param name="token">The <see cref="CancellationToken"/> to use to cancel the execution of the StoredProcedure.</param>
        /// <param name="timeout">The number of seconds to wait before aborting the 
        /// stored procedure's execution.</param>
        /// <returns>A Task&lt;Tuple&lt;IEnumerable&lt;T1&gt;, IEnumerable&lt;T2&gt;&gt;&gt; that will be completed when the StoredProcedure is finished executing.</returns>
        /// <example>If using from an Entity Framework DbContext, the connection can be passed:
        /// <code language='cs'>
		/// var cts     = new CancellationTokenSource();
        /// var results = await storedProcedure.ExecuteAsync(this.Database.Connection, cts.Token);
        /// </code>
        /// </example>
		public new Task<Tuple<IEnumerable<T1>, IEnumerable<T2>>> ExecuteAsync(IDbConnection connection, CancellationToken token, int timeout = defaultTimeout)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Task<Tuple<IEnumerable<T1>, IEnumerable<T2>>>>() != null);
			
#if !NET40
            var baseClass = connection as DbConnection;
            if (baseClass != null)
            {
                IDbConnection toClose;
                var cmd = connection.CreateCommand(Schema, Name, timeout, out toClose);
                var asyncCapable = cmd as DbCommand;
                return ExecuteAsync(asyncCapable, toClose, token);
            }
#endif

			return Task.Factory.StartNew(
				() => Execute(connection, token, timeout), 
				token,
                TaskCreationOptions.None,
                TaskScheduler.Default);
		}
		
#if !NET40
		async Task<Tuple<IEnumerable<T1>, IEnumerable<T2>>> ExecuteAsync(DbCommand cmd, IDbConnection toClose, CancellationToken token)
		{
			Contract.Requires(cmd != null);
			Contract.Ensures(Contract.Result<Task<Tuple<IEnumerable<T1>, IEnumerable<T2>>>>() != null);

			Tuple<IEnumerable<T1>, IEnumerable<T2>> results;
            var dbParameters = AddParameters(cmd);
			
            token.ThrowIfCancellationRequested();
			var reader = await cmd.ExecuteReaderAsync(token);
			var t1 = await T1Factory.ParseRowsAsync(reader, DataTransformers, token);
			await reader.NextResultAsync(token);
			var t2 = await T2Factory.ParseRowsAsync(reader, DataTransformers, token);
			results = Tuple.Create(t1, t2);

            token.ThrowIfCancellationRequested();
			TransferOutputParameters(token, dbParameters);

			if (toClose != null)
				toClose.Close();

			cmd.Dispose();

			return results;
		}
#endif

		
        /// <summary>
        /// Clones the StoredProcedure, and gives it the passed parameters.
        /// </summary>
        /// <param name="parameters">The <see cref="IStoredProcedureParameter"/>s to pass to the stored procedure.</param>
        /// <param name="dataTransformers">The <see cref="IDataTransformer"/>s to transform the results.</param>
        /// <returns>A clone of the stored procedure.</returns>
		protected internal override StoredProcedure CloneCore(
			IEnumerable<IStoredProcedureParameter> parameters,
			IEnumerable<IDataTransformer>          dataTransformers)
		{
			return new StoredProcedure<T1, T2>(Schema, Name, parameters, dataTransformers);
		}	
	}
	#endregion

	#region StoredProcedure<T1, T2, T3>
	/// <summary>Calls a StoredProcedure that returns 3 result set(s).</summary>
	/// <typeparam name="T1">The type of the first result set returned by the stored procedure.</typeparam>
	/// <typeparam name="T2">The type of the second result set returned by the stored procedure.</typeparam>
	/// <typeparam name="T3">The type of the third result set returned by the stored procedure.</typeparam>
	public class StoredProcedure<T1, T2, T3> : StoredProcedure<T1, T2>	{
				private IRowFactory<T3> factory;

		internal IRowFactory<T3> T3Factory 
		{
			get
			{
				Contract.Ensures(Contract.Result<IRowFactory<T3>>() != null);
				
				if (factory == null)
					factory = RowFactory<T3>.Create(isSingleResult);

				return factory;
			}
		}

        /// <summary>
        /// Creates a <see cref="StoredProcedure"/> with the given <paramref name="name"/>
        /// in the dbo schema.
        /// </summary>
        /// <param name="name">The name of the stored procedure.</param>
		public StoredProcedure(string name) : base(name)
		{ 
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
			Contract.Requires(typeof(T1).IsValidResultType());
			Contract.Requires(typeof(T2).IsValidResultType());
			Contract.Requires(typeof(T3).IsValidResultType());			isSingleResult = false;
	
		}
		
        /// <summary>
        /// Creates a <see cref="StoredProcedure"/> with the given <paramref name="name"/>
        /// in the <paramref name="schema"/> schema.
        /// </summary>
        /// <param name="schema">The schema of the stored procedure.</param>
        /// <param name="name">The name of the stored procedure.</param>
		public StoredProcedure(string schema, string name) : base(schema, name)
		{ 
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
			Contract.Requires(typeof(T1).IsValidResultType());
			Contract.Requires(typeof(T2).IsValidResultType());
			Contract.Requires(typeof(T3).IsValidResultType());	
			isSingleResult = false;
		}
		
		internal StoredProcedure(StoredProcedure toClone)
			: base(toClone.Schema, toClone.Name, toClone.Parameters, toClone.DataTransformers) 
		{ 
			Contract.Requires(toClone != null);
			isSingleResult = false;
		}
				
        /// <summary>
        /// Creates a <see cref="StoredProcedure"/> with the given <paramref name="name"/>
        /// in the <paramref name="schema"/> schema, with the <see cref="IStoredProcedureParameter"/>s
        /// to pass, the output action map, and the <see cref="IDataTransformer"/>s to 
        /// use to transform the results.
        /// </summary>
        /// <param name="schema">The schema of the stored procedure.</param>
        /// <param name="name">The name of the stored procedure.</param>
        /// <param name="parameters">The <see cref="IStoredProcedureParameter"/>s to pass to the stored procedure.</param>
        /// <param name="dataTransformers">The <see cref="IDataTransformer"/>s to transform the results.</param>
		protected StoredProcedure(string schema, 
            string name,
            IEnumerable<IStoredProcedureParameter> parameters,
			IEnumerable<IDataTransformer> dataTransformers)
			: base(schema, name, parameters, dataTransformers)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
			Contract.Requires(parameters       != null);
			Contract.Requires(dataTransformers != null);
		}
	
        /// <summary>
        /// Executes the stored procedure.
        /// </summary>
        /// <param name="connection">The connection to use to execute the StoredProcedure.</param>
        /// <param name="timeout">The number of seconds to wait before aborting the 
        /// stored procedure's execution.</param>
		/// <returns>The results from the stored procedure.</returns>
        /// <example>If using from an Entity Framework DbContext, the connection can be passed:
        /// <code language='cs'>
        /// var results = storedProcedure.Execute(this.Database.Connection);
        /// </code>
        /// </example>
		public new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>> Execute(IDbConnection connection, int timeout = defaultTimeout)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>>() != null);

			return Execute(connection, CancellationToken.None, timeout);
		}
		
		private Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>> Execute(IDbConnection connection, CancellationToken token, int timeout = defaultTimeout)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>>() != null);

			Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>> results;
			using (var cmd = connection.CreateCommand(Schema, Name, timeout, out connection))
            {
				var dbParameters = AddParameters(cmd);

				token.ThrowIfCancellationRequested();

				var reader = cmd.ExecuteReader();
				var t1 = T1Factory.ParseRows(reader, DataTransformers, token);

				reader.NextResult();
				token.ThrowIfCancellationRequested();

				var t2 = T2Factory.ParseRows(reader, DataTransformers, token);

				reader.NextResult();
				token.ThrowIfCancellationRequested();

				var t3 = T3Factory.ParseRows(reader, DataTransformers, token);
				results = Tuple.Create(t1, t2, t3);

				token.ThrowIfCancellationRequested();

				TransferOutputParameters(CancellationToken.None, dbParameters);
            }

            if (connection != null)
                connection.Close();
			
			return results;
		}
		
        /// <summary>
        /// Executes the StoredProcedure asynchronously.
        /// </summary>
        /// <param name="connection">The connection to use to execute the StoredProcedure.</param>
        /// <param name="timeout">The number of seconds to wait before aborting the 
        /// stored procedure's execution.</param>
        /// <returns>A Task&lt;Tuple&lt;IEnumerable&lt;T1&gt;, IEnumerable&lt;T2&gt;, IEnumerable&lt;T3&gt;&gt;&gt; that will be completed when the StoredProcedure is finished executing.</returns>
        /// <example>If using from an Entity Framework DbContext, the connection can be passed:
        /// <code language='cs'>
        /// var results = await storedProcedure.ExecuteAsync(this.Database.Connection);
        /// </code>
        /// </example>
		public new Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>> ExecuteAsync(IDbConnection connection, int timeout = defaultTimeout)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>>>() != null);

			return ExecuteAsync(connection, CancellationToken.None, timeout);
		}
		
        /// <summary>
        /// Executes the StoredProcedure asynchronously.
        /// </summary>
        /// <param name="connection">The connection to use to execute the StoredProcedure.</param>
        /// <param name="token">The <see cref="CancellationToken"/> to use to cancel the execution of the StoredProcedure.</param>
        /// <param name="timeout">The number of seconds to wait before aborting the 
        /// stored procedure's execution.</param>
        /// <returns>A Task&lt;Tuple&lt;IEnumerable&lt;T1&gt;, IEnumerable&lt;T2&gt;, IEnumerable&lt;T3&gt;&gt;&gt; that will be completed when the StoredProcedure is finished executing.</returns>
        /// <example>If using from an Entity Framework DbContext, the connection can be passed:
        /// <code language='cs'>
		/// var cts     = new CancellationTokenSource();
        /// var results = await storedProcedure.ExecuteAsync(this.Database.Connection, cts.Token);
        /// </code>
        /// </example>
		public new Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>> ExecuteAsync(IDbConnection connection, CancellationToken token, int timeout = defaultTimeout)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>>>() != null);
			
#if !NET40
            var baseClass = connection as DbConnection;
            if (baseClass != null)
            {
                IDbConnection toClose;
                var cmd = connection.CreateCommand(Schema, Name, timeout, out toClose);
                var asyncCapable = cmd as DbCommand;
                return ExecuteAsync(asyncCapable, toClose, token);
            }
#endif

			return Task.Factory.StartNew(
				() => Execute(connection, token, timeout), 
				token,
                TaskCreationOptions.None,
                TaskScheduler.Default);
		}
		
#if !NET40
		async Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>> ExecuteAsync(DbCommand cmd, IDbConnection toClose, CancellationToken token)
		{
			Contract.Requires(cmd != null);
			Contract.Ensures(Contract.Result<Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>>>() != null);

			Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>> results;
            var dbParameters = AddParameters(cmd);
			
            token.ThrowIfCancellationRequested();
			var reader = await cmd.ExecuteReaderAsync(token);
			var t1 = await T1Factory.ParseRowsAsync(reader, DataTransformers, token);
			await reader.NextResultAsync(token);
			var t2 = await T2Factory.ParseRowsAsync(reader, DataTransformers, token);
			await reader.NextResultAsync(token);
			var t3 = await T3Factory.ParseRowsAsync(reader, DataTransformers, token);
			results = Tuple.Create(t1, t2, t3);

            token.ThrowIfCancellationRequested();
			TransferOutputParameters(token, dbParameters);

			if (toClose != null)
				toClose.Close();

			cmd.Dispose();

			return results;
		}
#endif

		
        /// <summary>
        /// Clones the StoredProcedure, and gives it the passed parameters.
        /// </summary>
        /// <param name="parameters">The <see cref="IStoredProcedureParameter"/>s to pass to the stored procedure.</param>
        /// <param name="dataTransformers">The <see cref="IDataTransformer"/>s to transform the results.</param>
        /// <returns>A clone of the stored procedure.</returns>
		protected internal override StoredProcedure CloneCore(
			IEnumerable<IStoredProcedureParameter> parameters,
			IEnumerable<IDataTransformer>          dataTransformers)
		{
			return new StoredProcedure<T1, T2, T3>(Schema, Name, parameters, dataTransformers);
		}	
	}
	#endregion

	#region StoredProcedure<T1, T2, T3, T4>
	/// <summary>Calls a StoredProcedure that returns 4 result set(s).</summary>
	/// <typeparam name="T1">The type of the first result set returned by the stored procedure.</typeparam>
	/// <typeparam name="T2">The type of the second result set returned by the stored procedure.</typeparam>
	/// <typeparam name="T3">The type of the third result set returned by the stored procedure.</typeparam>
	/// <typeparam name="T4">The type of the fourth result set returned by the stored procedure.</typeparam>
	public class StoredProcedure<T1, T2, T3, T4> : StoredProcedure<T1, T2, T3>	{
				private IRowFactory<T4> factory;

		internal IRowFactory<T4> T4Factory 
		{
			get
			{
				Contract.Ensures(Contract.Result<IRowFactory<T4>>() != null);
				
				if (factory == null)
					factory = RowFactory<T4>.Create(isSingleResult);

				return factory;
			}
		}

        /// <summary>
        /// Creates a <see cref="StoredProcedure"/> with the given <paramref name="name"/>
        /// in the dbo schema.
        /// </summary>
        /// <param name="name">The name of the stored procedure.</param>
		public StoredProcedure(string name) : base(name)
		{ 
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
			Contract.Requires(typeof(T1).IsValidResultType());
			Contract.Requires(typeof(T2).IsValidResultType());
			Contract.Requires(typeof(T3).IsValidResultType());
			Contract.Requires(typeof(T4).IsValidResultType());			isSingleResult = false;
	
		}
		
        /// <summary>
        /// Creates a <see cref="StoredProcedure"/> with the given <paramref name="name"/>
        /// in the <paramref name="schema"/> schema.
        /// </summary>
        /// <param name="schema">The schema of the stored procedure.</param>
        /// <param name="name">The name of the stored procedure.</param>
		public StoredProcedure(string schema, string name) : base(schema, name)
		{ 
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
			Contract.Requires(typeof(T1).IsValidResultType());
			Contract.Requires(typeof(T2).IsValidResultType());
			Contract.Requires(typeof(T3).IsValidResultType());
			Contract.Requires(typeof(T4).IsValidResultType());	
			isSingleResult = false;
		}
		
		internal StoredProcedure(StoredProcedure toClone)
			: base(toClone.Schema, toClone.Name, toClone.Parameters, toClone.DataTransformers) 
		{ 
			Contract.Requires(toClone != null);
			isSingleResult = false;
		}
				
        /// <summary>
        /// Creates a <see cref="StoredProcedure"/> with the given <paramref name="name"/>
        /// in the <paramref name="schema"/> schema, with the <see cref="IStoredProcedureParameter"/>s
        /// to pass, the output action map, and the <see cref="IDataTransformer"/>s to 
        /// use to transform the results.
        /// </summary>
        /// <param name="schema">The schema of the stored procedure.</param>
        /// <param name="name">The name of the stored procedure.</param>
        /// <param name="parameters">The <see cref="IStoredProcedureParameter"/>s to pass to the stored procedure.</param>
        /// <param name="dataTransformers">The <see cref="IDataTransformer"/>s to transform the results.</param>
		protected StoredProcedure(string schema, 
            string name,
            IEnumerable<IStoredProcedureParameter> parameters,
			IEnumerable<IDataTransformer> dataTransformers)
			: base(schema, name, parameters, dataTransformers)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
			Contract.Requires(parameters       != null);
			Contract.Requires(dataTransformers != null);
		}
	
        /// <summary>
        /// Executes the stored procedure.
        /// </summary>
        /// <param name="connection">The connection to use to execute the StoredProcedure.</param>
        /// <param name="timeout">The number of seconds to wait before aborting the 
        /// stored procedure's execution.</param>
		/// <returns>The results from the stored procedure.</returns>
        /// <example>If using from an Entity Framework DbContext, the connection can be passed:
        /// <code language='cs'>
        /// var results = storedProcedure.Execute(this.Database.Connection);
        /// </code>
        /// </example>
		public new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>> Execute(IDbConnection connection, int timeout = defaultTimeout)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>>() != null);

			return Execute(connection, CancellationToken.None, timeout);
		}
		
		private Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>> Execute(IDbConnection connection, CancellationToken token, int timeout = defaultTimeout)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>>() != null);

			Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>> results;
			using (var cmd = connection.CreateCommand(Schema, Name, timeout, out connection))
            {
				var dbParameters = AddParameters(cmd);

				token.ThrowIfCancellationRequested();

				var reader = cmd.ExecuteReader();
				var t1 = T1Factory.ParseRows(reader, DataTransformers, token);

				reader.NextResult();
				token.ThrowIfCancellationRequested();

				var t2 = T2Factory.ParseRows(reader, DataTransformers, token);

				reader.NextResult();
				token.ThrowIfCancellationRequested();

				var t3 = T3Factory.ParseRows(reader, DataTransformers, token);

				reader.NextResult();
				token.ThrowIfCancellationRequested();

				var t4 = T4Factory.ParseRows(reader, DataTransformers, token);
				results = Tuple.Create(t1, t2, t3, t4);

				token.ThrowIfCancellationRequested();

				TransferOutputParameters(CancellationToken.None, dbParameters);
            }

            if (connection != null)
                connection.Close();
			
			return results;
		}
		
        /// <summary>
        /// Executes the StoredProcedure asynchronously.
        /// </summary>
        /// <param name="connection">The connection to use to execute the StoredProcedure.</param>
        /// <param name="timeout">The number of seconds to wait before aborting the 
        /// stored procedure's execution.</param>
        /// <returns>A Task&lt;Tuple&lt;IEnumerable&lt;T1&gt;, IEnumerable&lt;T2&gt;, IEnumerable&lt;T3&gt;, IEnumerable&lt;T4&gt;&gt;&gt; that will be completed when the StoredProcedure is finished executing.</returns>
        /// <example>If using from an Entity Framework DbContext, the connection can be passed:
        /// <code language='cs'>
        /// var results = await storedProcedure.ExecuteAsync(this.Database.Connection);
        /// </code>
        /// </example>
		public new Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>> ExecuteAsync(IDbConnection connection, int timeout = defaultTimeout)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>>>() != null);

			return ExecuteAsync(connection, CancellationToken.None, timeout);
		}
		
        /// <summary>
        /// Executes the StoredProcedure asynchronously.
        /// </summary>
        /// <param name="connection">The connection to use to execute the StoredProcedure.</param>
        /// <param name="token">The <see cref="CancellationToken"/> to use to cancel the execution of the StoredProcedure.</param>
        /// <param name="timeout">The number of seconds to wait before aborting the 
        /// stored procedure's execution.</param>
        /// <returns>A Task&lt;Tuple&lt;IEnumerable&lt;T1&gt;, IEnumerable&lt;T2&gt;, IEnumerable&lt;T3&gt;, IEnumerable&lt;T4&gt;&gt;&gt; that will be completed when the StoredProcedure is finished executing.</returns>
        /// <example>If using from an Entity Framework DbContext, the connection can be passed:
        /// <code language='cs'>
		/// var cts     = new CancellationTokenSource();
        /// var results = await storedProcedure.ExecuteAsync(this.Database.Connection, cts.Token);
        /// </code>
        /// </example>
		public new Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>> ExecuteAsync(IDbConnection connection, CancellationToken token, int timeout = defaultTimeout)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>>>() != null);
			
#if !NET40
            var baseClass = connection as DbConnection;
            if (baseClass != null)
            {
                IDbConnection toClose;
                var cmd = connection.CreateCommand(Schema, Name, timeout, out toClose);
                var asyncCapable = cmd as DbCommand;
                return ExecuteAsync(asyncCapable, toClose, token);
            }
#endif

			return Task.Factory.StartNew(
				() => Execute(connection, token, timeout), 
				token,
                TaskCreationOptions.None,
                TaskScheduler.Default);
		}
		
#if !NET40
		async Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>> ExecuteAsync(DbCommand cmd, IDbConnection toClose, CancellationToken token)
		{
			Contract.Requires(cmd != null);
			Contract.Ensures(Contract.Result<Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>>>() != null);

			Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>> results;
            var dbParameters = AddParameters(cmd);
			
            token.ThrowIfCancellationRequested();
			var reader = await cmd.ExecuteReaderAsync(token);
			var t1 = await T1Factory.ParseRowsAsync(reader, DataTransformers, token);
			await reader.NextResultAsync(token);
			var t2 = await T2Factory.ParseRowsAsync(reader, DataTransformers, token);
			await reader.NextResultAsync(token);
			var t3 = await T3Factory.ParseRowsAsync(reader, DataTransformers, token);
			await reader.NextResultAsync(token);
			var t4 = await T4Factory.ParseRowsAsync(reader, DataTransformers, token);
			results = Tuple.Create(t1, t2, t3, t4);

            token.ThrowIfCancellationRequested();
			TransferOutputParameters(token, dbParameters);

			if (toClose != null)
				toClose.Close();

			cmd.Dispose();

			return results;
		}
#endif

		
        /// <summary>
        /// Clones the StoredProcedure, and gives it the passed parameters.
        /// </summary>
        /// <param name="parameters">The <see cref="IStoredProcedureParameter"/>s to pass to the stored procedure.</param>
        /// <param name="dataTransformers">The <see cref="IDataTransformer"/>s to transform the results.</param>
        /// <returns>A clone of the stored procedure.</returns>
		protected internal override StoredProcedure CloneCore(
			IEnumerable<IStoredProcedureParameter> parameters,
			IEnumerable<IDataTransformer>          dataTransformers)
		{
			return new StoredProcedure<T1, T2, T3, T4>(Schema, Name, parameters, dataTransformers);
		}	
	}
	#endregion

	#region StoredProcedure<T1, T2, T3, T4, T5>
	/// <summary>Calls a StoredProcedure that returns 5 result set(s).</summary>
	/// <typeparam name="T1">The type of the first result set returned by the stored procedure.</typeparam>
	/// <typeparam name="T2">The type of the second result set returned by the stored procedure.</typeparam>
	/// <typeparam name="T3">The type of the third result set returned by the stored procedure.</typeparam>
	/// <typeparam name="T4">The type of the fourth result set returned by the stored procedure.</typeparam>
	/// <typeparam name="T5">The type of the fifth result set returned by the stored procedure.</typeparam>
	public class StoredProcedure<T1, T2, T3, T4, T5> : StoredProcedure<T1, T2, T3, T4>	{
				private IRowFactory<T5> factory;

		internal IRowFactory<T5> T5Factory 
		{
			get
			{
				Contract.Ensures(Contract.Result<IRowFactory<T5>>() != null);
				
				if (factory == null)
					factory = RowFactory<T5>.Create(isSingleResult);

				return factory;
			}
		}

        /// <summary>
        /// Creates a <see cref="StoredProcedure"/> with the given <paramref name="name"/>
        /// in the dbo schema.
        /// </summary>
        /// <param name="name">The name of the stored procedure.</param>
		public StoredProcedure(string name) : base(name)
		{ 
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
			Contract.Requires(typeof(T1).IsValidResultType());
			Contract.Requires(typeof(T2).IsValidResultType());
			Contract.Requires(typeof(T3).IsValidResultType());
			Contract.Requires(typeof(T4).IsValidResultType());
			Contract.Requires(typeof(T5).IsValidResultType());			isSingleResult = false;
	
		}
		
        /// <summary>
        /// Creates a <see cref="StoredProcedure"/> with the given <paramref name="name"/>
        /// in the <paramref name="schema"/> schema.
        /// </summary>
        /// <param name="schema">The schema of the stored procedure.</param>
        /// <param name="name">The name of the stored procedure.</param>
		public StoredProcedure(string schema, string name) : base(schema, name)
		{ 
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
			Contract.Requires(typeof(T1).IsValidResultType());
			Contract.Requires(typeof(T2).IsValidResultType());
			Contract.Requires(typeof(T3).IsValidResultType());
			Contract.Requires(typeof(T4).IsValidResultType());
			Contract.Requires(typeof(T5).IsValidResultType());	
			isSingleResult = false;
		}
		
		internal StoredProcedure(StoredProcedure toClone)
			: base(toClone.Schema, toClone.Name, toClone.Parameters, toClone.DataTransformers) 
		{ 
			Contract.Requires(toClone != null);
			isSingleResult = false;
		}
				
        /// <summary>
        /// Creates a <see cref="StoredProcedure"/> with the given <paramref name="name"/>
        /// in the <paramref name="schema"/> schema, with the <see cref="IStoredProcedureParameter"/>s
        /// to pass, the output action map, and the <see cref="IDataTransformer"/>s to 
        /// use to transform the results.
        /// </summary>
        /// <param name="schema">The schema of the stored procedure.</param>
        /// <param name="name">The name of the stored procedure.</param>
        /// <param name="parameters">The <see cref="IStoredProcedureParameter"/>s to pass to the stored procedure.</param>
        /// <param name="dataTransformers">The <see cref="IDataTransformer"/>s to transform the results.</param>
		protected StoredProcedure(string schema, 
            string name,
            IEnumerable<IStoredProcedureParameter> parameters,
			IEnumerable<IDataTransformer> dataTransformers)
			: base(schema, name, parameters, dataTransformers)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
			Contract.Requires(parameters       != null);
			Contract.Requires(dataTransformers != null);
		}
	
        /// <summary>
        /// Executes the stored procedure.
        /// </summary>
        /// <param name="connection">The connection to use to execute the StoredProcedure.</param>
        /// <param name="timeout">The number of seconds to wait before aborting the 
        /// stored procedure's execution.</param>
		/// <returns>The results from the stored procedure.</returns>
        /// <example>If using from an Entity Framework DbContext, the connection can be passed:
        /// <code language='cs'>
        /// var results = storedProcedure.Execute(this.Database.Connection);
        /// </code>
        /// </example>
		public new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>> Execute(IDbConnection connection, int timeout = defaultTimeout)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>>() != null);

			return Execute(connection, CancellationToken.None, timeout);
		}
		
		private Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>> Execute(IDbConnection connection, CancellationToken token, int timeout = defaultTimeout)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>>() != null);

			Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>> results;
			using (var cmd = connection.CreateCommand(Schema, Name, timeout, out connection))
            {
				var dbParameters = AddParameters(cmd);

				token.ThrowIfCancellationRequested();

				var reader = cmd.ExecuteReader();
				var t1 = T1Factory.ParseRows(reader, DataTransformers, token);

				reader.NextResult();
				token.ThrowIfCancellationRequested();

				var t2 = T2Factory.ParseRows(reader, DataTransformers, token);

				reader.NextResult();
				token.ThrowIfCancellationRequested();

				var t3 = T3Factory.ParseRows(reader, DataTransformers, token);

				reader.NextResult();
				token.ThrowIfCancellationRequested();

				var t4 = T4Factory.ParseRows(reader, DataTransformers, token);

				reader.NextResult();
				token.ThrowIfCancellationRequested();

				var t5 = T5Factory.ParseRows(reader, DataTransformers, token);
				results = Tuple.Create(t1, t2, t3, t4, t5);

				token.ThrowIfCancellationRequested();

				TransferOutputParameters(CancellationToken.None, dbParameters);
            }

            if (connection != null)
                connection.Close();
			
			return results;
		}
		
        /// <summary>
        /// Executes the StoredProcedure asynchronously.
        /// </summary>
        /// <param name="connection">The connection to use to execute the StoredProcedure.</param>
        /// <param name="timeout">The number of seconds to wait before aborting the 
        /// stored procedure's execution.</param>
        /// <returns>A Task&lt;Tuple&lt;IEnumerable&lt;T1&gt;, IEnumerable&lt;T2&gt;, IEnumerable&lt;T3&gt;, IEnumerable&lt;T4&gt;, IEnumerable&lt;T5&gt;&gt;&gt; that will be completed when the StoredProcedure is finished executing.</returns>
        /// <example>If using from an Entity Framework DbContext, the connection can be passed:
        /// <code language='cs'>
        /// var results = await storedProcedure.ExecuteAsync(this.Database.Connection);
        /// </code>
        /// </example>
		public new Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>> ExecuteAsync(IDbConnection connection, int timeout = defaultTimeout)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>>>() != null);

			return ExecuteAsync(connection, CancellationToken.None, timeout);
		}
		
        /// <summary>
        /// Executes the StoredProcedure asynchronously.
        /// </summary>
        /// <param name="connection">The connection to use to execute the StoredProcedure.</param>
        /// <param name="token">The <see cref="CancellationToken"/> to use to cancel the execution of the StoredProcedure.</param>
        /// <param name="timeout">The number of seconds to wait before aborting the 
        /// stored procedure's execution.</param>
        /// <returns>A Task&lt;Tuple&lt;IEnumerable&lt;T1&gt;, IEnumerable&lt;T2&gt;, IEnumerable&lt;T3&gt;, IEnumerable&lt;T4&gt;, IEnumerable&lt;T5&gt;&gt;&gt; that will be completed when the StoredProcedure is finished executing.</returns>
        /// <example>If using from an Entity Framework DbContext, the connection can be passed:
        /// <code language='cs'>
		/// var cts     = new CancellationTokenSource();
        /// var results = await storedProcedure.ExecuteAsync(this.Database.Connection, cts.Token);
        /// </code>
        /// </example>
		public new Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>> ExecuteAsync(IDbConnection connection, CancellationToken token, int timeout = defaultTimeout)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>>>() != null);
			
#if !NET40
            var baseClass = connection as DbConnection;
            if (baseClass != null)
            {
                IDbConnection toClose;
                var cmd = connection.CreateCommand(Schema, Name, timeout, out toClose);
                var asyncCapable = cmd as DbCommand;
                return ExecuteAsync(asyncCapable, toClose, token);
            }
#endif

			return Task.Factory.StartNew(
				() => Execute(connection, token, timeout), 
				token,
                TaskCreationOptions.None,
                TaskScheduler.Default);
		}
		
#if !NET40
		async Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>> ExecuteAsync(DbCommand cmd, IDbConnection toClose, CancellationToken token)
		{
			Contract.Requires(cmd != null);
			Contract.Ensures(Contract.Result<Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>>>() != null);

			Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>> results;
            var dbParameters = AddParameters(cmd);
			
            token.ThrowIfCancellationRequested();
			var reader = await cmd.ExecuteReaderAsync(token);
			var t1 = await T1Factory.ParseRowsAsync(reader, DataTransformers, token);
			await reader.NextResultAsync(token);
			var t2 = await T2Factory.ParseRowsAsync(reader, DataTransformers, token);
			await reader.NextResultAsync(token);
			var t3 = await T3Factory.ParseRowsAsync(reader, DataTransformers, token);
			await reader.NextResultAsync(token);
			var t4 = await T4Factory.ParseRowsAsync(reader, DataTransformers, token);
			await reader.NextResultAsync(token);
			var t5 = await T5Factory.ParseRowsAsync(reader, DataTransformers, token);
			results = Tuple.Create(t1, t2, t3, t4, t5);

            token.ThrowIfCancellationRequested();
			TransferOutputParameters(token, dbParameters);

			if (toClose != null)
				toClose.Close();

			cmd.Dispose();

			return results;
		}
#endif

		
        /// <summary>
        /// Clones the StoredProcedure, and gives it the passed parameters.
        /// </summary>
        /// <param name="parameters">The <see cref="IStoredProcedureParameter"/>s to pass to the stored procedure.</param>
        /// <param name="dataTransformers">The <see cref="IDataTransformer"/>s to transform the results.</param>
        /// <returns>A clone of the stored procedure.</returns>
		protected internal override StoredProcedure CloneCore(
			IEnumerable<IStoredProcedureParameter> parameters,
			IEnumerable<IDataTransformer>          dataTransformers)
		{
			return new StoredProcedure<T1, T2, T3, T4, T5>(Schema, Name, parameters, dataTransformers);
		}	
	}
	#endregion

	#region StoredProcedure<T1, T2, T3, T4, T5, T6>
	/// <summary>Calls a StoredProcedure that returns 6 result set(s).</summary>
	/// <typeparam name="T1">The type of the first result set returned by the stored procedure.</typeparam>
	/// <typeparam name="T2">The type of the second result set returned by the stored procedure.</typeparam>
	/// <typeparam name="T3">The type of the third result set returned by the stored procedure.</typeparam>
	/// <typeparam name="T4">The type of the fourth result set returned by the stored procedure.</typeparam>
	/// <typeparam name="T5">The type of the fifth result set returned by the stored procedure.</typeparam>
	/// <typeparam name="T6">The type of the sixth result set returned by the stored procedure.</typeparam>
	public class StoredProcedure<T1, T2, T3, T4, T5, T6> : StoredProcedure<T1, T2, T3, T4, T5>	{
				private IRowFactory<T6> factory;

		internal IRowFactory<T6> T6Factory 
		{
			get
			{
				Contract.Ensures(Contract.Result<IRowFactory<T6>>() != null);
				
				if (factory == null)
					factory = RowFactory<T6>.Create(isSingleResult);

				return factory;
			}
		}

        /// <summary>
        /// Creates a <see cref="StoredProcedure"/> with the given <paramref name="name"/>
        /// in the dbo schema.
        /// </summary>
        /// <param name="name">The name of the stored procedure.</param>
		public StoredProcedure(string name) : base(name)
		{ 
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
			Contract.Requires(typeof(T1).IsValidResultType());
			Contract.Requires(typeof(T2).IsValidResultType());
			Contract.Requires(typeof(T3).IsValidResultType());
			Contract.Requires(typeof(T4).IsValidResultType());
			Contract.Requires(typeof(T5).IsValidResultType());
			Contract.Requires(typeof(T6).IsValidResultType());			isSingleResult = false;
	
		}
		
        /// <summary>
        /// Creates a <see cref="StoredProcedure"/> with the given <paramref name="name"/>
        /// in the <paramref name="schema"/> schema.
        /// </summary>
        /// <param name="schema">The schema of the stored procedure.</param>
        /// <param name="name">The name of the stored procedure.</param>
		public StoredProcedure(string schema, string name) : base(schema, name)
		{ 
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
			Contract.Requires(typeof(T1).IsValidResultType());
			Contract.Requires(typeof(T2).IsValidResultType());
			Contract.Requires(typeof(T3).IsValidResultType());
			Contract.Requires(typeof(T4).IsValidResultType());
			Contract.Requires(typeof(T5).IsValidResultType());
			Contract.Requires(typeof(T6).IsValidResultType());	
			isSingleResult = false;
		}
		
		internal StoredProcedure(StoredProcedure toClone)
			: base(toClone.Schema, toClone.Name, toClone.Parameters, toClone.DataTransformers) 
		{ 
			Contract.Requires(toClone != null);
			isSingleResult = false;
		}
				
        /// <summary>
        /// Creates a <see cref="StoredProcedure"/> with the given <paramref name="name"/>
        /// in the <paramref name="schema"/> schema, with the <see cref="IStoredProcedureParameter"/>s
        /// to pass, the output action map, and the <see cref="IDataTransformer"/>s to 
        /// use to transform the results.
        /// </summary>
        /// <param name="schema">The schema of the stored procedure.</param>
        /// <param name="name">The name of the stored procedure.</param>
        /// <param name="parameters">The <see cref="IStoredProcedureParameter"/>s to pass to the stored procedure.</param>
        /// <param name="dataTransformers">The <see cref="IDataTransformer"/>s to transform the results.</param>
		protected StoredProcedure(string schema, 
            string name,
            IEnumerable<IStoredProcedureParameter> parameters,
			IEnumerable<IDataTransformer> dataTransformers)
			: base(schema, name, parameters, dataTransformers)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
			Contract.Requires(parameters       != null);
			Contract.Requires(dataTransformers != null);
		}
	
        /// <summary>
        /// Executes the stored procedure.
        /// </summary>
        /// <param name="connection">The connection to use to execute the StoredProcedure.</param>
        /// <param name="timeout">The number of seconds to wait before aborting the 
        /// stored procedure's execution.</param>
		/// <returns>The results from the stored procedure.</returns>
        /// <example>If using from an Entity Framework DbContext, the connection can be passed:
        /// <code language='cs'>
        /// var results = storedProcedure.Execute(this.Database.Connection);
        /// </code>
        /// </example>
		public new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>> Execute(IDbConnection connection, int timeout = defaultTimeout)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>>() != null);

			return Execute(connection, CancellationToken.None, timeout);
		}
		
		private Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>> Execute(IDbConnection connection, CancellationToken token, int timeout = defaultTimeout)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>>() != null);

			Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>> results;
			using (var cmd = connection.CreateCommand(Schema, Name, timeout, out connection))
            {
				var dbParameters = AddParameters(cmd);

				token.ThrowIfCancellationRequested();

				var reader = cmd.ExecuteReader();
				var t1 = T1Factory.ParseRows(reader, DataTransformers, token);

				reader.NextResult();
				token.ThrowIfCancellationRequested();

				var t2 = T2Factory.ParseRows(reader, DataTransformers, token);

				reader.NextResult();
				token.ThrowIfCancellationRequested();

				var t3 = T3Factory.ParseRows(reader, DataTransformers, token);

				reader.NextResult();
				token.ThrowIfCancellationRequested();

				var t4 = T4Factory.ParseRows(reader, DataTransformers, token);

				reader.NextResult();
				token.ThrowIfCancellationRequested();

				var t5 = T5Factory.ParseRows(reader, DataTransformers, token);

				reader.NextResult();
				token.ThrowIfCancellationRequested();

				var t6 = T6Factory.ParseRows(reader, DataTransformers, token);
				results = Tuple.Create(t1, t2, t3, t4, t5, t6);

				token.ThrowIfCancellationRequested();

				TransferOutputParameters(CancellationToken.None, dbParameters);
            }

            if (connection != null)
                connection.Close();
			
			return results;
		}
		
        /// <summary>
        /// Executes the StoredProcedure asynchronously.
        /// </summary>
        /// <param name="connection">The connection to use to execute the StoredProcedure.</param>
        /// <param name="timeout">The number of seconds to wait before aborting the 
        /// stored procedure's execution.</param>
        /// <returns>A Task&lt;Tuple&lt;IEnumerable&lt;T1&gt;, IEnumerable&lt;T2&gt;, IEnumerable&lt;T3&gt;, IEnumerable&lt;T4&gt;, IEnumerable&lt;T5&gt;, IEnumerable&lt;T6&gt;&gt;&gt; that will be completed when the StoredProcedure is finished executing.</returns>
        /// <example>If using from an Entity Framework DbContext, the connection can be passed:
        /// <code language='cs'>
        /// var results = await storedProcedure.ExecuteAsync(this.Database.Connection);
        /// </code>
        /// </example>
		public new Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>> ExecuteAsync(IDbConnection connection, int timeout = defaultTimeout)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>>>() != null);

			return ExecuteAsync(connection, CancellationToken.None, timeout);
		}
		
        /// <summary>
        /// Executes the StoredProcedure asynchronously.
        /// </summary>
        /// <param name="connection">The connection to use to execute the StoredProcedure.</param>
        /// <param name="token">The <see cref="CancellationToken"/> to use to cancel the execution of the StoredProcedure.</param>
        /// <param name="timeout">The number of seconds to wait before aborting the 
        /// stored procedure's execution.</param>
        /// <returns>A Task&lt;Tuple&lt;IEnumerable&lt;T1&gt;, IEnumerable&lt;T2&gt;, IEnumerable&lt;T3&gt;, IEnumerable&lt;T4&gt;, IEnumerable&lt;T5&gt;, IEnumerable&lt;T6&gt;&gt;&gt; that will be completed when the StoredProcedure is finished executing.</returns>
        /// <example>If using from an Entity Framework DbContext, the connection can be passed:
        /// <code language='cs'>
		/// var cts     = new CancellationTokenSource();
        /// var results = await storedProcedure.ExecuteAsync(this.Database.Connection, cts.Token);
        /// </code>
        /// </example>
		public new Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>> ExecuteAsync(IDbConnection connection, CancellationToken token, int timeout = defaultTimeout)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>>>() != null);
			
#if !NET40
            var baseClass = connection as DbConnection;
            if (baseClass != null)
            {
                IDbConnection toClose;
                var cmd = connection.CreateCommand(Schema, Name, timeout, out toClose);
                var asyncCapable = cmd as DbCommand;
                return ExecuteAsync(asyncCapable, toClose, token);
            }
#endif

			return Task.Factory.StartNew(
				() => Execute(connection, token, timeout), 
				token,
                TaskCreationOptions.None,
                TaskScheduler.Default);
		}
		
#if !NET40
		async Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>> ExecuteAsync(DbCommand cmd, IDbConnection toClose, CancellationToken token)
		{
			Contract.Requires(cmd != null);
			Contract.Ensures(Contract.Result<Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>>>() != null);

			Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>> results;
            var dbParameters = AddParameters(cmd);
			
            token.ThrowIfCancellationRequested();
			var reader = await cmd.ExecuteReaderAsync(token);
			var t1 = await T1Factory.ParseRowsAsync(reader, DataTransformers, token);
			await reader.NextResultAsync(token);
			var t2 = await T2Factory.ParseRowsAsync(reader, DataTransformers, token);
			await reader.NextResultAsync(token);
			var t3 = await T3Factory.ParseRowsAsync(reader, DataTransformers, token);
			await reader.NextResultAsync(token);
			var t4 = await T4Factory.ParseRowsAsync(reader, DataTransformers, token);
			await reader.NextResultAsync(token);
			var t5 = await T5Factory.ParseRowsAsync(reader, DataTransformers, token);
			await reader.NextResultAsync(token);
			var t6 = await T6Factory.ParseRowsAsync(reader, DataTransformers, token);
			results = Tuple.Create(t1, t2, t3, t4, t5, t6);

            token.ThrowIfCancellationRequested();
			TransferOutputParameters(token, dbParameters);

			if (toClose != null)
				toClose.Close();

			cmd.Dispose();

			return results;
		}
#endif

		
        /// <summary>
        /// Clones the StoredProcedure, and gives it the passed parameters.
        /// </summary>
        /// <param name="parameters">The <see cref="IStoredProcedureParameter"/>s to pass to the stored procedure.</param>
        /// <param name="dataTransformers">The <see cref="IDataTransformer"/>s to transform the results.</param>
        /// <returns>A clone of the stored procedure.</returns>
		protected internal override StoredProcedure CloneCore(
			IEnumerable<IStoredProcedureParameter> parameters,
			IEnumerable<IDataTransformer>          dataTransformers)
		{
			return new StoredProcedure<T1, T2, T3, T4, T5, T6>(Schema, Name, parameters, dataTransformers);
		}	
	}
	#endregion

	#region StoredProcedure<T1, T2, T3, T4, T5, T6, T7>
	/// <summary>Calls a StoredProcedure that returns 7 result set(s).</summary>
	/// <typeparam name="T1">The type of the first result set returned by the stored procedure.</typeparam>
	/// <typeparam name="T2">The type of the second result set returned by the stored procedure.</typeparam>
	/// <typeparam name="T3">The type of the third result set returned by the stored procedure.</typeparam>
	/// <typeparam name="T4">The type of the fourth result set returned by the stored procedure.</typeparam>
	/// <typeparam name="T5">The type of the fifth result set returned by the stored procedure.</typeparam>
	/// <typeparam name="T6">The type of the sixth result set returned by the stored procedure.</typeparam>
	/// <typeparam name="T7">The type of the seventh result set returned by the stored procedure.</typeparam>
	public class StoredProcedure<T1, T2, T3, T4, T5, T6, T7> : StoredProcedure<T1, T2, T3, T4, T5, T6>	{
				private IRowFactory<T7> factory;

		internal IRowFactory<T7> T7Factory 
		{
			get
			{
				Contract.Ensures(Contract.Result<IRowFactory<T7>>() != null);
				
				if (factory == null)
					factory = RowFactory<T7>.Create(isSingleResult);

				return factory;
			}
		}

        /// <summary>
        /// Creates a <see cref="StoredProcedure"/> with the given <paramref name="name"/>
        /// in the dbo schema.
        /// </summary>
        /// <param name="name">The name of the stored procedure.</param>
		public StoredProcedure(string name) : base(name)
		{ 
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
			Contract.Requires(typeof(T1).IsValidResultType());
			Contract.Requires(typeof(T2).IsValidResultType());
			Contract.Requires(typeof(T3).IsValidResultType());
			Contract.Requires(typeof(T4).IsValidResultType());
			Contract.Requires(typeof(T5).IsValidResultType());
			Contract.Requires(typeof(T6).IsValidResultType());
			Contract.Requires(typeof(T7).IsValidResultType());			isSingleResult = false;
	
		}
		
        /// <summary>
        /// Creates a <see cref="StoredProcedure"/> with the given <paramref name="name"/>
        /// in the <paramref name="schema"/> schema.
        /// </summary>
        /// <param name="schema">The schema of the stored procedure.</param>
        /// <param name="name">The name of the stored procedure.</param>
		public StoredProcedure(string schema, string name) : base(schema, name)
		{ 
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
			Contract.Requires(typeof(T1).IsValidResultType());
			Contract.Requires(typeof(T2).IsValidResultType());
			Contract.Requires(typeof(T3).IsValidResultType());
			Contract.Requires(typeof(T4).IsValidResultType());
			Contract.Requires(typeof(T5).IsValidResultType());
			Contract.Requires(typeof(T6).IsValidResultType());
			Contract.Requires(typeof(T7).IsValidResultType());	
			isSingleResult = false;
		}
		
		internal StoredProcedure(StoredProcedure toClone)
			: base(toClone.Schema, toClone.Name, toClone.Parameters, toClone.DataTransformers) 
		{ 
			Contract.Requires(toClone != null);
			isSingleResult = false;
		}
				
        /// <summary>
        /// Creates a <see cref="StoredProcedure"/> with the given <paramref name="name"/>
        /// in the <paramref name="schema"/> schema, with the <see cref="IStoredProcedureParameter"/>s
        /// to pass, the output action map, and the <see cref="IDataTransformer"/>s to 
        /// use to transform the results.
        /// </summary>
        /// <param name="schema">The schema of the stored procedure.</param>
        /// <param name="name">The name of the stored procedure.</param>
        /// <param name="parameters">The <see cref="IStoredProcedureParameter"/>s to pass to the stored procedure.</param>
        /// <param name="dataTransformers">The <see cref="IDataTransformer"/>s to transform the results.</param>
		protected StoredProcedure(string schema, 
            string name,
            IEnumerable<IStoredProcedureParameter> parameters,
			IEnumerable<IDataTransformer> dataTransformers)
			: base(schema, name, parameters, dataTransformers)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
			Contract.Requires(parameters       != null);
			Contract.Requires(dataTransformers != null);
		}
	
        /// <summary>
        /// Executes the stored procedure.
        /// </summary>
        /// <param name="connection">The connection to use to execute the StoredProcedure.</param>
        /// <param name="timeout">The number of seconds to wait before aborting the 
        /// stored procedure's execution.</param>
		/// <returns>The results from the stored procedure.</returns>
        /// <example>If using from an Entity Framework DbContext, the connection can be passed:
        /// <code language='cs'>
        /// var results = storedProcedure.Execute(this.Database.Connection);
        /// </code>
        /// </example>
		public new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>> Execute(IDbConnection connection, int timeout = defaultTimeout)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>>() != null);

			return Execute(connection, CancellationToken.None, timeout);
		}
		
		private Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>> Execute(IDbConnection connection, CancellationToken token, int timeout = defaultTimeout)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>>() != null);

			Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>> results;
			using (var cmd = connection.CreateCommand(Schema, Name, timeout, out connection))
            {
				var dbParameters = AddParameters(cmd);

				token.ThrowIfCancellationRequested();

				var reader = cmd.ExecuteReader();
				var t1 = T1Factory.ParseRows(reader, DataTransformers, token);

				reader.NextResult();
				token.ThrowIfCancellationRequested();

				var t2 = T2Factory.ParseRows(reader, DataTransformers, token);

				reader.NextResult();
				token.ThrowIfCancellationRequested();

				var t3 = T3Factory.ParseRows(reader, DataTransformers, token);

				reader.NextResult();
				token.ThrowIfCancellationRequested();

				var t4 = T4Factory.ParseRows(reader, DataTransformers, token);

				reader.NextResult();
				token.ThrowIfCancellationRequested();

				var t5 = T5Factory.ParseRows(reader, DataTransformers, token);

				reader.NextResult();
				token.ThrowIfCancellationRequested();

				var t6 = T6Factory.ParseRows(reader, DataTransformers, token);

				reader.NextResult();
				token.ThrowIfCancellationRequested();

				var t7 = T7Factory.ParseRows(reader, DataTransformers, token);
				results = Tuple.Create(t1, t2, t3, t4, t5, t6, t7);

				token.ThrowIfCancellationRequested();

				TransferOutputParameters(CancellationToken.None, dbParameters);
            }

            if (connection != null)
                connection.Close();
			
			return results;
		}
		
        /// <summary>
        /// Executes the StoredProcedure asynchronously.
        /// </summary>
        /// <param name="connection">The connection to use to execute the StoredProcedure.</param>
        /// <param name="timeout">The number of seconds to wait before aborting the 
        /// stored procedure's execution.</param>
        /// <returns>A Task&lt;Tuple&lt;IEnumerable&lt;T1&gt;, IEnumerable&lt;T2&gt;, IEnumerable&lt;T3&gt;, IEnumerable&lt;T4&gt;, IEnumerable&lt;T5&gt;, IEnumerable&lt;T6&gt;, IEnumerable&lt;T7&gt;&gt;&gt; that will be completed when the StoredProcedure is finished executing.</returns>
        /// <example>If using from an Entity Framework DbContext, the connection can be passed:
        /// <code language='cs'>
        /// var results = await storedProcedure.ExecuteAsync(this.Database.Connection);
        /// </code>
        /// </example>
		public new Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>> ExecuteAsync(IDbConnection connection, int timeout = defaultTimeout)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>>>() != null);

			return ExecuteAsync(connection, CancellationToken.None, timeout);
		}
		
        /// <summary>
        /// Executes the StoredProcedure asynchronously.
        /// </summary>
        /// <param name="connection">The connection to use to execute the StoredProcedure.</param>
        /// <param name="token">The <see cref="CancellationToken"/> to use to cancel the execution of the StoredProcedure.</param>
        /// <param name="timeout">The number of seconds to wait before aborting the 
        /// stored procedure's execution.</param>
        /// <returns>A Task&lt;Tuple&lt;IEnumerable&lt;T1&gt;, IEnumerable&lt;T2&gt;, IEnumerable&lt;T3&gt;, IEnumerable&lt;T4&gt;, IEnumerable&lt;T5&gt;, IEnumerable&lt;T6&gt;, IEnumerable&lt;T7&gt;&gt;&gt; that will be completed when the StoredProcedure is finished executing.</returns>
        /// <example>If using from an Entity Framework DbContext, the connection can be passed:
        /// <code language='cs'>
		/// var cts     = new CancellationTokenSource();
        /// var results = await storedProcedure.ExecuteAsync(this.Database.Connection, cts.Token);
        /// </code>
        /// </example>
		public new Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>> ExecuteAsync(IDbConnection connection, CancellationToken token, int timeout = defaultTimeout)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>>>() != null);
			
#if !NET40
            var baseClass = connection as DbConnection;
            if (baseClass != null)
            {
                IDbConnection toClose;
                var cmd = connection.CreateCommand(Schema, Name, timeout, out toClose);
                var asyncCapable = cmd as DbCommand;
                return ExecuteAsync(asyncCapable, toClose, token);
            }
#endif

			return Task.Factory.StartNew(
				() => Execute(connection, token, timeout), 
				token,
                TaskCreationOptions.None,
                TaskScheduler.Default);
		}
		
#if !NET40
		async Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>> ExecuteAsync(DbCommand cmd, IDbConnection toClose, CancellationToken token)
		{
			Contract.Requires(cmd != null);
			Contract.Ensures(Contract.Result<Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>>>() != null);

			Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>> results;
            var dbParameters = AddParameters(cmd);
			
            token.ThrowIfCancellationRequested();
			var reader = await cmd.ExecuteReaderAsync(token);
			var t1 = await T1Factory.ParseRowsAsync(reader, DataTransformers, token);
			await reader.NextResultAsync(token);
			var t2 = await T2Factory.ParseRowsAsync(reader, DataTransformers, token);
			await reader.NextResultAsync(token);
			var t3 = await T3Factory.ParseRowsAsync(reader, DataTransformers, token);
			await reader.NextResultAsync(token);
			var t4 = await T4Factory.ParseRowsAsync(reader, DataTransformers, token);
			await reader.NextResultAsync(token);
			var t5 = await T5Factory.ParseRowsAsync(reader, DataTransformers, token);
			await reader.NextResultAsync(token);
			var t6 = await T6Factory.ParseRowsAsync(reader, DataTransformers, token);
			await reader.NextResultAsync(token);
			var t7 = await T7Factory.ParseRowsAsync(reader, DataTransformers, token);
			results = Tuple.Create(t1, t2, t3, t4, t5, t6, t7);

            token.ThrowIfCancellationRequested();
			TransferOutputParameters(token, dbParameters);

			if (toClose != null)
				toClose.Close();

			cmd.Dispose();

			return results;
		}
#endif

		
        /// <summary>
        /// Clones the StoredProcedure, and gives it the passed parameters.
        /// </summary>
        /// <param name="parameters">The <see cref="IStoredProcedureParameter"/>s to pass to the stored procedure.</param>
        /// <param name="dataTransformers">The <see cref="IDataTransformer"/>s to transform the results.</param>
        /// <returns>A clone of the stored procedure.</returns>
		protected internal override StoredProcedure CloneCore(
			IEnumerable<IStoredProcedureParameter> parameters,
			IEnumerable<IDataTransformer>          dataTransformers)
		{
			return new StoredProcedure<T1, T2, T3, T4, T5, T6, T7>(Schema, Name, parameters, dataTransformers);
		}	
	}
	#endregion

}
