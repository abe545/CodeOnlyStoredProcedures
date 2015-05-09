using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.Contracts;

namespace CodeOnlyStoredProcedure
{
    public static partial class StoredProcedureExtensions
    {
		#region WithResults
		
		/// <summary>Clones the given <see cref="StoredProcedure" /> into one that returns the given results.</summary>
		/// <typeparam name="T1">The type of the first result set returned by the stored procedure.</typeparam>
		/// <param name="sp">The <see cref="StoredProcedure" /> to clone.</param>
		/// <returns>A copy of the <see cref="StoredProcedure" />, but that will return the given results.</returns>
        /// <remarks>StoredProcedures are immutable, so all the Fluent API methods return copies.</remarks>
		/// <example>
		/// <code language="cs">
		/// var sp = StoredProcedure.Create("usp_getPeople").WithResults&lt;Person&gt;();
		/// </code>
		/// </example>
		public static StoredProcedure<T1> WithResults<T1>(this StoredProcedure sp)
		{
            Contract.Requires(sp != null);
			Contract.Requires(sp.GetType() == typeof(StoredProcedure));
			Contract.Ensures(Contract.Result<StoredProcedure<T1>>() != null);

			return new StoredProcedure<T1>(sp);
		}

		/// <summary>Clones the given <see cref="StoredProcedure" /> into one that returns the given results.</summary>
		/// <typeparam name="T1">The type of the first result set returned by the stored procedure.</typeparam>
		/// <typeparam name="T2">The type of the second result set returned by the stored procedure.</typeparam>
		/// <param name="sp">The <see cref="StoredProcedure" /> to clone.</param>
		/// <returns>A copy of the <see cref="StoredProcedure" />, but that will return the given results.</returns>
        /// <remarks>StoredProcedures are immutable, so all the Fluent API methods return copies.</remarks>
				public static StoredProcedure<T1, T2> WithResults<T1, T2>(this StoredProcedure sp)
		{
            Contract.Requires(sp != null);
			Contract.Requires(sp.GetType() == typeof(StoredProcedure));
			Contract.Ensures(Contract.Result<StoredProcedure<T1, T2>>() != null);

			return new StoredProcedure<T1, T2>(sp);
		}

		/// <summary>Clones the given <see cref="StoredProcedure" /> into one that returns the given results.</summary>
		/// <typeparam name="T1">The type of the first result set returned by the stored procedure.</typeparam>
		/// <typeparam name="T2">The type of the second result set returned by the stored procedure.</typeparam>
		/// <typeparam name="T3">The type of the third result set returned by the stored procedure.</typeparam>
		/// <param name="sp">The <see cref="StoredProcedure" /> to clone.</param>
		/// <returns>A copy of the <see cref="StoredProcedure" />, but that will return the given results.</returns>
        /// <remarks>StoredProcedures are immutable, so all the Fluent API methods return copies.</remarks>
				public static StoredProcedure<T1, T2, T3> WithResults<T1, T2, T3>(this StoredProcedure sp)
		{
            Contract.Requires(sp != null);
			Contract.Requires(sp.GetType() == typeof(StoredProcedure));
			Contract.Ensures(Contract.Result<StoredProcedure<T1, T2, T3>>() != null);

			return new StoredProcedure<T1, T2, T3>(sp);
		}

		/// <summary>Clones the given <see cref="StoredProcedure" /> into one that returns the given results.</summary>
		/// <typeparam name="T1">The type of the first result set returned by the stored procedure.</typeparam>
		/// <typeparam name="T2">The type of the second result set returned by the stored procedure.</typeparam>
		/// <typeparam name="T3">The type of the third result set returned by the stored procedure.</typeparam>
		/// <typeparam name="T4">The type of the fourth result set returned by the stored procedure.</typeparam>
		/// <param name="sp">The <see cref="StoredProcedure" /> to clone.</param>
		/// <returns>A copy of the <see cref="StoredProcedure" />, but that will return the given results.</returns>
        /// <remarks>StoredProcedures are immutable, so all the Fluent API methods return copies.</remarks>
				public static StoredProcedure<T1, T2, T3, T4> WithResults<T1, T2, T3, T4>(this StoredProcedure sp)
		{
            Contract.Requires(sp != null);
			Contract.Requires(sp.GetType() == typeof(StoredProcedure));
			Contract.Ensures(Contract.Result<StoredProcedure<T1, T2, T3, T4>>() != null);

			return new StoredProcedure<T1, T2, T3, T4>(sp);
		}

		/// <summary>Clones the given <see cref="StoredProcedure" /> into one that returns the given results.</summary>
		/// <typeparam name="T1">The type of the first result set returned by the stored procedure.</typeparam>
		/// <typeparam name="T2">The type of the second result set returned by the stored procedure.</typeparam>
		/// <typeparam name="T3">The type of the third result set returned by the stored procedure.</typeparam>
		/// <typeparam name="T4">The type of the fourth result set returned by the stored procedure.</typeparam>
		/// <typeparam name="T5">The type of the fifth result set returned by the stored procedure.</typeparam>
		/// <param name="sp">The <see cref="StoredProcedure" /> to clone.</param>
		/// <returns>A copy of the <see cref="StoredProcedure" />, but that will return the given results.</returns>
        /// <remarks>StoredProcedures are immutable, so all the Fluent API methods return copies.</remarks>
				public static StoredProcedure<T1, T2, T3, T4, T5> WithResults<T1, T2, T3, T4, T5>(this StoredProcedure sp)
		{
            Contract.Requires(sp != null);
			Contract.Requires(sp.GetType() == typeof(StoredProcedure));
			Contract.Ensures(Contract.Result<StoredProcedure<T1, T2, T3, T4, T5>>() != null);

			return new StoredProcedure<T1, T2, T3, T4, T5>(sp);
		}

		/// <summary>Clones the given <see cref="StoredProcedure" /> into one that returns the given results.</summary>
		/// <typeparam name="T1">The type of the first result set returned by the stored procedure.</typeparam>
		/// <typeparam name="T2">The type of the second result set returned by the stored procedure.</typeparam>
		/// <typeparam name="T3">The type of the third result set returned by the stored procedure.</typeparam>
		/// <typeparam name="T4">The type of the fourth result set returned by the stored procedure.</typeparam>
		/// <typeparam name="T5">The type of the fifth result set returned by the stored procedure.</typeparam>
		/// <typeparam name="T6">The type of the sixth result set returned by the stored procedure.</typeparam>
		/// <param name="sp">The <see cref="StoredProcedure" /> to clone.</param>
		/// <returns>A copy of the <see cref="StoredProcedure" />, but that will return the given results.</returns>
        /// <remarks>StoredProcedures are immutable, so all the Fluent API methods return copies.</remarks>
				public static StoredProcedure<T1, T2, T3, T4, T5, T6> WithResults<T1, T2, T3, T4, T5, T6>(this StoredProcedure sp)
		{
            Contract.Requires(sp != null);
			Contract.Requires(sp.GetType() == typeof(StoredProcedure));
			Contract.Ensures(Contract.Result<StoredProcedure<T1, T2, T3, T4, T5, T6>>() != null);

			return new StoredProcedure<T1, T2, T3, T4, T5, T6>(sp);
		}

		/// <summary>Clones the given <see cref="StoredProcedure" /> into one that returns the given results.</summary>
		/// <typeparam name="T1">The type of the first result set returned by the stored procedure.</typeparam>
		/// <typeparam name="T2">The type of the second result set returned by the stored procedure.</typeparam>
		/// <typeparam name="T3">The type of the third result set returned by the stored procedure.</typeparam>
		/// <typeparam name="T4">The type of the fourth result set returned by the stored procedure.</typeparam>
		/// <typeparam name="T5">The type of the fifth result set returned by the stored procedure.</typeparam>
		/// <typeparam name="T6">The type of the sixth result set returned by the stored procedure.</typeparam>
		/// <typeparam name="T7">The type of the seventh result set returned by the stored procedure.</typeparam>
		/// <param name="sp">The <see cref="StoredProcedure" /> to clone.</param>
		/// <returns>A copy of the <see cref="StoredProcedure" />, but that will return the given results.</returns>
        /// <remarks>StoredProcedures are immutable, so all the Fluent API methods return copies.</remarks>
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