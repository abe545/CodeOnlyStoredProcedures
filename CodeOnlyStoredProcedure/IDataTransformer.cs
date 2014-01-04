using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace CodeOnlyStoredProcedure
{
    [ContractClass(typeof(IDataTransformerContract))]
    public interface IDataTransformer
    {
        bool   CanTransform(object value, Type targetType, IEnumerable<Attribute> propertyAttributes);
        object Transform   (object value, Type targetType, IEnumerable<Attribute> propertyAttributes);
    }

    [ContractClassFor(typeof(IDataTransformer))]
    abstract class IDataTransformerContract : IDataTransformer
    {
        public bool CanTransform(object value, Type targetType, IEnumerable<Attribute> propertyAttributes)
        {
            Contract.Requires(targetType != null);
            Contract.Requires(propertyAttributes != null);

            return false;
        }

        public object Transform(object value, Type targetType, IEnumerable<Attribute> propertyAttributes)
        {
            Contract.Requires(targetType != null);
            Contract.Requires(propertyAttributes != null);

            return null;
        }
    }
}
