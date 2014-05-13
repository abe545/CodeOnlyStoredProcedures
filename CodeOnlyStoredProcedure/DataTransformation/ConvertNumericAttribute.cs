using System;
using System.Linq;

namespace CodeOnlyStoredProcedure.DataTransformation
{
    /// <summary>
    /// Converts a numeric type from the Database to the type of the property
    /// </summary>
    public class ConvertNumericAttribute : DataTransformerAttributeBase
    {
        /// <summary>
        /// Creates a ConvertNumericAttribute, with the given application order.
        /// </summary>
        /// <param name="order">The order in which to apply the attribute. Defaults to 0.</param>
        public ConvertNumericAttribute(int order = 0)
            : base(order)
        {
        }

        /// <summary>
        /// Transforms the database numeric type to the property's type. Uses <see cref="Convert.ChangeType(object, Type)"/> to convert the value.
        /// </summary>
        /// <param name="value">The value to convert</param>
        /// <param name="targetType">The type of the property</param>
        /// <param name="isNullable">If the target property is a nullable of type <paramref name="targetType"/></param>
        /// <returns>The converted value.</returns>
        public override object Transform(object value, Type targetType, bool isNullable)
        {
            if (isNullable && ReferenceEquals(null, value))
                return value;

            return Convert.ChangeType(value, targetType);
        }
    }
}
