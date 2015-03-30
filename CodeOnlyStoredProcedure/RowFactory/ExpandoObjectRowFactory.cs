using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;

namespace CodeOnlyStoredProcedure.RowFactory
{
    internal class ExpandoObjectRowFactory<T> : RowFactory<T>
    {
        static MethodInfo          addToDictionaryMethod = typeof(IDictionary<string, object>).GetMethod("Add");
        static MethodInfo          getDataValuesMethod   = typeof(IDataRecord)                .GetMethod("GetValues");
        static ParameterExpression readerExpression      = Expression                         .Parameter(typeof(IDataReader));
        static ParameterExpression resultExpression      = Expression                         .Parameter(typeof(ExpandoObject));
        static ParameterExpression valuesExpression      = Expression                         .Parameter(typeof(object[]));

        protected override Func<IDataReader, T> CreateRowFactory(IDataReader reader, IEnumerable<IDataTransformer> xFormers)
        {
            var all = new List<Expression>();

            all.Add(Expression.Assign(resultExpression, Expression.New(typeof(ExpandoObject))));
            all.Add(Expression.Assign(valuesExpression, Expression.NewArrayBounds(typeof(object), Expression.Constant(reader.FieldCount))));
            all.Add(Expression.Call(readerExpression, getDataValuesMethod, valuesExpression));

            for (int i = 0; i < reader.FieldCount; i++)
            {
                all.Add(Expression.Call(resultExpression,
                                        addToDictionaryMethod,
                                        Expression.Constant(reader.GetName(i)),
                                        Expression.ArrayIndex(valuesExpression, Expression.Constant(i))));
            }

            all.Add(Expression.Convert(resultExpression, typeof(T)));

            return Expression.Lambda<Func<IDataReader, T>>(Expression.Block(typeof(T),
                                                                            new[] { resultExpression, valuesExpression }, 
                                                                            all.ToArray()), 
                                                                            readerExpression)
                                .Compile();
        }

        public override bool MatchesColumns(IEnumerable<string> columnNames, out int leftoverColumns)
        {
            leftoverColumns = 0;
            return true;
        }
    }
}
