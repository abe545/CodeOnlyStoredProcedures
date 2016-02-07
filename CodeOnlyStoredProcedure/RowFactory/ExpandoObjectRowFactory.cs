using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CodeOnlyStoredProcedure.RowFactory
{
    internal class ExpandoObjectRowFactory<T> : RowFactory<T>
    {
        static MethodInfo          addToDictionaryMethod = typeof(IDictionary<string, object>).GetMethod("Add");
        static MethodInfo          isDbNull              = typeof(IDataRecord)                .GetMethod("IsDBNull");
        static MethodInfo          getDataValuesMethod   = typeof(IDataRecord)                .GetMethod("GetValues");
        static ParameterExpression readerExpression      = Expression                         .Parameter(typeof(IDataReader));
        static ParameterExpression resultExpression      = Expression                         .Parameter(typeof(ExpandoObject));
        static ParameterExpression valuesExpression      = Expression                         .Parameter(typeof(object[]));
        static Lazy<MethodInfo>    canTransformMethod    = new Lazy<MethodInfo>(() => typeof(IDataTransformer).GetMethod("CanTransform"));
        static Lazy<MethodInfo>    transformMethod       = new Lazy<MethodInfo>(() => typeof(IDataTransformer).GetMethod("Transform"));

        protected override Func<IDataReader, T> CreateRowFactory(IDataReader reader, IEnumerable<IDataTransformer> xFormers)
        {
            var all = new List<Expression>();

            all.Add(Expression.Assign(resultExpression, Expression.New(typeof(ExpandoObject))));
            all.Add(Expression.Assign(valuesExpression, Expression.NewArrayBounds(typeof(object), Expression.Constant(reader.FieldCount))));
            all.Add(Expression.Call(readerExpression, getDataValuesMethod, valuesExpression));

            for (int i = 0; i < reader.FieldCount; i++)
            {
                Expression getValue = Expression.Condition( 
                    Expression.Call(readerExpression, isDbNull, Expression.Constant(i)),
                    Expression.Constant(null, typeof(object)),
                    Expression.ArrayIndex(valuesExpression, Expression.Constant(i)));

                if (xFormers.Any())
                {
                    var valType  = Expression.Constant(reader.GetFieldType(i));
                    var curValue = Expression.Variable(typeof(object), "value");
                    var exprs    = new List<Expression>();
                    var trueExpr = Expression.Constant(true);
                    var attrExpr = Expression.Constant(new Attribute[0]);

                    exprs.Add(Expression.Assign(curValue, getValue));

                    foreach (var x in xFormers)
                    {
                        var xExpr = Expression.Constant(x);
                        exprs.Add(Expression.IfThen(Expression.Call(xExpr, canTransformMethod.Value, curValue, valType, trueExpr, attrExpr),
                            Expression.Assign(curValue, Expression.Call(xExpr, transformMethod.Value, curValue, valType, trueExpr, attrExpr))));
                    }

                    exprs.Add(curValue);
                    getValue = Expression.Block(new[] { curValue }, exprs.ToArray());
                }

                all.Add(Expression.Call(resultExpression,
                                        addToDictionaryMethod,
                                        Expression.Constant(reader.GetName(i)),
                                        getValue));
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
