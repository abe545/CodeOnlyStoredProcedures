using System;
using System.Collections.Generic;

namespace CodeOnlyStoredProcedure.DataTransformation
{
    /// <summary>
    /// An <see cref="IDataTransformer"/> that will intern all strings returned.
    /// </summary>
    public class InternAllStringsTransformer : IDataTransformer
    {
        /// <summary>
        /// Returns true if the value is a non-null string, and the target type is a string.
        /// </summary>
        /// <param name="value">The value to attempt to transform</param>
        /// <param name="targetType">The type of the property</param>
        /// <param name="isNullable">If the target property is a nullable of type <paramref name="targetType"/></param>
        /// <param name="propertyAttributes">All attributes applied to the property</param>
        /// <returns>True if the value is a string, and so is targetType, false otherwise.</returns>
        public bool CanTransform(object value, Type targetType, bool isNullable, IEnumerable<Attribute> propertyAttributes)
        {
            if (targetType != typeof(string) || !(value is string))
                return false;

            return true;
        }

        /// <summary>
        /// Returns the interned version of the string
        /// </summary>
        /// <param name="value">The string to intern</param>
        /// <param name="targetType">The type of the property</param>
        /// <param name="isNullable">If the target property is a nullable of type <paramref name="targetType"/></param>
        /// <param name="propertyAttributes">All attributes applied to the property</param>
        /// <returns>The interned string</returns>
        public object Transform(object value, Type targetType, bool isNullable, IEnumerable<Attribute> propertyAttributes)
        {
            return string.Intern((string)value);
        }
    }
}
