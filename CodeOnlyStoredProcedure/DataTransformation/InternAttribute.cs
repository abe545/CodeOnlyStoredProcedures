using System;

namespace CodeOnlyStoredProcedure.DataTransformation
{
    /// <summary>
    /// DataTransformer Attribute that will call String.Intern on the input. 
    /// </summary>
    /// <remarks>
    /// This can potentially save a lot of memory if you have many strings that are returned often.
    /// </remarks>
    /// <seealso cref="IDataTransformerAttribute{T}"/>
    /// <example>
    /// <code language='cs'>
    /// public class DataModel
    /// {
    ///     [Intern]
    ///     public string Name { get; set; }
    /// }
    /// </code>
    /// </example>
    public class InternAttribute : DataTransformerAttributeBase, IDataTransformerAttribute<string>
    {
        /// <summary>
        /// Creates an InternAttribute, with the given application order.
        /// </summary>
        /// <param name="order">The order in which to apply the attribute. Defaults to <see cref="Int32.MaxValue"/>, so the final 
        /// string will be interned.</param>
        public InternAttribute(int order = int.MaxValue)
            : base(order)
        {
        }

        /// <summary>
        /// Returns the interned string.
        /// </summary>
        /// <param name="value">The string to intern.</param>
        /// <param name="targetType">Must be typeof(string)</param>
        /// <param name="isNullable">If the target property is a nullable of type <paramref name="targetType"/></param>
        /// <returns>The interned string</returns>
        public override object Transform(object value, Type targetType, bool isNullable)
        {
            if (targetType != typeof(string))
                throw new NotSupportedException("Can only put the InternAttribute on string properties.");

            if (value == null)
                return null;

            if (!(value is string))
                throw new NotSupportedException("Can only intern strings.");

            return string.Intern((string)value);
        }

        /// <summary>
        /// Returns the interned string.
        /// </summary>
        /// <param name="input">The value from the database.</param>
        /// <returns>The interned string</returns>
        public string Transform(string input)
        {
            return string.Intern(input);
        }
    }
}
