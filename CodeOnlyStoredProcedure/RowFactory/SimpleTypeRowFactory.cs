﻿using System;
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
            var a = accessor;
            if (GlobalSettings.Instance.IsTestInstance)
                a = new ValueAccessorFactory<T>(dataReaderExpression, Expression.Constant(0), null, null);

            return Expression.Lambda<Func<IDataReader, T>>(a.CreateExpressionToGetValueFromReader(reader, xFormers, reader.GetFieldType(0)),
                                                           dataReaderExpression)
                             .Compile();
        }
    }
}
