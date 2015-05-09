using System.Diagnostics.Contracts;

namespace CodeOnlyStoredProcedure
{
    public static partial class StoredProcedureExtensions
    {
        /// <summary>
        /// Clones the given <see cref="StoredProcedure"/> with a new <see cref="IDataTransformer"/> that 
        /// will be used to transform data returned from the database.
        /// </summary>
        /// <typeparam name="TSP">The <see cref="StoredProcedure"/> type that this is applied to. This is needed so the fluent API works correctly.</typeparam>
        /// <param name="sp">The <see cref="StoredProcedure"/> to clone and associated the transformer with.</param>
        /// <param name="transformer">The <see cref="IDataTransformer"/> to use to transform all data returned by the database.</param>
        /// <returns>A copy of the <see cref="StoredProcedure"/> with the <see cref="IDataTransformer"/> associated.</returns>
        /// <remarks>Every column returned will attempt to transform data through this transformer, unless all transformers used implement 
        /// <see cref="IDataTransformer{T}"/>, in which case only the generic <see cref="IDataTransformer{T}.Transform"/> will be called
        /// for columns that are the matching type for the transformer.</remarks>
        /// <example>
        /// <code language="cs">
        /// var sp = StoredProcedure.Create("usp_GetPeople")
        ///                         .WithResults&lt;Person&gt;()
        ///                         .WithDataTransformer(new InternAllStringsTransformer());
        /// </code>
        /// </example>
        public static TSP WithDataTransformer<TSP>(this TSP sp, IDataTransformer transformer)
            where TSP : StoredProcedure
        {
            Contract.Requires(sp != null);
            Contract.Requires(transformer != null);
            Contract.Ensures(Contract.Result<TSP>() != null);

            return (TSP)sp.CloneWith(transformer);
        }
    }
}