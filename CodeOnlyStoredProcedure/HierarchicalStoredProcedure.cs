using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using CodeOnlyStoredProcedure.RowFactory;

namespace CodeOnlyStoredProcedure
{
    /// <summary>
    /// Executes a StoredProcedure that returns hierarchical result sets.
    /// </summary>
    /// <typeparam name="T">The parent result set to return.</typeparam>
    public sealed class HierarchicalStoredProcedure<T> : StoredProcedure<T>
    {
        private readonly IEnumerable<Type> resultTypesInOrder;

        /// <summary>
        /// Creates a hierarchical <see cref="StoredProcedure"/> with the given <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the Stored Procedure to execute.</param>
        /// <param name="resultTypesInOrder">The types (in order) that will be composed into a hierarchical result set.</param>
        public HierarchicalStoredProcedure(string name, IEnumerable<Type> resultTypesInOrder)
            : base(name)
        {
            Contract.Requires(typeof(T).IsValidResultType());
            Contract.Requires(!string.IsNullOrEmpty(name));
            Contract.Requires(resultTypesInOrder != null && Contract.ForAll(resultTypesInOrder, t => t.IsValidResultType()));

            this.resultTypesInOrder = resultTypesInOrder;
        }

        /// <summary>
        /// Creates a hierarchical <see cref="StoredProcedure"/> with the given <paramref name="name"/> and <paramref name="schema"/>.
        /// </summary>
        /// <param name="name">The name of the Stored Procedure to execute.</param>
        /// <param name="schema">The schema of the Stored Procedure to execute.</param>
        /// <param name="resultTypesInOrder">The types (in order) that will be composed into a hierarchical result set.</param>
        public HierarchicalStoredProcedure(string schema, string name, IEnumerable<Type> resultTypesInOrder)
            : base(schema, name)
        {
            Contract.Requires(typeof(T).IsValidResultType());
            Contract.Requires(!string.IsNullOrEmpty(name));
            Contract.Requires(!string.IsNullOrEmpty(schema));
            Contract.Requires(resultTypesInOrder != null && Contract.ForAll(resultTypesInOrder, t => t.IsValidResultType()));

            this.resultTypesInOrder = resultTypesInOrder;
        }

        internal HierarchicalStoredProcedure(
            string schema, 
            string name,
            IEnumerable<IStoredProcedureParameter> parameters,
            IEnumerable<IDataTransformer> dataTransformers,
            IEnumerable<Type> resultTypesInOrder)
            : base(schema, name, parameters, dataTransformers)
        {
            Contract.Requires(typeof(T).IsValidResultType());
            Contract.Requires(!string.IsNullOrEmpty(name));
            Contract.Requires(!string.IsNullOrEmpty(schema));
            Contract.Requires(parameters         != null);
            Contract.Requires(dataTransformers   != null);
            Contract.Requires(resultTypesInOrder != null);
            Contract.Requires(Contract.ForAll(resultTypesInOrder, t => t.IsValidResultType()));

            this.resultTypesInOrder = resultTypesInOrder;
        }

        /// <summary>
        /// Overridden. Creates a clone of the <see cref="HierarchicalStoredProcedure{T}"/>, so parameters or transformers can be added.
        /// </summary>
        /// <param name="parameters">The <see cref="IStoredProcedureParameter"/>s to pass to the stored procedure.</param>
        /// <param name="dataTransformers">The <see cref="IDataTransformer"/>s to transform the results.</param>
        /// <returns>A clone of the stored procedure.</returns>
        protected internal override StoredProcedure CloneCore(
            IEnumerable<IStoredProcedureParameter> parameters,
            IEnumerable<IDataTransformer> dataTransformers)
        {
            return new HierarchicalStoredProcedure<T>(Schema, Name, parameters, dataTransformers, resultTypesInOrder);
        }

        /// <summary>
        /// Overridden. Creates a <see cref="IRowFactory{T}"/> to use to generate the results from the database.
        /// Will always create a row factory that parses the hierarchical relationships between the result sets.
        /// </summary>
        /// <typeparam name="TFactory">The type of model to create a factory for.</typeparam>
        /// <returns>A <see cref="IRowFactory{T}"/> that parses the hierarchical relationships between the result sets.</returns>
        protected override IRowFactory<TFactory> CreateFactory<TFactory>()
        {
            return new HierarchicalTypeRowFactory<TFactory>(resultTypesInOrder);
        }
    }
}
