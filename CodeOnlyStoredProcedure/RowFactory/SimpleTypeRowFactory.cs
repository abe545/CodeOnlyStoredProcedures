using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeOnlyStoredProcedure.RowFactory
{
    internal class SimpleTypeRowFactory : IRowFactory
    {
        private readonly bool isNullable;
        private readonly Type targetType;
        private readonly Type collectionType;

        public IEnumerable<string> UnfoundPropertyNames
        {
            get { return Enumerable.Empty<string>(); }
        }

        public SimpleTypeRowFactory(Type targetType)
        {
            Contract.Requires(targetType != null);

            this.collectionType = targetType;

            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                isNullable = true;
                targetType = targetType.GetGenericArguments().Single();
            }

            this.targetType = targetType;
        }

        public object CreateRow(string[] fieldNames,
                                object[] values,
                                IEnumerable<IDataTransformer> transformers)
        {
            var value = values[0];
            if (DBNull.Value.Equals(value))
                value = null;

            foreach (var xform in transformers)
            {
                if (xform.CanTransform(value, targetType, isNullable, Enumerable.Empty<Attribute>()))
                    value = xform.Transform(value, targetType, isNullable, Enumerable.Empty<Attribute>());
            }

            if (value is string && targetType.IsEnum)
                value = Enum.Parse(targetType, (string)value);

            return value;
        }
    }
}
