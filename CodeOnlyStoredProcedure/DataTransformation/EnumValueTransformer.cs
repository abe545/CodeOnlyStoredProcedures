using System;
using System.Collections.Generic;
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

        public bool CanTransform(object value, Type targetType, IEnumerable<Attribute> propertyAttributes)
        {
            return value != null && targetType.IsEnum && validTypes.Contains(value.GetType());
        }

        public object Transform(object value, Type targetType, IEnumerable<Attribute> propertyAttributes)
        {
            var valueType = value.GetType();

            if (valueType == typeof(string))
                return Enum.Parse(targetType, (string)value);

            return Enum.ToObject(targetType, value);
        }
    }
}
