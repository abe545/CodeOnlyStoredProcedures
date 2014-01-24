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
        /// Transforms the database numeric type to the property's type. Uses <see cref="Convert.ChangeType(object, Type)"/> to convert the value.
        /// </summary>
        /// <param name="value">The value to convert</param>
        /// <param name="targetType">The type of the property</param>
        /// <returns>The converted value.</returns>
        public override object Transform(object value, Type targetType)
        {
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                if (value == null)
                    return null;

                targetType = targetType.GetGenericArguments().Single();
            }

            return Convert.ChangeType(value, targetType);
        }
    }
}
