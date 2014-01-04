using System;
using System.Collections.Generic;

namespace CodeOnlyStoredProcedure
{
    public interface IDataTransformer
    {
        bool   CanTransform(object value, Type targetType, IEnumerable<Attribute> propertyAttributes);
        object Transform   (object value, Type targetType, IEnumerable<Attribute> propertyAttributes);
    }
}
