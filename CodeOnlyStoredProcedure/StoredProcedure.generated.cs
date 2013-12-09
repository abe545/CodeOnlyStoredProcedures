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
	#region StoredProcedure<T1>
	public class StoredProcedure<T1> : StoredProcedure
	{
		private static readonly Type t1 = typeof(T1);

		public StoredProcedure(string name) : base(name)
		{ 
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
		}

		public StoredProcedure(string schema, string name) : base(schema, name)
		{ 
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
		}
		
		internal StoredProcedure(StoredProcedure toClone)
			: base(toClone.Schema, toClone.Name, toClone.Parameters, toClone.OutputParameterSetters) 
		{ 
			Contract.Requires(toClone != null);
		}
		
		protected StoredProcedure(string schema, 
            string name,
            ImmutableList<SqlParameter> parameters,
            ImmutableDictionary<string, Action<object>> outputParameterSetters)
			: base(schema, name, parameters, outputParameterSetters)
        {
		}
		
		public new IEnumerable<T1> Execute(IDbConnection connection, int? timeout = null)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<IEnumerable<T1>>() != null);

			return Execute(connection, CancellationToken.None, timeout);
		}

		IEnumerable<T1> Execute(IDbConnection connection, CancellationToken token, int? timeout = null)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<IEnumerable<T1>>() != null);

			var results = connection.Execute(FullName,
				token, 
				timeout,
				Parameters,
				new[] { t1 });
			TransferOutputParameters();

			return (IEnumerable<T1>)results[t1]; 
		}

		public new Task<IEnumerable<T1>> ExecuteAsync(IDbConnection connection, int? timeout = null)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Task<IEnumerable<T1>>>() != null);

			return ExecuteAsync(connection, CancellationToken.None, timeout);
		}

		public new Task<IEnumerable<T1>> ExecuteAsync(IDbConnection connection, CancellationToken token, int? timeout = null)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Task<IEnumerable<T1>>>() != null);

			return Task.Run(() => Execute(connection, token, timeout));
		}

		protected override StoredProcedure CloneCore(ImmutableList<SqlParameter> parameters,
            ImmutableDictionary<string, Action<object>> outputParameterSetters)
        {
            return new StoredProcedure<T1>(Schema, Name, parameters, outputParameterSetters);
        }
	}
	#endregion

	#region StoredProcedure<T1, T2>
	public class StoredProcedure<T1, T2> : StoredProcedure<T1>
	{
		private static readonly Type t1 = typeof(T1);
		private static readonly Type t2 = typeof(T2);

		public StoredProcedure(string name) : base(name)
		{ 
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
		}

		public StoredProcedure(string schema, string name) : base(schema, name)
		{ 
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
		}
		
		internal StoredProcedure(StoredProcedure toClone)
			: base(toClone.Schema, toClone.Name, toClone.Parameters, toClone.OutputParameterSetters) 
		{ 
			Contract.Requires(toClone != null);
		}
		
		protected StoredProcedure(string schema, 
            string name,
            ImmutableList<SqlParameter> parameters,
            ImmutableDictionary<string, Action<object>> outputParameterSetters)
			: base(schema, name, parameters, outputParameterSetters)
        {
		}
		
		public new Tuple<IEnumerable<T1>, IEnumerable<T2>> Execute(IDbConnection connection, int? timeout = null)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Tuple<IEnumerable<T1>, IEnumerable<T2>>>() != null);

			return Execute(connection, CancellationToken.None, timeout);
		}

		Tuple<IEnumerable<T1>, IEnumerable<T2>> Execute(IDbConnection connection, CancellationToken token, int? timeout = null)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Tuple<IEnumerable<T1>, IEnumerable<T2>>>() != null);

			var results = connection.Execute(FullName,
				token, 
				timeout,
				Parameters,
				new[] { t1, t2 });
			TransferOutputParameters();

			return Tuple.Create((IEnumerable<T1>)results[t1], (IEnumerable<T2>)results[t2]); 
		}

		public new Task<Tuple<IEnumerable<T1>, IEnumerable<T2>>> ExecuteAsync(IDbConnection connection, int? timeout = null)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Task<Tuple<IEnumerable<T1>, IEnumerable<T2>>>>() != null);

			return ExecuteAsync(connection, CancellationToken.None, timeout);
		}

		public new Task<Tuple<IEnumerable<T1>, IEnumerable<T2>>> ExecuteAsync(IDbConnection connection, CancellationToken token, int? timeout = null)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Task<Tuple<IEnumerable<T1>, IEnumerable<T2>>>>() != null);

			return Task.Run(() => Execute(connection, token, timeout));
		}

		protected override StoredProcedure CloneCore(ImmutableList<SqlParameter> parameters,
            ImmutableDictionary<string, Action<object>> outputParameterSetters)
        {
            return new StoredProcedure<T1, T2>(Schema, Name, parameters, outputParameterSetters);
        }
	}
	#endregion

	#region StoredProcedure<T1, T2, T3>
	public class StoredProcedure<T1, T2, T3> : StoredProcedure<T1, T2>
	{
		private static readonly Type t1 = typeof(T1);
		private static readonly Type t2 = typeof(T2);
		private static readonly Type t3 = typeof(T3);

		public StoredProcedure(string name) : base(name)
		{ 
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
		}

		public StoredProcedure(string schema, string name) : base(schema, name)
		{ 
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
		}
		
		internal StoredProcedure(StoredProcedure toClone)
			: base(toClone.Schema, toClone.Name, toClone.Parameters, toClone.OutputParameterSetters) 
		{ 
			Contract.Requires(toClone != null);
		}
		
		protected StoredProcedure(string schema, 
            string name,
            ImmutableList<SqlParameter> parameters,
            ImmutableDictionary<string, Action<object>> outputParameterSetters)
			: base(schema, name, parameters, outputParameterSetters)
        {
		}
		
		public new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>> Execute(IDbConnection connection, int? timeout = null)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>>() != null);

			return Execute(connection, CancellationToken.None, timeout);
		}

		Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>> Execute(IDbConnection connection, CancellationToken token, int? timeout = null)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>>() != null);

			var results = connection.Execute(FullName,
				token, 
				timeout,
				Parameters,
				new[] { t1, t2, t3 });
			TransferOutputParameters();

			return Tuple.Create((IEnumerable<T1>)results[t1], (IEnumerable<T2>)results[t2], (IEnumerable<T3>)results[t3]); 
		}

		public new Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>> ExecuteAsync(IDbConnection connection, int? timeout = null)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>>>() != null);

			return ExecuteAsync(connection, CancellationToken.None, timeout);
		}

		public new Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>> ExecuteAsync(IDbConnection connection, CancellationToken token, int? timeout = null)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>>>() != null);

			return Task.Run(() => Execute(connection, token, timeout));
		}

		protected override StoredProcedure CloneCore(ImmutableList<SqlParameter> parameters,
            ImmutableDictionary<string, Action<object>> outputParameterSetters)
        {
            return new StoredProcedure<T1, T2, T3>(Schema, Name, parameters, outputParameterSetters);
        }
	}
	#endregion

	#region StoredProcedure<T1, T2, T3, T4>
	public class StoredProcedure<T1, T2, T3, T4> : StoredProcedure<T1, T2, T3>
	{
		private static readonly Type t1 = typeof(T1);
		private static readonly Type t2 = typeof(T2);
		private static readonly Type t3 = typeof(T3);
		private static readonly Type t4 = typeof(T4);

		public StoredProcedure(string name) : base(name)
		{ 
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
		}

		public StoredProcedure(string schema, string name) : base(schema, name)
		{ 
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
		}
		
		internal StoredProcedure(StoredProcedure toClone)
			: base(toClone.Schema, toClone.Name, toClone.Parameters, toClone.OutputParameterSetters) 
		{ 
			Contract.Requires(toClone != null);
		}
		
		protected StoredProcedure(string schema, 
            string name,
            ImmutableList<SqlParameter> parameters,
            ImmutableDictionary<string, Action<object>> outputParameterSetters)
			: base(schema, name, parameters, outputParameterSetters)
        {
		}
		
		public new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>> Execute(IDbConnection connection, int? timeout = null)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>>() != null);

			return Execute(connection, CancellationToken.None, timeout);
		}

		Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>> Execute(IDbConnection connection, CancellationToken token, int? timeout = null)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>>() != null);

			var results = connection.Execute(FullName,
				token, 
				timeout,
				Parameters,
				new[] { t1, t2, t3, t4 });
			TransferOutputParameters();

			return Tuple.Create((IEnumerable<T1>)results[t1], (IEnumerable<T2>)results[t2], (IEnumerable<T3>)results[t3], (IEnumerable<T4>)results[t4]); 
		}

		public new Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>> ExecuteAsync(IDbConnection connection, int? timeout = null)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>>>() != null);

			return ExecuteAsync(connection, CancellationToken.None, timeout);
		}

		public new Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>> ExecuteAsync(IDbConnection connection, CancellationToken token, int? timeout = null)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>>>() != null);

			return Task.Run(() => Execute(connection, token, timeout));
		}

		protected override StoredProcedure CloneCore(ImmutableList<SqlParameter> parameters,
            ImmutableDictionary<string, Action<object>> outputParameterSetters)
        {
            return new StoredProcedure<T1, T2, T3, T4>(Schema, Name, parameters, outputParameterSetters);
        }
	}
	#endregion

	#region StoredProcedure<T1, T2, T3, T4, T5>
	public class StoredProcedure<T1, T2, T3, T4, T5> : StoredProcedure<T1, T2, T3, T4>
	{
		private static readonly Type t1 = typeof(T1);
		private static readonly Type t2 = typeof(T2);
		private static readonly Type t3 = typeof(T3);
		private static readonly Type t4 = typeof(T4);
		private static readonly Type t5 = typeof(T5);

		public StoredProcedure(string name) : base(name)
		{ 
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
		}

		public StoredProcedure(string schema, string name) : base(schema, name)
		{ 
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
		}
		
		internal StoredProcedure(StoredProcedure toClone)
			: base(toClone.Schema, toClone.Name, toClone.Parameters, toClone.OutputParameterSetters) 
		{ 
			Contract.Requires(toClone != null);
		}
		
		protected StoredProcedure(string schema, 
            string name,
            ImmutableList<SqlParameter> parameters,
            ImmutableDictionary<string, Action<object>> outputParameterSetters)
			: base(schema, name, parameters, outputParameterSetters)
        {
		}
		
		public new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>> Execute(IDbConnection connection, int? timeout = null)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>>() != null);

			return Execute(connection, CancellationToken.None, timeout);
		}

		Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>> Execute(IDbConnection connection, CancellationToken token, int? timeout = null)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>>() != null);

			var results = connection.Execute(FullName,
				token, 
				timeout,
				Parameters,
				new[] { t1, t2, t3, t4, t5 });
			TransferOutputParameters();

			return Tuple.Create((IEnumerable<T1>)results[t1], (IEnumerable<T2>)results[t2], (IEnumerable<T3>)results[t3], (IEnumerable<T4>)results[t4], (IEnumerable<T5>)results[t5]); 
		}

		public new Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>> ExecuteAsync(IDbConnection connection, int? timeout = null)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>>>() != null);

			return ExecuteAsync(connection, CancellationToken.None, timeout);
		}

		public new Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>> ExecuteAsync(IDbConnection connection, CancellationToken token, int? timeout = null)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>>>() != null);

			return Task.Run(() => Execute(connection, token, timeout));
		}

		protected override StoredProcedure CloneCore(ImmutableList<SqlParameter> parameters,
            ImmutableDictionary<string, Action<object>> outputParameterSetters)
        {
            return new StoredProcedure<T1, T2, T3, T4, T5>(Schema, Name, parameters, outputParameterSetters);
        }
	}
	#endregion

	#region StoredProcedure<T1, T2, T3, T4, T5, T6>
	public class StoredProcedure<T1, T2, T3, T4, T5, T6> : StoredProcedure<T1, T2, T3, T4, T5>
	{
		private static readonly Type t1 = typeof(T1);
		private static readonly Type t2 = typeof(T2);
		private static readonly Type t3 = typeof(T3);
		private static readonly Type t4 = typeof(T4);
		private static readonly Type t5 = typeof(T5);
		private static readonly Type t6 = typeof(T6);

		public StoredProcedure(string name) : base(name)
		{ 
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
		}

		public StoredProcedure(string schema, string name) : base(schema, name)
		{ 
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
		}
		
		internal StoredProcedure(StoredProcedure toClone)
			: base(toClone.Schema, toClone.Name, toClone.Parameters, toClone.OutputParameterSetters) 
		{ 
			Contract.Requires(toClone != null);
		}
		
		protected StoredProcedure(string schema, 
            string name,
            ImmutableList<SqlParameter> parameters,
            ImmutableDictionary<string, Action<object>> outputParameterSetters)
			: base(schema, name, parameters, outputParameterSetters)
        {
		}
		
		public new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>> Execute(IDbConnection connection, int? timeout = null)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>>() != null);

			return Execute(connection, CancellationToken.None, timeout);
		}

		Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>> Execute(IDbConnection connection, CancellationToken token, int? timeout = null)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>>() != null);

			var results = connection.Execute(FullName,
				token, 
				timeout,
				Parameters,
				new[] { t1, t2, t3, t4, t5, t6 });
			TransferOutputParameters();

			return Tuple.Create((IEnumerable<T1>)results[t1], (IEnumerable<T2>)results[t2], (IEnumerable<T3>)results[t3], (IEnumerable<T4>)results[t4], (IEnumerable<T5>)results[t5], (IEnumerable<T6>)results[t6]); 
		}

		public new Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>> ExecuteAsync(IDbConnection connection, int? timeout = null)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>>>() != null);

			return ExecuteAsync(connection, CancellationToken.None, timeout);
		}

		public new Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>> ExecuteAsync(IDbConnection connection, CancellationToken token, int? timeout = null)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>>>() != null);

			return Task.Run(() => Execute(connection, token, timeout));
		}

		protected override StoredProcedure CloneCore(ImmutableList<SqlParameter> parameters,
            ImmutableDictionary<string, Action<object>> outputParameterSetters)
        {
            return new StoredProcedure<T1, T2, T3, T4, T5, T6>(Schema, Name, parameters, outputParameterSetters);
        }
	}
	#endregion

	#region StoredProcedure<T1, T2, T3, T4, T5, T6, T7>
	public class StoredProcedure<T1, T2, T3, T4, T5, T6, T7> : StoredProcedure<T1, T2, T3, T4, T5, T6>
	{
		private static readonly Type t1 = typeof(T1);
		private static readonly Type t2 = typeof(T2);
		private static readonly Type t3 = typeof(T3);
		private static readonly Type t4 = typeof(T4);
		private static readonly Type t5 = typeof(T5);
		private static readonly Type t6 = typeof(T6);
		private static readonly Type t7 = typeof(T7);

		public StoredProcedure(string name) : base(name)
		{ 
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
		}

		public StoredProcedure(string schema, string name) : base(schema, name)
		{ 
            Contract.Requires(!string.IsNullOrWhiteSpace(schema));
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
		}
		
		internal StoredProcedure(StoredProcedure toClone)
			: base(toClone.Schema, toClone.Name, toClone.Parameters, toClone.OutputParameterSetters) 
		{ 
			Contract.Requires(toClone != null);
		}
		
		protected StoredProcedure(string schema, 
            string name,
            ImmutableList<SqlParameter> parameters,
            ImmutableDictionary<string, Action<object>> outputParameterSetters)
			: base(schema, name, parameters, outputParameterSetters)
        {
		}
		
		public new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>> Execute(IDbConnection connection, int? timeout = null)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>>() != null);

			return Execute(connection, CancellationToken.None, timeout);
		}

		Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>> Execute(IDbConnection connection, CancellationToken token, int? timeout = null)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>>() != null);

			var results = connection.Execute(FullName,
				token, 
				timeout,
				Parameters,
				new[] { t1, t2, t3, t4, t5, t6, t7 });
			TransferOutputParameters();

			return Tuple.Create((IEnumerable<T1>)results[t1], (IEnumerable<T2>)results[t2], (IEnumerable<T3>)results[t3], (IEnumerable<T4>)results[t4], (IEnumerable<T5>)results[t5], (IEnumerable<T6>)results[t6], (IEnumerable<T7>)results[t7]); 
		}

		public new Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>> ExecuteAsync(IDbConnection connection, int? timeout = null)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>>>() != null);

			return ExecuteAsync(connection, CancellationToken.None, timeout);
		}

		public new Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>> ExecuteAsync(IDbConnection connection, CancellationToken token, int? timeout = null)
		{
			Contract.Requires(connection != null);
			Contract.Ensures(Contract.Result<Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>>>() != null);

			return Task.Run(() => Execute(connection, token, timeout));
		}

		protected override StoredProcedure CloneCore(ImmutableList<SqlParameter> parameters,
            ImmutableDictionary<string, Action<object>> outputParameterSetters)
        {
            return new StoredProcedure<T1, T2, T3, T4, T5, T6, T7>(Schema, Name, parameters, outputParameterSetters);
        }
	}
	#endregion

}
