using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;

namespace CodeOnlyStoredProcedure.RowFactory
{
    internal class EnumRowFactory<T> : RowFactory<T>
    {
        static readonly EnumAccessorFactory<T> accessor = new EnumAccessorFactory<T>(dataReaderExpression, Expression.Constant(0), null, null);

        protected override Func<IDataReader, T> CreateRowFactory(IDataReader reader, IEnumerable<IDataTransformer> xFormers)
        {
            return Expression.Lambda<Func<IDataReader, T>>(accessor.CreateExpressionToGetValueFromReader(reader, xFormers, reader.GetFieldType(0)),
                                                           dataReaderExpression)
                             .Compile();
        }
    }
}
