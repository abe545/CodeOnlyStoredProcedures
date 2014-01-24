using System;

namespace CodeOnlyStoredProcedure.DataTransformation
{
    /// <summary>
    /// Removes all whitespace from a string value
    /// </summary>
    public class TrimAttribute : DataTransformerAttributeBase
    {
        /// <summary>
        /// Removes all whitespace from the input
        /// </summary>
        /// <param name="value">The string to trim</param>
        /// <param name="targetType">The type to transform to. Only string types are supported.</param>
        /// <returns>The trimmed string, or empty if a null value was passed.</returns>
        public override object Transform(object value, Type targetType)
        {
            if (targetType != typeof(string))
                throw new NotSupportedException("Can only set the TrimAttribute on a String property");

            if (!(value is string))
            {
                if (ReferenceEquals(null, value))
                    return string.Empty;

                throw new NotSupportedException("Can only trim a string value");
            }

            var str = (string)value;
            if (string.IsNullOrWhiteSpace(str))
                return string.Empty;

            return str.Trim();
        }
    }
}
