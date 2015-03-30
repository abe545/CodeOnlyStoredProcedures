using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace CodeOnlyStoredProcedure
{
    /// <summary>
    /// An IDataTransformer can be used to apply a transformation for multiple properties. If it is
    /// set on an StoredProcedure using the <see cref="StoredProcedureExtensions.WithDataTransformer"/>
    /// extension method, then the transformation can potentially run for every property in each 
    /// model returned by that stored procedure.
    /// </summary>
    [ContractClass(typeof(IDataTransformerContract))]
    public interface IDataTransformer
    {
        /// <summary>
        /// Determines if the IDataTransformer can successfully transform the given input.
        /// </summary>
        /// <param name="value">The input value to transform. This can either be directly from
        /// the database, or the output from another transformer if multiple transformers are
        /// setup.</param>
        /// <param name="targetType">The type of the property the value is being set on.</param>
        /// <param name="isNullable">If the target property is a nullable of type <paramref name="targetType"/></param>
        /// <param name="propertyAttributes">The attributes applied to the property.</param>
        /// <returns>True if Transform will produce a valid result; false otherwise.</returns>
        [Pure]
        bool CanTransform(object value, Type targetType, bool isNullable, IEnumerable<Attribute> propertyAttributes);
        /// <summary>
        /// When implemented, transforms the input value in some way
        /// </summary>
        /// <param name="value">The input value to transform. This can either be directly from
        /// the database, or the output from another transformer if multiple transformers are
        /// setup.</param>
        /// <param name="targetType">The type of the property the value is being set on.</param>
        /// <param name="isNullable">If the target property is a nullable of type <paramref name="targetType"/></param>
        /// <param name="propertyAttributes">The attributes applied to the property.</param>
        /// <returns>The transformed value</returns>
        object Transform(object value, Type targetType, bool isNullable, IEnumerable<Attribute> propertyAttributes);
    }

    /// <summary>
    /// Interface that transformers should implement when they only operate on a single type. When this interface is implemented,
    /// and all other transformers are also strongly typed, then only this method will be called on the transformer. 
    /// </summary>
    /// <typeparam name="T">The type that this transformer operates on.</typeparam>
    [ContractClass(typeof(IDataTransformerContract<>))]
    public interface IDataTransformer<T> : IDataTransformer
    {
        /// <summary>
        /// When implemented, transforms the input value in some way
        /// </summary>
        /// <param name="value">The input value to transform. This can either be directly from
        /// the database, or the output from another transformer if multiple transformers are
        /// setup.</param>
        /// <param name="propertyAttributes">The attributes applied to the property.</param>
        /// <returns>The transformed value</returns>
        T Transform(T value, IEnumerable<Attribute> propertyAttributes);
    }

    [ContractClassFor(typeof(IDataTransformer))]
    abstract class IDataTransformerContract : IDataTransformer
    {
        public bool CanTransform(object value, Type targetType, bool isNullable, IEnumerable<Attribute> propertyAttributes)
        {
            Contract.Requires(targetType         != null);
            Contract.Requires(propertyAttributes != null);

            return false;
        }

        public object Transform(object value, Type targetType, bool isNullable, IEnumerable<Attribute> propertyAttributes)
        {
            Contract.Requires(targetType         != null);
            Contract.Requires(propertyAttributes != null);
            Contract.Requires(CanTransform(value, targetType, isNullable, propertyAttributes));

            return null;
        }
    }

    [ContractClassFor(typeof(IDataTransformer<>))]
    abstract class IDataTransformerContract<T> : IDataTransformer<T>
    {
        public T Transform(T value, IEnumerable<Attribute> propertyAttributes)
        {
            Contract.Requires(propertyAttributes != null);

            return default(T);
        }

        bool IDataTransformer.CanTransform(object value, Type targetType, bool isNullable, IEnumerable<Attribute> propertyAttributes)
        {
            return false;
        }

        object IDataTransformer.Transform(object value, Type targetType, bool isNullable, IEnumerable<Attribute> propertyAttributes)
        {
            return null;
        }
    }

}
