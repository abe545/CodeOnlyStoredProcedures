﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeOnlyStoredProcedure
{
    /// <summary>
    /// Exception thrown when setting a property on a model fails
    /// </summary>
    [Serializable]
    public class StoredProcedureColumnException : StoredProcedureException
    {
        /// <summary>
        /// Creates a new StoredProcedureColumnException
        /// </summary>
        /// <param name="propertyName">The name of the property being set</param>
        /// <param name="propertyType">The type of the property</param>
        /// <param name="value">The value that was attempted to be set on the property</param>
        /// <param name="innerException">The exception that was thrown originally</param>
        public StoredProcedureColumnException(string propertyName, Type propertyType, object value, Exception innerException)
            : base(BuildMessage(propertyName, propertyType, value), innerException)
        {
            Contract.Requires(!string.IsNullOrEmpty(propertyName));
            Contract.Requires(propertyType != null);
            Contract.Requires(innerException != null);
        }

        private static string BuildMessage(string propertyName, Type propertyType, object value)
        {
            Contract.Requires(!string.IsNullOrEmpty(propertyName));
            Contract.Requires(propertyType != null);
            Contract.Ensures(!string.IsNullOrEmpty(Contract.Result<string>()));

            var sb = new StringBuilder();

            sb.AppendFormat("Error setting [{0}] {1} property. Received value: ", propertyType.Name, propertyName);

            if (value == null || value == DBNull.Value)
                sb.Append("<NULL>.");
            else if (value is string)
                sb.AppendFormat("\"{0}\".", value);
            else
                sb.AppendFormat("[{0}] {1}.", value.GetType().Name, value.ToString());

            return sb.ToString();
        }
    }
}
