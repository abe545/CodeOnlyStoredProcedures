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
               readonly Attribute[]                               attributes;
               readonly string                                    errorMessage;
               readonly string                                    propertyName;
               readonly bool                                      convertNumeric = GlobalSettings.Instance.ConvertAllNumericValues;
               readonly PropertyInfo                              propertyInfo;
               readonly string                                    columnName;
                        bool                                      isFirstExecution = true;
                        Expression                                unboxedExpression;

        public ValueAccessorFactory(ParameterExpression dataReaderExpression, Expression index, PropertyInfo propertyInfo, string columnName)
        {
            Contract.Requires(dataReaderExpression != null);
            Contract.Requires(index                != null);

            this.dataReaderExpression = dataReaderExpression;
            this.indexExpression      = index;
            this.propertyInfo         = propertyInfo;
            this.columnName           = columnName;
            
            boxedExpression = CreateBoxedRetrieval(dataReaderExpression,
                                                   index,
                                                   boxedValueExpression,
                                                   propertyInfo,
                                                   columnName,
                                                   out errorMessage);

            if (propertyInfo == null)
            {
                attributes   = new Attribute[0];
                propertyName = "result";
                errorMessage = "Null value is not allowed for single column result set that returns " +
                                typeof(T) + ", but null was the result from the stored procedure.";
            }
            else
            {
                propertyName = propertyInfo .Name;
                attributes   = propertyInfo .GetCustomAttributes(false).Cast<Attribute>().ToArray();
                transformers = attributes   .OfType<DataTransformerAttributeBase>()
                                            .OrderBy(x => x.Order)
                                            .ToArray();
                convertNumeric |= attributes.OfType<ConvertNumericAttribute>().Any();
            }
        }

        public override Expression CreateExpressionToGetValueFromReader(
            IDataReader                   reader,
            IEnumerable<IDataTransformer> xFormers, 
            Type                          dbColumnType,
            CodeSteppingInfo              codeSteppingInfo = null)
        {
            if (isFirstExecution)
            {
                isFirstExecution  = false;
                unboxedExpression = CreateUnboxedRetrieval<T>(reader, dataReaderExpression, indexExpression, transformers, errorMessage);
            }

            var        hasStepper   = codeSteppingInfo != null;
            var        type         = typeof(T);
            Expression body         = null;
            var        expectedType = type;
            var        isNullable   = expectedType.GetUnderlyingNullableType(out expectedType);
            StripSignForDatabase(ref expectedType);

            if (hasStepper)
                codeSteppingInfo.StartParseMethod(dataReaderExpression.Name);

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
                                                convertNumeric,
                                                codeSteppingInfo);
                if (hasStepper)
                {
                    body = codeSteppingInfo.AddTransformers(
                        xFormers.OfType<IDataTransformer<T>>().ToArray(), 
                        attributes, 
                        body, 
                        "");
                }
                else
                    AddTypedTransformers<T>(xFormers, attributes, ref body);

            }
            else if (unboxedExpression == null || xFormers.Any())
            {
                var exprs = new List<Expression>();

                if (hasStepper)
                {
                    string err;
                    exprs.Add(CreateBoxedRetrieval(dataReaderExpression,
                                                   indexExpression,
                                                   boxedValueExpression,
                                                   propertyInfo,
                                                   columnName,
                                                   out err,
                                                   codeSteppingInfo));
                }
                else
                    exprs.Add(boxedExpression);

                if (xFormers.Any())
                    AddTransformers(type, boxedValueExpression, attributes, xFormers, exprs, codeSteppingInfo);

                exprs.AddRange(CreateUnboxingExpression<T>(dbColumnType,
                                                           expectedType,
                                                           typeof(T),
                                                           isNullable, 
                                                           boxedValueExpression,
                                                           errorMessage,
                                                           propertyName,
                                                           convertNumeric,
                                                           xFormers.Any(),
                                                           codeSteppingInfo));

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
