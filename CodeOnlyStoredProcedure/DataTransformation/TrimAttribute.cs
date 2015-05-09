using System;

namespace CodeOnlyStoredProcedure.DataTransformation
{
    /// <summary>
    /// Removes all whitespace from a string value returned from a Stored Procedure.
    /// </summary>
    /// <remarks>
    /// If the stored procedure returns " Foo  ", and [Trim] decorates a model's property,
    /// the value set on that property will be "Foo".
    /// </remarks>
    /// <seealso cref="IDataTransformerAttribute{T}"/>
    /// <example>
    /// <code language='cs'>
    /// public class DataModel
    /// {
    ///     [Trim]
    ///     public string Name { get; set; }
    /// }
    /// </code>
    /// </example>
    public class TrimAttribute : DataTransformerAttributeBase, IDataTransformerAttribute<string>
    {
        /// <summary>
        /// Creates a TrimAttribute, with the given application order.
        /// </summary>
        /// <param name="order">The order in which to apply the attribute. Defaults to 0.</param>
        public TrimAttribute(int order = 0)
            : base(order)
        {
        }

        /// <summary>
        /// Removes all whitespace from the input
        /// </summary>
        /// <param name="value">The string to trim</param>
        /// <param name="targetType">The type to transform to. Only string types are supported.</param>
        /// <param name="isNullable">If the target property is a nullable of type <paramref name="targetType"/></param>
        /// <returns>The trimmed string, or empty if a null value was passed.</returns>
        public override object Transform(object value, Type targetType, bool isNullable)
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

        /// <summary>
        /// Removes all whitespace from the input.
        /// </summary>
        /// <param name="input">The value from the database.</param>
        /// <returns>The trimmed string, or empty if a null value was passed.</returns>
        public string Transform(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            return input.Trim();
        }
    }
}
