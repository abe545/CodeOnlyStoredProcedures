using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using CodeOnlyStoredProcedure.DataTransformation;

namespace CodeOnlyStoredProcedure.RowFactory
{
    internal class ValueAccessorFactory<T> : AccessorFactoryBase
    {
        static readonly Lazy<MethodInfo>                          typedTransform       = new Lazy<MethodInfo>(() => typeof(IDataTransformer<T>).GetMethod("Transform"));
               readonly IEnumerable<DataTransformerAttributeBase> transformers         = Enumerable.Empty<DataTransformerAttributeBase>();
               readonly ParameterExpression                       boxedValueExpression = Expression.Variable(typeof(object), "value");
               readonly ParameterExpression                       dataReaderExpression;
               readonly Expression                                indexExpression;
               readonly Expression                                boxedExpression;
               readonly Expression                                attributeExpression;
               readonly string                                    errorMessage;
               readonly string                                    propertyName;
               readonly bool                                      convertNumeric = GlobalSettings.Instance.ConvertAllNumericValues;
                        bool                                      isFirstExecution = true;
                        Expression                                unboxedExpression;

        public ValueAccessorFactory(ParameterExpression dataReaderExpression, Expression index, PropertyInfo propertyInfo, string columnName)
        {
            Contract.Requires(dataReaderExpression != null);
            Contract.Requires(index                != null);

            this.dataReaderExpression = dataReaderExpression;
            this.indexExpression      = index;
            
            boxedExpression = CreateBoxedRetrieval(dataReaderExpression,
                                                   index,
                                                   boxedValueExpression,
                                                   propertyInfo,
                                                   columnName,
                                                   out errorMessage);

            if (propertyInfo == null)
            {
                propertyName = "result";
                errorMessage = $"Null value is not allowed for single column result set that returns {typeof(T)}, but null was the result from the stored procedure.";
                attributeExpression = Expression.Constant(new Attribute[0]);
            }
            else
            {
                propertyName = propertyInfo.Name;
                var attrs = propertyInfo.GetCustomAttributes(false).Cast<Attribute>().ToArray();
                transformers = attrs.OfType<DataTransformerAttributeBase>()
                                    .OrderBy(x => x.Order)
                                    .ToArray();
                attributeExpression = Expression.Constant(attrs);
                convertNumeric |= attrs.OfType<ConvertNumericAttribute>().Any();
            }
        }

        public override Expression CreateExpressionToGetValueFromReader(IDataReader reader, IEnumerable<IDataTransformer> xFormers, Type dbColumnType)
        {
            if (isFirstExecution)
            {
                isFirstExecution  = false;
                unboxedExpression = CreateUnboxedRetrieval<T>(reader, dataReaderExpression, indexExpression, transformers, errorMessage);
            }

            var        type         = typeof(T);
            Expression body         = null;
            var        expectedType = type;
            var        isNullable   = GetUnderlyingNullableType(ref expectedType);
            StripSignForDatabase(ref expectedType);

            if (unboxedExpression != null && xFormers.All(IsTypedTransformer))
            {
                body = CreateTypedRetrieval<T>(reader,
                                               dataReaderExpression,
                                               indexExpression,
                                               unboxedExpression,
                                               transformers,
                                               propertyName,
                                               errorMessage,
                                               dbColumnType,
                                               expectedType,
                                               convertNumeric);

                AddTypedTransformers<T>(xFormers, attributeExpression, ref body);
            }
            else if (unboxedExpression == null || xFormers.Any())
            {
                var exprs = new List<Expression>();

                exprs.Add(boxedExpression);

                if (xFormers.Any())
                    AddTransformers(type, boxedValueExpression, attributeExpression, xFormers, exprs);

                var res = CreateUnboxingExpression(dbColumnType, isNullable, boxedValueExpression, exprs, errorMessage);

                if (dbColumnType != expectedType)
                {
                    if (convertNumeric)
                    {
                        if (expectedType == typeof(bool))
                        {
                            if (isNullable)
                            {
                                // res = res == null ? null : (bool?)(bool)res
                                res = Expression.Condition(
                                    Expression.Equal(res, Expression.Constant(null)),
                                    Expression.Constant(default(bool?), typeof(bool?)),
                                    Expression.Convert(Expression.NotEqual(res, Zero(dbColumnType, true)), typeof(bool?)));
                            }
                            else
                                res = Expression.NotEqual(res, Zero(dbColumnType));
                        }
                        else if (expectedType == typeof(byte[]))
                        {
                            //BitConverter.GetBytes()
                            res = Expression.Condition(
                                Expression.Equal(res, Expression.Constant(null)),
                                Expression.Constant(default(byte[]), typeof(byte[])), 
                                Expression.Call(null, typeof(BitConverter).GetMethod(nameof(BitConverter.GetBytes), new[] { dbColumnType }), Expression.Property(res, "Value")));
                        }
                        else
                            res = Expression.Convert(res, type);
                    }
                    else
                        throw new StoredProcedureColumnException(type, dbColumnType, propertyName);
                }
                else if (isNullable)
                    res = Expression.Convert(res, type);

                exprs.Add(res);

                body = Expression.Block(type, new[] { boxedValueExpression }, exprs);
            }
            else
            {
                body = CreateTypedRetrieval<T>(reader,
                                               dataReaderExpression,
                                               indexExpression,
                                               unboxedExpression,
                                               transformers,
                                               propertyName,
                                               errorMessage,
                                               dbColumnType,
                                               expectedType,
                                               convertNumeric);
            }

            return body;
        }
    }
}
