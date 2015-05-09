namespace CodeOnlyStoredProcedure.DataTransformation
{
    /// <summary>
    /// Interface that <see cref="DataTransformerAttributeBase"/> implementations should implement if they input and output
    /// the same type. This will allow more columns to be retrieved in their native type, so less boxing/unboxing
    /// has to happen. Doing so improves speed dramatically.
    /// </summary>
    /// <typeparam name="T">The type that the attribute transforms.</typeparam>
    /// <remarks>You must inherit from <see cref="DataTransformerAttributeBase"/>, otherwise the attribute will be ignored.</remarks>
    /// <example>
    /// <code language='cs'>
    /// public class ToUtcAttribute : DataTransformerAttributeBase, IDataTransformerAttribute&lt;DateTime&gt;
    /// {
    ///     public ToUtcAttribute(int order = 0) 
    ///         : base(order)
    ///     {
    ///     }
    ///     
    ///     public DateTime Transform(DateTime value) 
    ///     {
    ///         return value.ToUniversalTime();
    ///     }
    ///     
    ///     public override object Transform(object value) 
    ///     {
    ///         return Transform((DateTime)value); 
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IDataTransformerAttribute<T>
    {
        /// <summary>
        /// Transforms a given value, as specified by the implementation.
        /// </summary>
        /// <param name="value">The value to transform. Can be from a stored procedure, or already altered
        /// by a previous transformer.</param>
        /// <returns>The transformed value.</returns>
        T Transform(T value);
    }
}
