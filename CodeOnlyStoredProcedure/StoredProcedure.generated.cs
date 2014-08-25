using System;
using System.Collections.Generic;
#if !NET40
using System.Collections.Immutable;
#endif
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeOnlyStoredProcedure
{
	#region StoredProcedure<T1>
	/// <summary>Calls a StoredProcedure that returns 1 result set(s).</summary>
	/// <typeparam name="T1">The type of the first result set returned by the stored procedure.</typeparam>
	public class StoredProcedure<T1> : StoredProcedure
	{
		private static readonly Type t1 = typeof(T1);

        /// <summary>
        /// Creates a <see cref="StoredProcedure"/> with the given <paramref name="name"/>
        /// in the dbo schema.
        /// </summary>
        /// <param name="name">The name of the stored procedure.</param>
		public StoredProcedure(string name) : base(name)
		{ 
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
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
		}
		
		internal StoredProcedure(StoredProcedure toClone)
			: base(toClone.Schema, toClone.Name, toClone.Parameters, toClone.OutputParameterSetters, toClone.DataTransformers) 
		{ 
			Contract.Requires(toClone != null);
		}
				
#if NET40
        /// <summary>
        /// Creates a <see cref="StoredProcedure"/> with the given <paramref name="name"/>
        /// in the <paramref name="schema"/> schema, with the <see cref="SqlParameter"/>s
        /// to pass, the output action map, and the <see cref="IDataTransformer"/>s to 
        /// use to transform the results.
        /// </summary>
        /// <param name="schema">The schema of the stored procedure.</param>
        /// <param name="name">The name of the stored procedure.</param>
        /// <param name="parameters">The <see cref="SqlParameter"/>s to pass to the stored procedure.</param>
        /// <param name="outputParameterSetters">The map of <see cref="Action{T}"/> to call for output parameters.</param>
        /// <param name="dataTransformers">The <see cref="IDataTransformer"/>s to transform the results.</param>
		protected StoredProcedure(string schema, 
            string name,
            IEnumerable<SqlParameter> parameters,
            IEnumerable<KeyValuePair<string, Action<object>>> outputParameterSetters,
			IEnumerable<IDataTransformer> dataTransformers)
			: base(schema, name, parameters, outputParameterSetters, dataTransformers)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
			Contract.Requires(parameters != null);
			Contract.Requires(outputParameterSetters != null);
			Contract.Requires(dataTransformers != null);
		}
#else
        /// <summary>
        /// Creates a <see cref="StoredProcedure"/> with the given <paramref name="name"/>
        /// in the <paramref name="schema"/> schema, with the <see cref="SqlParameter"/>s
        /// to pass, the output action map, and the <see cref="IDataTransformer"/>s to 
        /// use to transform the results.
        /// </summary>
        /// <param name="schema">The schema of the stored procedure.</param>
        /// <param name="name">The name of the stored procedure.</param>
        /// <param name="parameters">The <see cref="SqlParameter"/>s to pass to the stored procedure.</param>
        /// <param name="outputParameterSetters">The map of <see cref="Action{T}"/> to call for output parameters.</param>
        /// <param name="dataTransformers">The <see cref="IDataTransformer"/>s to transform the results.</param>
		protected StoredProcedure(string                                      schema,
                                  string                                      name,
                                  ImmutableList<SqlParameter>                 parameters,
                                  ImmutableDictionary<string, Action<object>> outputParameterSetters,
                                  ImmutableList<IDataTransformer>             dataTransformers)
			: base(schema, name, parameters, outputParameterSetters, dataTransformers)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
            Contract.Requires(parameters             != null);
            Contract.Requires(outputParameterSetters != null);
            Contract.Requires(dataTransformers       != null);
		}
#endif
	
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
		public new IEnumerable<T1> Execute(IDbConnection connection, int timeout = 30)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<IEnumerable<T1>>() != null);

			var results = Execute(connection, CancellationToken.None, timeout, new[] { t1 });

			return (IEnumerable<T1>)results[t1]; 
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
		public new Task<IEnumerable<T1>> ExecuteAsync(IDbConnection connection, int timeout = 30)
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
		public new Task<IEnumerable<T1>> ExecuteAsync(IDbConnection connection, CancellationToken token, int timeout = 30)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Task<IEnumerable<T1>>>() != null);

			return Task.Factory.StartNew(
				() => 
				{
					var results = Execute(connection, token, timeout, new[] { t1 });

					return (IEnumerable<T1>)results[t1]; 
				}, token);
		}
		
        /// <summary>
        /// Clones the StoredProcedure, and gives it the passed parameters.
        /// </summary>
        /// <param name="parameters">The <see cref="SqlParameter"/>s to pass to the stored procedure.</param>
        /// <param name="outputParameterSetters">The map of <see cref="Action{T}"/> to call for output parameters.</param>
        /// <param name="dataTransformers">The <see cref="IDataTransformer"/>s to transform the results.</param>
        /// <returns>A clone of the stored procedure.</returns>
		protected internal override StoredProcedure CloneCore(
#if NET40
			IEnumerable<SqlParameter>                         parameters,
            IEnumerable<KeyValuePair<string, Action<object>>> outputParameterSetters,
			IEnumerable<IDataTransformer>                     dataTransformers)
#else
            ImmutableList<SqlParameter>                 parameters,
            ImmutableDictionary<string, Action<object>> outputParameterSetters,
            ImmutableList<IDataTransformer>             dataTransformers)
#endif
		{
			return new StoredProcedure<T1>(Schema, Name, parameters, outputParameterSetters, dataTransformers);
		}		

        internal override object InternalCall(
            IDbConnection connection,
            int           commandTimeout = 30)
        {
            return this.Execute(connection, commandTimeout);
        }

        internal override object InternalCallAsync(
            IDbConnection     connection,
            CancellationToken token,
            int               commandTimeout = 30)
        {
            return this.ExecuteAsync(connection, token, commandTimeout);
        }
	}
	#endregion

	#region StoredProcedure<T1, T2>
	/// <summary>Calls a StoredProcedure that returns 2 result set(s).</summary>
	/// <typeparam name="T1">The type of the first result set returned by the stored procedure.</typeparam>
	/// <typeparam name="T2">The type of the second result set returned by the stored procedure.</typeparam>
	public class StoredProcedure<T1, T2> : StoredProcedure<T1>
	{
		private static readonly Type t1 = typeof(T1);
		private static readonly Type t2 = typeof(T2);

        /// <summary>
        /// Creates a <see cref="StoredProcedure"/> with the given <paramref name="name"/>
        /// in the dbo schema.
        /// </summary>
        /// <param name="name">The name of the stored procedure.</param>
		public StoredProcedure(string name) : base(name)
		{ 
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
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
		}
		
		internal StoredProcedure(StoredProcedure toClone)
			: base(toClone.Schema, toClone.Name, toClone.Parameters, toClone.OutputParameterSetters, toClone.DataTransformers) 
		{ 
			Contract.Requires(toClone != null);
		}
				
#if NET40
        /// <summary>
        /// Creates a <see cref="StoredProcedure"/> with the given <paramref name="name"/>
        /// in the <paramref name="schema"/> schema, with the <see cref="SqlParameter"/>s
        /// to pass, the output action map, and the <see cref="IDataTransformer"/>s to 
        /// use to transform the results.
        /// </summary>
        /// <param name="schema">The schema of the stored procedure.</param>
        /// <param name="name">The name of the stored procedure.</param>
        /// <param name="parameters">The <see cref="SqlParameter"/>s to pass to the stored procedure.</param>
        /// <param name="outputParameterSetters">The map of <see cref="Action{T}"/> to call for output parameters.</param>
        /// <param name="dataTransformers">The <see cref="IDataTransformer"/>s to transform the results.</param>
		protected StoredProcedure(string schema, 
            string name,
            IEnumerable<SqlParameter> parameters,
            IEnumerable<KeyValuePair<string, Action<object>>> outputParameterSetters,
			IEnumerable<IDataTransformer> dataTransformers)
			: base(schema, name, parameters, outputParameterSetters, dataTransformers)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
			Contract.Requires(parameters != null);
			Contract.Requires(outputParameterSetters != null);
			Contract.Requires(dataTransformers != null);
		}
#else
        /// <summary>
        /// Creates a <see cref="StoredProcedure"/> with the given <paramref name="name"/>
        /// in the <paramref name="schema"/> schema, with the <see cref="SqlParameter"/>s
        /// to pass, the output action map, and the <see cref="IDataTransformer"/>s to 
        /// use to transform the results.
        /// </summary>
        /// <param name="schema">The schema of the stored procedure.</param>
        /// <param name="name">The name of the stored procedure.</param>
        /// <param name="parameters">The <see cref="SqlParameter"/>s to pass to the stored procedure.</param>
        /// <param name="outputParameterSetters">The map of <see cref="Action{T}"/> to call for output parameters.</param>
        /// <param name="dataTransformers">The <see cref="IDataTransformer"/>s to transform the results.</param>
		protected StoredProcedure(string                                      schema,
                                  string                                      name,
                                  ImmutableList<SqlParameter>                 parameters,
                                  ImmutableDictionary<string, Action<object>> outputParameterSetters,
                                  ImmutableList<IDataTransformer>             dataTransformers)
			: base(schema, name, parameters, outputParameterSetters, dataTransformers)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
            Contract.Requires(parameters             != null);
            Contract.Requires(outputParameterSetters != null);
            Contract.Requires(dataTransformers       != null);
		}
#endif
	
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
		public new Tuple<IEnumerable<T1>, IEnumerable<T2>> Execute(IDbConnection connection, int timeout = 30)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Tuple<IEnumerable<T1>, IEnumerable<T2>>>() != null);

			var results = Execute(connection, CancellationToken.None, timeout, new[] { t1, t2 });

			return Tuple.Create((IEnumerable<T1>)results[t1], (IEnumerable<T2>)results[t2]); 
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
		public new Task<Tuple<IEnumerable<T1>, IEnumerable<T2>>> ExecuteAsync(IDbConnection connection, int timeout = 30)
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
		public new Task<Tuple<IEnumerable<T1>, IEnumerable<T2>>> ExecuteAsync(IDbConnection connection, CancellationToken token, int timeout = 30)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Task<Tuple<IEnumerable<T1>, IEnumerable<T2>>>>() != null);

			return Task.Factory.StartNew(
				() => 
				{
					var results = Execute(connection, token, timeout, new[] { t1, t2 });

					return Tuple.Create((IEnumerable<T1>)results[t1], (IEnumerable<T2>)results[t2]); 
				}, token);
		}
		
        /// <summary>
        /// Clones the StoredProcedure, and gives it the passed parameters.
        /// </summary>
        /// <param name="parameters">The <see cref="SqlParameter"/>s to pass to the stored procedure.</param>
        /// <param name="outputParameterSetters">The map of <see cref="Action{T}"/> to call for output parameters.</param>
        /// <param name="dataTransformers">The <see cref="IDataTransformer"/>s to transform the results.</param>
        /// <returns>A clone of the stored procedure.</returns>
		protected internal override StoredProcedure CloneCore(
#if NET40
			IEnumerable<SqlParameter>                         parameters,
            IEnumerable<KeyValuePair<string, Action<object>>> outputParameterSetters,
			IEnumerable<IDataTransformer>                     dataTransformers)
#else
            ImmutableList<SqlParameter>                 parameters,
            ImmutableDictionary<string, Action<object>> outputParameterSetters,
            ImmutableList<IDataTransformer>             dataTransformers)
#endif
		{
			return new StoredProcedure<T1, T2>(Schema, Name, parameters, outputParameterSetters, dataTransformers);
		}		

        internal override object InternalCall(
            IDbConnection connection,
            int           commandTimeout = 30)
        {
            return this.Execute(connection, commandTimeout);
        }

        internal override object InternalCallAsync(
            IDbConnection     connection,
            CancellationToken token,
            int               commandTimeout = 30)
        {
            return this.ExecuteAsync(connection, token, commandTimeout);
        }
	}
	#endregion

	#region StoredProcedure<T1, T2, T3>
	/// <summary>Calls a StoredProcedure that returns 3 result set(s).</summary>
	/// <typeparam name="T1">The type of the first result set returned by the stored procedure.</typeparam>
	/// <typeparam name="T2">The type of the second result set returned by the stored procedure.</typeparam>
	/// <typeparam name="T3">The type of the third result set returned by the stored procedure.</typeparam>
	public class StoredProcedure<T1, T2, T3> : StoredProcedure<T1, T2>
	{
		private static readonly Type t1 = typeof(T1);
		private static readonly Type t2 = typeof(T2);
		private static readonly Type t3 = typeof(T3);

        /// <summary>
        /// Creates a <see cref="StoredProcedure"/> with the given <paramref name="name"/>
        /// in the dbo schema.
        /// </summary>
        /// <param name="name">The name of the stored procedure.</param>
		public StoredProcedure(string name) : base(name)
		{ 
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
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
		}
		
		internal StoredProcedure(StoredProcedure toClone)
			: base(toClone.Schema, toClone.Name, toClone.Parameters, toClone.OutputParameterSetters, toClone.DataTransformers) 
		{ 
			Contract.Requires(toClone != null);
		}
				
#if NET40
        /// <summary>
        /// Creates a <see cref="StoredProcedure"/> with the given <paramref name="name"/>
        /// in the <paramref name="schema"/> schema, with the <see cref="SqlParameter"/>s
        /// to pass, the output action map, and the <see cref="IDataTransformer"/>s to 
        /// use to transform the results.
        /// </summary>
        /// <param name="schema">The schema of the stored procedure.</param>
        /// <param name="name">The name of the stored procedure.</param>
        /// <param name="parameters">The <see cref="SqlParameter"/>s to pass to the stored procedure.</param>
        /// <param name="outputParameterSetters">The map of <see cref="Action{T}"/> to call for output parameters.</param>
        /// <param name="dataTransformers">The <see cref="IDataTransformer"/>s to transform the results.</param>
		protected StoredProcedure(string schema, 
            string name,
            IEnumerable<SqlParameter> parameters,
            IEnumerable<KeyValuePair<string, Action<object>>> outputParameterSetters,
			IEnumerable<IDataTransformer> dataTransformers)
			: base(schema, name, parameters, outputParameterSetters, dataTransformers)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
			Contract.Requires(parameters != null);
			Contract.Requires(outputParameterSetters != null);
			Contract.Requires(dataTransformers != null);
		}
#else
        /// <summary>
        /// Creates a <see cref="StoredProcedure"/> with the given <paramref name="name"/>
        /// in the <paramref name="schema"/> schema, with the <see cref="SqlParameter"/>s
        /// to pass, the output action map, and the <see cref="IDataTransformer"/>s to 
        /// use to transform the results.
        /// </summary>
        /// <param name="schema">The schema of the stored procedure.</param>
        /// <param name="name">The name of the stored procedure.</param>
        /// <param name="parameters">The <see cref="SqlParameter"/>s to pass to the stored procedure.</param>
        /// <param name="outputParameterSetters">The map of <see cref="Action{T}"/> to call for output parameters.</param>
        /// <param name="dataTransformers">The <see cref="IDataTransformer"/>s to transform the results.</param>
		protected StoredProcedure(string                                      schema,
                                  string                                      name,
                                  ImmutableList<SqlParameter>                 parameters,
                                  ImmutableDictionary<string, Action<object>> outputParameterSetters,
                                  ImmutableList<IDataTransformer>             dataTransformers)
			: base(schema, name, parameters, outputParameterSetters, dataTransformers)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
            Contract.Requires(parameters             != null);
            Contract.Requires(outputParameterSetters != null);
            Contract.Requires(dataTransformers       != null);
		}
#endif
	
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
		public new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>> Execute(IDbConnection connection, int timeout = 30)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>>() != null);

			var results = Execute(connection, CancellationToken.None, timeout, new[] { t1, t2, t3 });

			return Tuple.Create((IEnumerable<T1>)results[t1], (IEnumerable<T2>)results[t2], (IEnumerable<T3>)results[t3]); 
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
		public new Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>> ExecuteAsync(IDbConnection connection, int timeout = 30)
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
		public new Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>> ExecuteAsync(IDbConnection connection, CancellationToken token, int timeout = 30)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>>>() != null);

			return Task.Factory.StartNew(
				() => 
				{
					var results = Execute(connection, token, timeout, new[] { t1, t2, t3 });

					return Tuple.Create((IEnumerable<T1>)results[t1], (IEnumerable<T2>)results[t2], (IEnumerable<T3>)results[t3]); 
				}, token);
		}
		
        /// <summary>
        /// Clones the StoredProcedure, and gives it the passed parameters.
        /// </summary>
        /// <param name="parameters">The <see cref="SqlParameter"/>s to pass to the stored procedure.</param>
        /// <param name="outputParameterSetters">The map of <see cref="Action{T}"/> to call for output parameters.</param>
        /// <param name="dataTransformers">The <see cref="IDataTransformer"/>s to transform the results.</param>
        /// <returns>A clone of the stored procedure.</returns>
		protected internal override StoredProcedure CloneCore(
#if NET40
			IEnumerable<SqlParameter>                         parameters,
            IEnumerable<KeyValuePair<string, Action<object>>> outputParameterSetters,
			IEnumerable<IDataTransformer>                     dataTransformers)
#else
            ImmutableList<SqlParameter>                 parameters,
            ImmutableDictionary<string, Action<object>> outputParameterSetters,
            ImmutableList<IDataTransformer>             dataTransformers)
#endif
		{
			return new StoredProcedure<T1, T2, T3>(Schema, Name, parameters, outputParameterSetters, dataTransformers);
		}		

        internal override object InternalCall(
            IDbConnection connection,
            int           commandTimeout = 30)
        {
            return this.Execute(connection, commandTimeout);
        }

        internal override object InternalCallAsync(
            IDbConnection     connection,
            CancellationToken token,
            int               commandTimeout = 30)
        {
            return this.ExecuteAsync(connection, token, commandTimeout);
        }
	}
	#endregion

	#region StoredProcedure<T1, T2, T3, T4>
	/// <summary>Calls a StoredProcedure that returns 4 result set(s).</summary>
	/// <typeparam name="T1">The type of the first result set returned by the stored procedure.</typeparam>
	/// <typeparam name="T2">The type of the second result set returned by the stored procedure.</typeparam>
	/// <typeparam name="T3">The type of the third result set returned by the stored procedure.</typeparam>
	/// <typeparam name="T4">The type of the fourth result set returned by the stored procedure.</typeparam>
	public class StoredProcedure<T1, T2, T3, T4> : StoredProcedure<T1, T2, T3>
	{
		private static readonly Type t1 = typeof(T1);
		private static readonly Type t2 = typeof(T2);
		private static readonly Type t3 = typeof(T3);
		private static readonly Type t4 = typeof(T4);

        /// <summary>
        /// Creates a <see cref="StoredProcedure"/> with the given <paramref name="name"/>
        /// in the dbo schema.
        /// </summary>
        /// <param name="name">The name of the stored procedure.</param>
		public StoredProcedure(string name) : base(name)
		{ 
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
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
		}
		
		internal StoredProcedure(StoredProcedure toClone)
			: base(toClone.Schema, toClone.Name, toClone.Parameters, toClone.OutputParameterSetters, toClone.DataTransformers) 
		{ 
			Contract.Requires(toClone != null);
		}
				
#if NET40
        /// <summary>
        /// Creates a <see cref="StoredProcedure"/> with the given <paramref name="name"/>
        /// in the <paramref name="schema"/> schema, with the <see cref="SqlParameter"/>s
        /// to pass, the output action map, and the <see cref="IDataTransformer"/>s to 
        /// use to transform the results.
        /// </summary>
        /// <param name="schema">The schema of the stored procedure.</param>
        /// <param name="name">The name of the stored procedure.</param>
        /// <param name="parameters">The <see cref="SqlParameter"/>s to pass to the stored procedure.</param>
        /// <param name="outputParameterSetters">The map of <see cref="Action{T}"/> to call for output parameters.</param>
        /// <param name="dataTransformers">The <see cref="IDataTransformer"/>s to transform the results.</param>
		protected StoredProcedure(string schema, 
            string name,
            IEnumerable<SqlParameter> parameters,
            IEnumerable<KeyValuePair<string, Action<object>>> outputParameterSetters,
			IEnumerable<IDataTransformer> dataTransformers)
			: base(schema, name, parameters, outputParameterSetters, dataTransformers)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
			Contract.Requires(parameters != null);
			Contract.Requires(outputParameterSetters != null);
			Contract.Requires(dataTransformers != null);
		}
#else
        /// <summary>
        /// Creates a <see cref="StoredProcedure"/> with the given <paramref name="name"/>
        /// in the <paramref name="schema"/> schema, with the <see cref="SqlParameter"/>s
        /// to pass, the output action map, and the <see cref="IDataTransformer"/>s to 
        /// use to transform the results.
        /// </summary>
        /// <param name="schema">The schema of the stored procedure.</param>
        /// <param name="name">The name of the stored procedure.</param>
        /// <param name="parameters">The <see cref="SqlParameter"/>s to pass to the stored procedure.</param>
        /// <param name="outputParameterSetters">The map of <see cref="Action{T}"/> to call for output parameters.</param>
        /// <param name="dataTransformers">The <see cref="IDataTransformer"/>s to transform the results.</param>
		protected StoredProcedure(string                                      schema,
                                  string                                      name,
                                  ImmutableList<SqlParameter>                 parameters,
                                  ImmutableDictionary<string, Action<object>> outputParameterSetters,
                                  ImmutableList<IDataTransformer>             dataTransformers)
			: base(schema, name, parameters, outputParameterSetters, dataTransformers)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
            Contract.Requires(parameters             != null);
            Contract.Requires(outputParameterSetters != null);
            Contract.Requires(dataTransformers       != null);
		}
#endif
	
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
		public new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>> Execute(IDbConnection connection, int timeout = 30)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>>() != null);

			var results = Execute(connection, CancellationToken.None, timeout, new[] { t1, t2, t3, t4 });

			return Tuple.Create((IEnumerable<T1>)results[t1], (IEnumerable<T2>)results[t2], (IEnumerable<T3>)results[t3], (IEnumerable<T4>)results[t4]); 
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
		public new Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>> ExecuteAsync(IDbConnection connection, int timeout = 30)
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
		public new Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>> ExecuteAsync(IDbConnection connection, CancellationToken token, int timeout = 30)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>>>() != null);

			return Task.Factory.StartNew(
				() => 
				{
					var results = Execute(connection, token, timeout, new[] { t1, t2, t3, t4 });

					return Tuple.Create((IEnumerable<T1>)results[t1], (IEnumerable<T2>)results[t2], (IEnumerable<T3>)results[t3], (IEnumerable<T4>)results[t4]); 
				}, token);
		}
		
        /// <summary>
        /// Clones the StoredProcedure, and gives it the passed parameters.
        /// </summary>
        /// <param name="parameters">The <see cref="SqlParameter"/>s to pass to the stored procedure.</param>
        /// <param name="outputParameterSetters">The map of <see cref="Action{T}"/> to call for output parameters.</param>
        /// <param name="dataTransformers">The <see cref="IDataTransformer"/>s to transform the results.</param>
        /// <returns>A clone of the stored procedure.</returns>
		protected internal override StoredProcedure CloneCore(
#if NET40
			IEnumerable<SqlParameter>                         parameters,
            IEnumerable<KeyValuePair<string, Action<object>>> outputParameterSetters,
			IEnumerable<IDataTransformer>                     dataTransformers)
#else
            ImmutableList<SqlParameter>                 parameters,
            ImmutableDictionary<string, Action<object>> outputParameterSetters,
            ImmutableList<IDataTransformer>             dataTransformers)
#endif
		{
			return new StoredProcedure<T1, T2, T3, T4>(Schema, Name, parameters, outputParameterSetters, dataTransformers);
		}		

        internal override object InternalCall(
            IDbConnection connection,
            int           commandTimeout = 30)
        {
            return this.Execute(connection, commandTimeout);
        }

        internal override object InternalCallAsync(
            IDbConnection     connection,
            CancellationToken token,
            int               commandTimeout = 30)
        {
            return this.ExecuteAsync(connection, token, commandTimeout);
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
	public class StoredProcedure<T1, T2, T3, T4, T5> : StoredProcedure<T1, T2, T3, T4>
	{
		private static readonly Type t1 = typeof(T1);
		private static readonly Type t2 = typeof(T2);
		private static readonly Type t3 = typeof(T3);
		private static readonly Type t4 = typeof(T4);
		private static readonly Type t5 = typeof(T5);

        /// <summary>
        /// Creates a <see cref="StoredProcedure"/> with the given <paramref name="name"/>
        /// in the dbo schema.
        /// </summary>
        /// <param name="name">The name of the stored procedure.</param>
		public StoredProcedure(string name) : base(name)
		{ 
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
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
		}
		
		internal StoredProcedure(StoredProcedure toClone)
			: base(toClone.Schema, toClone.Name, toClone.Parameters, toClone.OutputParameterSetters, toClone.DataTransformers) 
		{ 
			Contract.Requires(toClone != null);
		}
				
#if NET40
        /// <summary>
        /// Creates a <see cref="StoredProcedure"/> with the given <paramref name="name"/>
        /// in the <paramref name="schema"/> schema, with the <see cref="SqlParameter"/>s
        /// to pass, the output action map, and the <see cref="IDataTransformer"/>s to 
        /// use to transform the results.
        /// </summary>
        /// <param name="schema">The schema of the stored procedure.</param>
        /// <param name="name">The name of the stored procedure.</param>
        /// <param name="parameters">The <see cref="SqlParameter"/>s to pass to the stored procedure.</param>
        /// <param name="outputParameterSetters">The map of <see cref="Action{T}"/> to call for output parameters.</param>
        /// <param name="dataTransformers">The <see cref="IDataTransformer"/>s to transform the results.</param>
		protected StoredProcedure(string schema, 
            string name,
            IEnumerable<SqlParameter> parameters,
            IEnumerable<KeyValuePair<string, Action<object>>> outputParameterSetters,
			IEnumerable<IDataTransformer> dataTransformers)
			: base(schema, name, parameters, outputParameterSetters, dataTransformers)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
			Contract.Requires(parameters != null);
			Contract.Requires(outputParameterSetters != null);
			Contract.Requires(dataTransformers != null);
		}
#else
        /// <summary>
        /// Creates a <see cref="StoredProcedure"/> with the given <paramref name="name"/>
        /// in the <paramref name="schema"/> schema, with the <see cref="SqlParameter"/>s
        /// to pass, the output action map, and the <see cref="IDataTransformer"/>s to 
        /// use to transform the results.
        /// </summary>
        /// <param name="schema">The schema of the stored procedure.</param>
        /// <param name="name">The name of the stored procedure.</param>
        /// <param name="parameters">The <see cref="SqlParameter"/>s to pass to the stored procedure.</param>
        /// <param name="outputParameterSetters">The map of <see cref="Action{T}"/> to call for output parameters.</param>
        /// <param name="dataTransformers">The <see cref="IDataTransformer"/>s to transform the results.</param>
		protected StoredProcedure(string                                      schema,
                                  string                                      name,
                                  ImmutableList<SqlParameter>                 parameters,
                                  ImmutableDictionary<string, Action<object>> outputParameterSetters,
                                  ImmutableList<IDataTransformer>             dataTransformers)
			: base(schema, name, parameters, outputParameterSetters, dataTransformers)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
            Contract.Requires(parameters             != null);
            Contract.Requires(outputParameterSetters != null);
            Contract.Requires(dataTransformers       != null);
		}
#endif
	
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
		public new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>> Execute(IDbConnection connection, int timeout = 30)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>>() != null);

			var results = Execute(connection, CancellationToken.None, timeout, new[] { t1, t2, t3, t4, t5 });

			return Tuple.Create((IEnumerable<T1>)results[t1], (IEnumerable<T2>)results[t2], (IEnumerable<T3>)results[t3], (IEnumerable<T4>)results[t4], (IEnumerable<T5>)results[t5]); 
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
		public new Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>> ExecuteAsync(IDbConnection connection, int timeout = 30)
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
		public new Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>> ExecuteAsync(IDbConnection connection, CancellationToken token, int timeout = 30)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>>>() != null);

			return Task.Factory.StartNew(
				() => 
				{
					var results = Execute(connection, token, timeout, new[] { t1, t2, t3, t4, t5 });

					return Tuple.Create((IEnumerable<T1>)results[t1], (IEnumerable<T2>)results[t2], (IEnumerable<T3>)results[t3], (IEnumerable<T4>)results[t4], (IEnumerable<T5>)results[t5]); 
				}, token);
		}
		
        /// <summary>
        /// Clones the StoredProcedure, and gives it the passed parameters.
        /// </summary>
        /// <param name="parameters">The <see cref="SqlParameter"/>s to pass to the stored procedure.</param>
        /// <param name="outputParameterSetters">The map of <see cref="Action{T}"/> to call for output parameters.</param>
        /// <param name="dataTransformers">The <see cref="IDataTransformer"/>s to transform the results.</param>
        /// <returns>A clone of the stored procedure.</returns>
		protected internal override StoredProcedure CloneCore(
#if NET40
			IEnumerable<SqlParameter>                         parameters,
            IEnumerable<KeyValuePair<string, Action<object>>> outputParameterSetters,
			IEnumerable<IDataTransformer>                     dataTransformers)
#else
            ImmutableList<SqlParameter>                 parameters,
            ImmutableDictionary<string, Action<object>> outputParameterSetters,
            ImmutableList<IDataTransformer>             dataTransformers)
#endif
		{
			return new StoredProcedure<T1, T2, T3, T4, T5>(Schema, Name, parameters, outputParameterSetters, dataTransformers);
		}		

        internal override object InternalCall(
            IDbConnection connection,
            int           commandTimeout = 30)
        {
            return this.Execute(connection, commandTimeout);
        }

        internal override object InternalCallAsync(
            IDbConnection     connection,
            CancellationToken token,
            int               commandTimeout = 30)
        {
            return this.ExecuteAsync(connection, token, commandTimeout);
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
	public class StoredProcedure<T1, T2, T3, T4, T5, T6> : StoredProcedure<T1, T2, T3, T4, T5>
	{
		private static readonly Type t1 = typeof(T1);
		private static readonly Type t2 = typeof(T2);
		private static readonly Type t3 = typeof(T3);
		private static readonly Type t4 = typeof(T4);
		private static readonly Type t5 = typeof(T5);
		private static readonly Type t6 = typeof(T6);

        /// <summary>
        /// Creates a <see cref="StoredProcedure"/> with the given <paramref name="name"/>
        /// in the dbo schema.
        /// </summary>
        /// <param name="name">The name of the stored procedure.</param>
		public StoredProcedure(string name) : base(name)
		{ 
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
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
		}
		
		internal StoredProcedure(StoredProcedure toClone)
			: base(toClone.Schema, toClone.Name, toClone.Parameters, toClone.OutputParameterSetters, toClone.DataTransformers) 
		{ 
			Contract.Requires(toClone != null);
		}
				
#if NET40
        /// <summary>
        /// Creates a <see cref="StoredProcedure"/> with the given <paramref name="name"/>
        /// in the <paramref name="schema"/> schema, with the <see cref="SqlParameter"/>s
        /// to pass, the output action map, and the <see cref="IDataTransformer"/>s to 
        /// use to transform the results.
        /// </summary>
        /// <param name="schema">The schema of the stored procedure.</param>
        /// <param name="name">The name of the stored procedure.</param>
        /// <param name="parameters">The <see cref="SqlParameter"/>s to pass to the stored procedure.</param>
        /// <param name="outputParameterSetters">The map of <see cref="Action{T}"/> to call for output parameters.</param>
        /// <param name="dataTransformers">The <see cref="IDataTransformer"/>s to transform the results.</param>
		protected StoredProcedure(string schema, 
            string name,
            IEnumerable<SqlParameter> parameters,
            IEnumerable<KeyValuePair<string, Action<object>>> outputParameterSetters,
			IEnumerable<IDataTransformer> dataTransformers)
			: base(schema, name, parameters, outputParameterSetters, dataTransformers)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
			Contract.Requires(parameters != null);
			Contract.Requires(outputParameterSetters != null);
			Contract.Requires(dataTransformers != null);
		}
#else
        /// <summary>
        /// Creates a <see cref="StoredProcedure"/> with the given <paramref name="name"/>
        /// in the <paramref name="schema"/> schema, with the <see cref="SqlParameter"/>s
        /// to pass, the output action map, and the <see cref="IDataTransformer"/>s to 
        /// use to transform the results.
        /// </summary>
        /// <param name="schema">The schema of the stored procedure.</param>
        /// <param name="name">The name of the stored procedure.</param>
        /// <param name="parameters">The <see cref="SqlParameter"/>s to pass to the stored procedure.</param>
        /// <param name="outputParameterSetters">The map of <see cref="Action{T}"/> to call for output parameters.</param>
        /// <param name="dataTransformers">The <see cref="IDataTransformer"/>s to transform the results.</param>
		protected StoredProcedure(string                                      schema,
                                  string                                      name,
                                  ImmutableList<SqlParameter>                 parameters,
                                  ImmutableDictionary<string, Action<object>> outputParameterSetters,
                                  ImmutableList<IDataTransformer>             dataTransformers)
			: base(schema, name, parameters, outputParameterSetters, dataTransformers)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
            Contract.Requires(parameters             != null);
            Contract.Requires(outputParameterSetters != null);
            Contract.Requires(dataTransformers       != null);
		}
#endif
	
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
		public new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>> Execute(IDbConnection connection, int timeout = 30)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>>() != null);

			var results = Execute(connection, CancellationToken.None, timeout, new[] { t1, t2, t3, t4, t5, t6 });

			return Tuple.Create((IEnumerable<T1>)results[t1], (IEnumerable<T2>)results[t2], (IEnumerable<T3>)results[t3], (IEnumerable<T4>)results[t4], (IEnumerable<T5>)results[t5], (IEnumerable<T6>)results[t6]); 
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
		public new Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>> ExecuteAsync(IDbConnection connection, int timeout = 30)
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
		public new Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>> ExecuteAsync(IDbConnection connection, CancellationToken token, int timeout = 30)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>>>() != null);

			return Task.Factory.StartNew(
				() => 
				{
					var results = Execute(connection, token, timeout, new[] { t1, t2, t3, t4, t5, t6 });

					return Tuple.Create((IEnumerable<T1>)results[t1], (IEnumerable<T2>)results[t2], (IEnumerable<T3>)results[t3], (IEnumerable<T4>)results[t4], (IEnumerable<T5>)results[t5], (IEnumerable<T6>)results[t6]); 
				}, token);
		}
		
        /// <summary>
        /// Clones the StoredProcedure, and gives it the passed parameters.
        /// </summary>
        /// <param name="parameters">The <see cref="SqlParameter"/>s to pass to the stored procedure.</param>
        /// <param name="outputParameterSetters">The map of <see cref="Action{T}"/> to call for output parameters.</param>
        /// <param name="dataTransformers">The <see cref="IDataTransformer"/>s to transform the results.</param>
        /// <returns>A clone of the stored procedure.</returns>
		protected internal override StoredProcedure CloneCore(
#if NET40
			IEnumerable<SqlParameter>                         parameters,
            IEnumerable<KeyValuePair<string, Action<object>>> outputParameterSetters,
			IEnumerable<IDataTransformer>                     dataTransformers)
#else
            ImmutableList<SqlParameter>                 parameters,
            ImmutableDictionary<string, Action<object>> outputParameterSetters,
            ImmutableList<IDataTransformer>             dataTransformers)
#endif
		{
			return new StoredProcedure<T1, T2, T3, T4, T5, T6>(Schema, Name, parameters, outputParameterSetters, dataTransformers);
		}		

        internal override object InternalCall(
            IDbConnection connection,
            int           commandTimeout = 30)
        {
            return this.Execute(connection, commandTimeout);
        }

        internal override object InternalCallAsync(
            IDbConnection     connection,
            CancellationToken token,
            int               commandTimeout = 30)
        {
            return this.ExecuteAsync(connection, token, commandTimeout);
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
	public class StoredProcedure<T1, T2, T3, T4, T5, T6, T7> : StoredProcedure<T1, T2, T3, T4, T5, T6>
	{
		private static readonly Type t1 = typeof(T1);
		private static readonly Type t2 = typeof(T2);
		private static readonly Type t3 = typeof(T3);
		private static readonly Type t4 = typeof(T4);
		private static readonly Type t5 = typeof(T5);
		private static readonly Type t6 = typeof(T6);
		private static readonly Type t7 = typeof(T7);

        /// <summary>
        /// Creates a <see cref="StoredProcedure"/> with the given <paramref name="name"/>
        /// in the dbo schema.
        /// </summary>
        /// <param name="name">The name of the stored procedure.</param>
		public StoredProcedure(string name) : base(name)
		{ 
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
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
		}
		
		internal StoredProcedure(StoredProcedure toClone)
			: base(toClone.Schema, toClone.Name, toClone.Parameters, toClone.OutputParameterSetters, toClone.DataTransformers) 
		{ 
			Contract.Requires(toClone != null);
		}
				
#if NET40
        /// <summary>
        /// Creates a <see cref="StoredProcedure"/> with the given <paramref name="name"/>
        /// in the <paramref name="schema"/> schema, with the <see cref="SqlParameter"/>s
        /// to pass, the output action map, and the <see cref="IDataTransformer"/>s to 
        /// use to transform the results.
        /// </summary>
        /// <param name="schema">The schema of the stored procedure.</param>
        /// <param name="name">The name of the stored procedure.</param>
        /// <param name="parameters">The <see cref="SqlParameter"/>s to pass to the stored procedure.</param>
        /// <param name="outputParameterSetters">The map of <see cref="Action{T}"/> to call for output parameters.</param>
        /// <param name="dataTransformers">The <see cref="IDataTransformer"/>s to transform the results.</param>
		protected StoredProcedure(string schema, 
            string name,
            IEnumerable<SqlParameter> parameters,
            IEnumerable<KeyValuePair<string, Action<object>>> outputParameterSetters,
			IEnumerable<IDataTransformer> dataTransformers)
			: base(schema, name, parameters, outputParameterSetters, dataTransformers)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
			Contract.Requires(parameters != null);
			Contract.Requires(outputParameterSetters != null);
			Contract.Requires(dataTransformers != null);
		}
#else
        /// <summary>
        /// Creates a <see cref="StoredProcedure"/> with the given <paramref name="name"/>
        /// in the <paramref name="schema"/> schema, with the <see cref="SqlParameter"/>s
        /// to pass, the output action map, and the <see cref="IDataTransformer"/>s to 
        /// use to transform the results.
        /// </summary>
        /// <param name="schema">The schema of the stored procedure.</param>
        /// <param name="name">The name of the stored procedure.</param>
        /// <param name="parameters">The <see cref="SqlParameter"/>s to pass to the stored procedure.</param>
        /// <param name="outputParameterSetters">The map of <see cref="Action{T}"/> to call for output parameters.</param>
        /// <param name="dataTransformers">The <see cref="IDataTransformer"/>s to transform the results.</param>
		protected StoredProcedure(string                                      schema,
                                  string                                      name,
                                  ImmutableList<SqlParameter>                 parameters,
                                  ImmutableDictionary<string, Action<object>> outputParameterSetters,
                                  ImmutableList<IDataTransformer>             dataTransformers)
			: base(schema, name, parameters, outputParameterSetters, dataTransformers)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
            Contract.Requires(parameters             != null);
            Contract.Requires(outputParameterSetters != null);
            Contract.Requires(dataTransformers       != null);
		}
#endif
	
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
		public new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>> Execute(IDbConnection connection, int timeout = 30)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>>() != null);

			var results = Execute(connection, CancellationToken.None, timeout, new[] { t1, t2, t3, t4, t5, t6, t7 });

			return Tuple.Create((IEnumerable<T1>)results[t1], (IEnumerable<T2>)results[t2], (IEnumerable<T3>)results[t3], (IEnumerable<T4>)results[t4], (IEnumerable<T5>)results[t5], (IEnumerable<T6>)results[t6], (IEnumerable<T7>)results[t7]); 
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
		public new Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>> ExecuteAsync(IDbConnection connection, int timeout = 30)
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
		public new Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>> ExecuteAsync(IDbConnection connection, CancellationToken token, int timeout = 30)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>>>() != null);

			return Task.Factory.StartNew(
				() => 
				{
					var results = Execute(connection, token, timeout, new[] { t1, t2, t3, t4, t5, t6, t7 });

					return Tuple.Create((IEnumerable<T1>)results[t1], (IEnumerable<T2>)results[t2], (IEnumerable<T3>)results[t3], (IEnumerable<T4>)results[t4], (IEnumerable<T5>)results[t5], (IEnumerable<T6>)results[t6], (IEnumerable<T7>)results[t7]); 
				}, token);
		}
		
        /// <summary>
        /// Clones the StoredProcedure, and gives it the passed parameters.
        /// </summary>
        /// <param name="parameters">The <see cref="SqlParameter"/>s to pass to the stored procedure.</param>
        /// <param name="outputParameterSetters">The map of <see cref="Action{T}"/> to call for output parameters.</param>
        /// <param name="dataTransformers">The <see cref="IDataTransformer"/>s to transform the results.</param>
        /// <returns>A clone of the stored procedure.</returns>
		protected internal override StoredProcedure CloneCore(
#if NET40
			IEnumerable<SqlParameter>                         parameters,
            IEnumerable<KeyValuePair<string, Action<object>>> outputParameterSetters,
			IEnumerable<IDataTransformer>                     dataTransformers)
#else
            ImmutableList<SqlParameter>                 parameters,
            ImmutableDictionary<string, Action<object>> outputParameterSetters,
            ImmutableList<IDataTransformer>             dataTransformers)
#endif
		{
			return new StoredProcedure<T1, T2, T3, T4, T5, T6, T7>(Schema, Name, parameters, outputParameterSetters, dataTransformers);
		}		

        internal override object InternalCall(
            IDbConnection connection,
            int           commandTimeout = 30)
        {
            return this.Execute(connection, commandTimeout);
        }

        internal override object InternalCallAsync(
            IDbConnection     connection,
            CancellationToken token,
            int               commandTimeout = 30)
        {
            return this.ExecuteAsync(connection, token, commandTimeout);
        }
	}
	#endregion

}
