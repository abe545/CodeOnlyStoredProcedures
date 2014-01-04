using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.Contracts;

namespace CodeOnlyStoredProcedure
{
    public static partial class StoredProcedureExtensions
    {
		#region WithResults
		
		public static StoredProcedure<T1> WithResults<T1>(this StoredProcedure sp)
		{
            Contract.Requires(sp != null);
			Contract.Requires(sp.GetType() == typeof(StoredProcedure));
			Contract.Ensures(Contract.Result<StoredProcedure<T1>>() != null);

			return new StoredProcedure<T1>(sp);
		}

		public static StoredProcedure<T1, T2> WithResults<T1, T2>(this StoredProcedure sp)
		{
            Contract.Requires(sp != null);
			Contract.Requires(sp.GetType() == typeof(StoredProcedure));
			Contract.Ensures(Contract.Result<StoredProcedure<T1, T2>>() != null);

			return new StoredProcedure<T1, T2>(sp);
		}

		public static StoredProcedure<T1, T2, T3> WithResults<T1, T2, T3>(this StoredProcedure sp)
		{
            Contract.Requires(sp != null);
			Contract.Requires(sp.GetType() == typeof(StoredProcedure));
			Contract.Ensures(Contract.Result<StoredProcedure<T1, T2, T3>>() != null);

			return new StoredProcedure<T1, T2, T3>(sp);
		}

		public static StoredProcedure<T1, T2, T3, T4> WithResults<T1, T2, T3, T4>(this StoredProcedure sp)
		{
            Contract.Requires(sp != null);
			Contract.Requires(sp.GetType() == typeof(StoredProcedure));
			Contract.Ensures(Contract.Result<StoredProcedure<T1, T2, T3, T4>>() != null);

			return new StoredProcedure<T1, T2, T3, T4>(sp);
		}

		public static StoredProcedure<T1, T2, T3, T4, T5> WithResults<T1, T2, T3, T4, T5>(this StoredProcedure sp)
		{
            Contract.Requires(sp != null);
			Contract.Requires(sp.GetType() == typeof(StoredProcedure));
			Contract.Ensures(Contract.Result<StoredProcedure<T1, T2, T3, T4, T5>>() != null);

			return new StoredProcedure<T1, T2, T3, T4, T5>(sp);
		}

		public static StoredProcedure<T1, T2, T3, T4, T5, T6> WithResults<T1, T2, T3, T4, T5, T6>(this StoredProcedure sp)
		{
            Contract.Requires(sp != null);
			Contract.Requires(sp.GetType() == typeof(StoredProcedure));
			Contract.Ensures(Contract.Result<StoredProcedure<T1, T2, T3, T4, T5, T6>>() != null);

			return new StoredProcedure<T1, T2, T3, T4, T5, T6>(sp);
		}

		public static StoredProcedure<T1, T2, T3, T4, T5, T6, T7> WithResults<T1, T2, T3, T4, T5, T6, T7>(this StoredProcedure sp)
		{
            Contract.Requires(sp != null);
			Contract.Requires(sp.GetType() == typeof(StoredProcedure));
			Contract.Ensures(Contract.Result<StoredProcedure<T1, T2, T3, T4, T5, T6, T7>>() != null);

			return new StoredProcedure<T1, T2, T3, T4, T5, T6, T7>(sp);
		}
		#endregion
	}
}