using System;

namespace CodeOnlyStoredProcedure
{
    /// <summary>
    /// Base class for attributes that can be applied to model properties to alter how the
    /// data is returned from the database.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public abstract class DataTransformerAttributeBase : Attribute
    {
        /// <summary>
        /// The order in which the attribute is applied.
        /// </summary>
        /// <remarks>
        /// Since .NET has no way of returning attributes in the order in which they decorate
        /// a property, we must force the programmer to pass the order in which these 
        /// transformations should be applied.
        /// </remarks>
        public int Order { get; }

        /// <summary>
        /// Creates a new DataTransformer attribute.
        /// </summary>
        /// <param name="order">The order in which this attribute is applied.
        /// Since .NET has no way of returning attributes in the order in which they decorate
        /// a property, we must force the programmer to pass the order in which these 
        /// transformations should be applied.
        /// </param>
        public DataTransformerAttributeBase(int order = 0)
        {
            Order = order;
        }

        /// <summary>
        /// When implemented in a derived class, transforms the input value in some fashion.
        /// </summary>
        /// <param name="value">The value to transform. This can potentially come from
        /// another DataTransformer attribute</param>
        /// <param name="targetType">The type of the property the attribute is applied to.</param>
        /// <param name="isNullable">If the target property is a nullable of type <paramref name="targetType"/></param>
        /// <returns>The transformed value.</returns>
        public abstract object Transform(object value, Type targetType, bool isNullable);
    }
}
