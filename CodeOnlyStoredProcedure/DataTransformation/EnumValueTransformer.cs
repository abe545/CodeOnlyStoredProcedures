using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace CodeOnlyStoredProcedure.DataTransformation
{
    /// <summary>
    /// Transforms a value in the db to an Enumuerated value
    /// </summary>
    class EnumValueTransformer : IDataTransformer
    {
        private static readonly Type[] validTypes = new[]
            {
                typeof(string),
                typeof(int),
                typeof(long),
                typeof(uint),
                typeof(ulong),
                typeof(sbyte),
                typeof(short),
                typeof(byte),
                typeof(ushort)
            };

        /// <summary>
        /// Determines if the given value can be transformed.
        /// </summary>
        /// <param name="value">The object to attempt to transform</param>
        /// <param name="targetType">The type of the property that is receiving the value</param>
        /// <param name="propertyAttributes">All attributes applied to the property</param>
        /// <returns>True if the value can be transformed to an Enum type, and the target property
        /// is an Enum type.</returns>
        public bool CanTransform(object value, Type targetType, IEnumerable<Attribute> propertyAttributes)
        {
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                targetType = targetType.GetGenericArguments().Single();

            return value != null && targetType.IsEnum && validTypes.Contains(value.GetType());
        }

        /// <summary>
        /// Transforms the value to an enum value
        /// </summary>
        /// <param name="value">The numeric (or string) value to transform</param>
        /// <param name="targetType">The type of the enum</param>
        /// <param name="propertyAttributes">The list of attributes applied to the property</param>
        /// <returns>The converted enum value.</returns>
        public object Transform(object value, Type targetType, IEnumerable<Attribute> propertyAttributes)
        {
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                targetType = targetType.GetGenericArguments().Single();

            // the interpreter doesn't realize that GetGenericArguments can't return a null value
            Contract.Assume(targetType != null);

            var valueType = value.GetType();

            if (valueType == typeof(string))
                return Enum.Parse(targetType, (string)value);

            return Enum.ToObject(targetType, value);
        }
    }
}
