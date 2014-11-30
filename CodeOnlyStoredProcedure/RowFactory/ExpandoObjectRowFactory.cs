using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace CodeOnlyStoredProcedure.RowFactory
{
    internal class ExpandoObjectRowFactory : IRowFactory
    {
        public IEnumerable<string> UnfoundPropertyNames
        {
            get { return Enumerable.Empty<string>(); }
        }

        public object CreateRow(string[] fieldNames, object[] values, IEnumerable<IDataTransformer> transformers)
        {
            IDictionary<string, object> ret = new ExpandoObject();

            for (int i = 0; i < fieldNames.Length; i++)
            {
                var val = values[i];
                if (val == DBNull.Value)
                    val = null;

                var valType = val != null ? val.GetType() : typeof(object);

                foreach (var x in transformers)
                {
                    if (x.CanTransform(val, valType, true, Enumerable.Empty<Attribute>()))
                        val = x.Transform(val, valType, true, Enumerable.Empty<Attribute>());
                }

                ret.Add(fieldNames[i], val);
            }

            return ret;
        }
    }
}
