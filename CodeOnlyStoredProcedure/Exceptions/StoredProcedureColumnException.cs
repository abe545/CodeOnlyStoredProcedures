using System;
using System.Diagnostics.Contracts;

namespace CodeOnlyStoredProcedure
{
    /// <summary>
    /// Exception thrown when setting a property on a model fails
    /// </summary>
    [Serializable]
    public class StoredProcedureColumnException : StoredProcedureException
    {
        /// <summary>
        /// Creates a new StoredProcedureColumnException when the return type is a single
        /// column.
        /// </summary>
        /// <param name="resultType">Type type of results expected</param>
        /// <param name="dbType">The type the database returns</param>
        /// <param name="propertyName">The name of the property being set.</param>
        public StoredProcedureColumnException(Type resultType, Type dbType, string propertyName = "result")
            : base(BuildMessage(propertyName, resultType, dbType))
        {
            Contract.Requires(resultType != null);
            Contract.Requires(dbType     != null);
            Contract.Requires(!string.IsNullOrEmpty(propertyName));
        }

        /// <summary>
        /// Creates a new StoredProcedureColumnException when the return type is a single
        /// column.
        /// </summary>
        /// <param name="resultType">Type type of results expected</param>
        /// <param name="dbType">The type the database returns</param>
        /// <param name="propertyName">The name of the property being set.</param>
        /// <param name="innerException">The exception to wrap</param>
        public StoredProcedureColumnException(Type resultType, Type dbType, Exception innerException, string propertyName = "result")
            : base(BuildMessage(propertyName, resultType, dbType), innerException)
        {
            Contract.Requires(resultType     != null);
            Contract.Requires(dbType         != null);
            Contract.Requires(innerException != null);
            Contract.Requires(!string.IsNullOrEmpty(propertyName));
        }

        private static string BuildMessage(string propertyName, Type propertyType, Type dbType)
        {
            Contract.Requires(!string.IsNullOrEmpty(propertyName));
            Contract.Requires(propertyType != null);
            Contract.Requires(dbType       != null);
            Contract.Ensures(!string.IsNullOrEmpty(Contract.Result<string>()));

            return $"Error setting [{propertyType.Name}] {propertyName}. Stored Procedure returns [{dbType.Name}].";
        }
    }
}
