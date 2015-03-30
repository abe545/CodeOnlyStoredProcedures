using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeOnlyStoredProcedure.DataTransformation
{
    /// <summary>
    /// Trims all strings returned by the stored procedure.
    /// </summary>
    public class TrimAllStringsTransformer : IDataTransformer<string>
    {
        /// <summary>
        /// Returns true if both value is a string, and targetType is typeof(string).
        /// </summary>
        /// <param name="value">The input value to transform.</param>
        /// <param name="targetType">The type of the property the value is being set on.</param>
        /// <param name="isNullable">If the target property is a nullable of type <paramref name="targetType"/></param>
        /// <param name="propertyAttributes">The attributes applied to the property.</param>
        /// <returns>True if both value is a string, and targetType is typeof(string); false otherwise.</returns>
        public bool CanTransform(object value, Type targetType, bool isNullable, IEnumerable<Attribute> propertyAttributes)
        {
            if (targetType != typeof(string))
                return false;

            if (ReferenceEquals(null, value))
                return true;

            return value is string;
        }

        /// <summary>
        /// Trims the whitespace from the input string
        /// </summary>
        /// <param name="value">The string to trim</param>
        /// <param name="targetType">Must be typeof(string)</param>
        /// <param name="isNullable">If the target property is a nullable of type <paramref name="targetType"/></param>
        /// <param name="propertyAttributes">The attributes applied to the property.</param>
        /// <returns>The trimmed string.</returns>
        public object Transform(object value, Type targetType, bool isNullable, IEnumerable<Attribute> propertyAttributes)
        {
            return Transform((string)value, propertyAttributes);
        }
        
        /// <summary>
        /// Trims the whitespace from the input string
        /// </summary>
        /// <param name="value">The string to trim</param>
        /// <param name="propertyAttributes">The attributes applied to the property.</param>
        /// <returns>The trimmed string.</returns>
        public string Transform(string value, IEnumerable<Attribute> propertyAttributes)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return value.Trim();
        }
    }
}
