using System;

namespace CodeOnlyStoredProcedure
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public abstract class DataTransformerAttributeBase : Attribute
    {
        public int Order { get; private set; }

        public DataTransformerAttributeBase(int order = 0)
        {
            Order = order;
        }

        public abstract object Transform(object value, Type targetType);
    }
}
