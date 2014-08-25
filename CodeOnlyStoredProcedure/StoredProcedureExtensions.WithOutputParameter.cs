using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.Contracts;

namespace CodeOnlyStoredProcedure
{
    public static partial class StoredProcedureExtensions
    {
        /// <summary>
        /// Creates a clone of the given <see cref="StoredProcedure"/> with an output parameter.
        /// </summary>
        /// <typeparam name="TSP">The type of StoredProcedure to add an output parameter to.</typeparam>
        /// <typeparam name="TValue">The type of output parameter.</typeparam>
        /// <param name="sp">The <see cref="StoredProcedure"/> to add an output parameter to.</param>
        /// <param name="name">The name of the output parameter.</param>
        /// <param name="setter">The setter to call when the stored procedure returns the parameter.</param>
        /// <param name="size">The size expected for the Sql data type. This can normally be omitted.</param>
        /// <param name="scale">The scale expected for the Sql data type. This can normally be omitted.</param>
        /// <param name="precision">The precision expected for the Sql data type. This can normally be omitted.</param>
        /// <returns>A copy of the <see cref="StoredProcedure"/> with the parameter setup.</returns>
        public static TSP WithOutputParameter<TSP, TValue>(this TSP sp,
            string         name,
            Action<TValue> setter,
            int?           size      = null,
            byte?          scale     = null,
            byte?          precision = null)
            where TSP : StoredProcedure
        {
            Contract.Requires(sp                     != null);
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
            Contract.Requires(setter                 != null);
            Contract.Ensures (Contract.Result<TSP>() != null);

            return (TSP)sp.CloneWith(
                new SqlParameter
                {
                    ParameterName = name,
                    Direction     = ParameterDirection.Output
                }.AddPrecisison(size, scale, precision),
                o => setter((TValue)o));
        }

        /// <summary>
        /// Creates a clone of the given <see cref="StoredProcedure"/> with an output parameter.
        /// </summary>
        /// <typeparam name="TSP">The type of StoredProcedure to add an output parameter to.</typeparam>
        /// <typeparam name="TValue">The type of output parameter.</typeparam>
        /// <param name="sp">The <see cref="StoredProcedure"/> to add an output parameter to.</param>
        /// <param name="name">The name of the output parameter.</param>
        /// <param name="setter">The setter to call when the stored procedure returns the parameter.</param>
        /// <param name="dbType">The name of the Sql data type.</param>
        /// <param name="size">The size expected for the Sql data type. This can normally be omitted.</param>
        /// <param name="scale">The scale expected for the Sql data type. This can normally be omitted.</param>
        /// <param name="precision">The precision expected for the Sql data type. This can normally be omitted.</param>
        /// <returns>A copy of the <see cref="StoredProcedure"/> with the parameter setup.</returns>
        public static TSP WithOutputParameter<TSP, TValue>(this TSP sp,
            string         name,
            Action<TValue> setter,
            SqlDbType      dbType,
            int?           size      = null,
            byte?          scale     = null,
            byte?          precision = null)
            where TSP : StoredProcedure
        {
            Contract.Requires(sp                     != null);
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
            Contract.Requires(setter                 != null);
            Contract.Ensures (Contract.Result<TSP>() != null);

            return (TSP)sp.CloneWith(
                new SqlParameter
                {
                    ParameterName = name,
                    Direction     = ParameterDirection.Output,
                    SqlDbType     = dbType
                }.AddPrecisison(size, scale, precision),
                o => setter((TValue)o));
        }

        internal static TSP WithOutputParameter<TSP>(this TSP sp, string name, Action<object> setter)
            where TSP : StoredProcedure
        {
            Contract.Requires(sp                     != null);
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
            Contract.Requires(setter                 != null);
            Contract.Ensures (Contract.Result<TSP>() != null);

            return (TSP)sp.CloneWith(
                new SqlParameter
                {
                    ParameterName = name,
                    Direction     = ParameterDirection.Output
                },
                setter);
        }
    }
}
