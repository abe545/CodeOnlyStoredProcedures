using System;
using System.Diagnostics.Contracts;

namespace CodeOnlyStoredProcedure
{
    /// <summary>
    /// Base class for all exceptions thrown by this library
    /// </summary>
    [Serializable]
    public class StoredProcedureException : Exception
    {
        /// <summary>
        /// Creates a new StoredProcedureException
        /// </summary>
        /// <param name="message">The message to show to the programmer</param>
        public StoredProcedureException(string message) : base(message) 
        {
            Contract.Requires(!string.IsNullOrEmpty(message));
        }

        /// <summary>
        /// Creates a new StoredProcedureException
        /// </summary>
        /// <param name="message">The message to show to the programmer</param>
        /// <param name="innerException">The original exception</param>
        public StoredProcedureException(string message, Exception innerException)
            : base(message, innerException)
        {
            Contract.Requires(!string.IsNullOrEmpty(message));
            Contract.Requires(innerException != null);
        }
    }
}
