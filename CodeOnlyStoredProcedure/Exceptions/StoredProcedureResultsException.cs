using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;

namespace CodeOnlyStoredProcedure
{
    /// <summary>
    /// Exception thrown if the results of a <see cref="StoredProcedure"/> are unexpected.
    /// </summary>
    [Serializable]
    public sealed class StoredProcedureResultsException : StoredProcedureException
    {
        /// <summary>
        /// Gets the names of the properties that were not found in the result set.
        /// </summary>
        public IEnumerable<string> MissingProperties { get; private set; }

        /// <summary>
        /// Gets the types that were missing from the results.
        /// </summary>
        public IEnumerable<Type> MissingChildTypes { get; private set; }

        /// <summary>
        /// Creates a new StoredProcedureResultsException
        /// </summary>
        /// <param name="resultType">The <see cref="Type"/> of results that were expected.</param>
        /// <param name="propertyNames">The names of the properties that were not found in the result set, but
        /// were expected to be.</param>
        public StoredProcedureResultsException(Type resultType, params string[] propertyNames)
            : base(BuildMessage(resultType, propertyNames))
        {
            Contract.Requires(resultType    != null);
            Contract.Requires(propertyNames != null && propertyNames.Length > 0);

            this.MissingProperties = propertyNames;
        }

        /// <summary>
        /// Creates a new StoredProcedureResultsException
        /// </summary>
        /// <param name="resultType">The <see cref="Type"/> of results that were expected.</param>
        /// <param name="missingChildTypes">The types that were expected, but were not found in the results.</param>
        public StoredProcedureResultsException(Type resultType, params Type[] missingChildTypes)
            : base(BuildMessage(resultType, missingChildTypes))
        {
            Contract.Requires(resultType        != null);
            Contract.Requires(missingChildTypes != null && missingChildTypes.Length > 0);

            this.MissingChildTypes = missingChildTypes;
        }

        private static string BuildMessage(Type resultType, string[] propertyNames)
        {
            Contract.Requires(resultType != null);
            Contract.Requires(propertyNames != null && propertyNames.Length > 0);
            Contract.Ensures(!string.IsNullOrEmpty(Contract.Result<string>()));

            string props, cols = "columns", were = "were";
            if (propertyNames.Length == 1)
            {
                cols  = "column";
                were  = "was";
                props = propertyNames[0];
            }
            else if (propertyNames.Length == 2)
                props = propertyNames[0] + " or " + propertyNames[1];
            else
            {
                var sb = new StringBuilder();
                for (int i = 0; i < propertyNames.Length - 1; i++)
                {
                    sb.Append(propertyNames[i]);
                    sb.Append(", ");
                }

                sb.Append("or ");
                sb.Append(propertyNames[propertyNames.Length - 1]);
                props = sb.ToString();
            }

            return string.Format(
                "No {0} with name {1} {2} found in the result set for type {3}.\nThis property will be ignored if it is decorated with a NotMappedAttribute.\nYou can also map the property to a different column in the result set with the ColumnAttribute.\nIf the stored procedure can sometimes return the column, decorate the column with the OptionalAttribute.",
                cols,
                props,
                were,
                resultType.Name);
        }

        private static string BuildMessage(Type resultType, Type[] missingTypes)
        {
            Contract.Requires(resultType != null);
            Contract.Requires(missingTypes != null && missingTypes.Length > 0);
            Contract.Ensures(!string.IsNullOrEmpty(Contract.Result<string>()));
            
            string types, ts = "types";
            if (missingTypes.Length == 1)
            {
                ts = "type";
                types = missingTypes[0].Name;
            }
            else if (missingTypes.Length == 2)
                types = missingTypes[0].Name + " or " + missingTypes[1].Name;
            else
            {
                var sb = new StringBuilder();
                for (int i = 0; i < missingTypes.Length - 1; i++)
                {
                    sb.Append(missingTypes[i].Name);
                    sb.Append(", ");
                }

                sb.Append("or ");
                sb.Append(missingTypes[missingTypes.Length - 1].Name);
                types = sb.ToString();
            }

            return string.Format(
                "No result sets were found that match the {0} {1} when parsing {2}. If the child collection is not returned for this model, you should mark it with the NotMapped attribute.",
                ts,
                types,
                resultType.Name);
        }
    }
}
