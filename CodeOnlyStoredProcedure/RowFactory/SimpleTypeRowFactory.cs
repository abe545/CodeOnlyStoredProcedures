using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;

namespace CodeOnlyStoredProcedure.RowFactory
{
    internal class SimpleTypeRowFactory<T> : RowFactory<T>
    {
        static readonly ValueAccessorFactory<T> accessor = new ValueAccessorFactory<T>(dataReaderExpression, Expression.Constant(0), null, null);

        protected override Func<IDataReader, T> CreateRowFactory(IDataReader reader, IEnumerable<IDataTransformer> xFormers)
        {
            return Expression.Lambda<Func<IDataReader, T>>(accessor.CreateExpressionToGetValueFromReader(reader, xFormers, reader.GetFieldType(0)),
                                                           dataReaderExpression)
                             .Compile();
        }
    }
}
