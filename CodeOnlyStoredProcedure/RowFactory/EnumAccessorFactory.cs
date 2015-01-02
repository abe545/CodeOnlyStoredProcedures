using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using CodeOnlyStoredProcedure.DataTransformation;

namespace CodeOnlyStoredProcedure.RowFactory
{
    internal class EnumAccessorFactory<T> : AccessorFactoryBase
    {
        private static readonly Lazy<MethodInfo>    getStringMethod       = new Lazy<MethodInfo>(() => typeof(IDataRecord).GetMethod("GetString"));
        private        readonly ParameterExpression boxedValueExpression  = Expression.Variable (typeof(object));
        private        readonly ParameterExpression stringValueExpression = Expression.Variable (typeof(string));
        private        readonly ParameterExpression dataReaderExpression;
        private        readonly Expression          indexExpression;
        private        readonly Expression          boxedExpression;
        private        readonly Expression          unboxedExpression;
        private        readonly Expression          parseStringExpression;
        private        readonly Expression          attributeExpression;
        private        readonly string              errorMessage;
        private        readonly string              propertyName;
        private        readonly bool                convertNumeric;
        private        readonly IEnumerable<DataTransformerAttributeBase> transformers = Enumerable.Empty<DataTransformerAttributeBase>();

        public EnumAccessorFactory(ParameterExpression dataReaderExpression, Expression index, PropertyInfo propertyInfo, string columnName)
        {
            this.indexExpression      = index;
            this.dataReaderExpression = dataReaderExpression;

            var type       = typeof(T);
            var dbType     = type;
            var isNullable = GetUnderlyingNullableType(ref dbType);

            if (!dbType.IsEnum)
                throw new NotSupportedException("Can not use an EnumRowFactory on a type that is not an Enum.");

            boxedExpression = CreateBoxedRetrieval(dataReaderExpression,
                                                   index,
                                                   boxedValueExpression,
                                                   propertyInfo,
                                                   columnName,
                                                   out errorMessage);

            if (propertyInfo == null)
            {
                propertyName = "result";
                errorMessage = "Null value is not allowed for single column result set that returns " +
                                typeof(T) + ", but null was the result from the stored procedure.";
                attributeExpression = Expression.Constant(null, typeof(Attribute[]));
            }
            else
            {
                propertyName = propertyInfo.Name;
                var attrs = propertyInfo.GetCustomAttributes(false).Cast<Attribute>().ToArray();
                transformers = attrs.OfType<DataTransformerAttributeBase>()
                                    .OrderBy(x => x.Order)
                                    .ToArray();
                attributeExpression = Expression.Constant(attrs);
                convertNumeric = attrs.OfType<ConvertNumericAttribute>().Any();
            }

            unboxedExpression = CreateUnboxedRetrieval<T>(dataReaderExpression, index, Enumerable.Empty<DataTransformerAttributeBase>());

            var underlying = dbType;
            GetUnderlyingEnumType(ref underlying);
            // TODO: Generate a switch statement to actually do the parsing, so that no boxing has to happen
            parseStringExpression = Expression.Convert(
                Expression.Convert(
                    Expression.Call(
                        typeof(Enum).GetMethod("Parse", new[] { typeof(Type), typeof(string) }),
                        Expression.Constant(dbType),
                        stringValueExpression
                    ),
                    underlying
                ),
                dbType
            );

            if (isNullable)
                parseStringExpression = Expression.Convert(parseStringExpression, type);
        }

        public override Expression CreateExpressionToGetValueFromReader(IDataReader reader, IEnumerable<IDataTransformer> xFormers, Type dbColumnType)
        {
            var        type         = typeof(T);
            Expression body         = null;
            var        unnulledType = type;
            var        isNullable   = GetUnderlyingNullableType(ref unnulledType);
            var        expectedType = unnulledType;
            GetUnderlyingEnumType(ref expectedType);
            var        underlying   = expectedType;
            StripSignForDatabase(ref expectedType);

            if (dbColumnType == typeof(string))
            {
                var exprs = new List<Expression>();

                var getString = Expression.Call(
                    dataReaderExpression,
                    getStringMethod.Value,
                    indexExpression
                );

                Expression nullValueExpression;
                if (isNullable || xFormers.Any())
                    nullValueExpression = Expression.Assign(stringValueExpression, Expression.Constant(null, typeof(string)));
                else
                {
                    nullValueExpression = Expression.Throw(
                        Expression.New(
                            typeof(NoNullAllowedException).GetConstructor(new[] { typeof(string) }),
                            Expression.Constant(errorMessage)
                        )
                    );
                }

                exprs.Add(
                    Expression.IfThenElse(
                        Expression.Call(dataReaderExpression, IsDbNullMethod, indexExpression),
                        nullValueExpression,
                        Expression.Assign(stringValueExpression, getString)
                    )
                );

                if (xFormers.Any())
                {
                    exprs.Add(Expression.Assign(boxedValueExpression, stringValueExpression));
                    AddTransformers(type, boxedValueExpression, Expression.Constant(null, typeof(Attribute[])), xFormers, exprs);
                    exprs.Add(Expression.Assign(stringValueExpression, Expression.Convert(boxedValueExpression, typeof(string))));
                }

                if (isNullable)
                {
                    exprs.Add(
                        Expression.Condition(
                            Expression.ReferenceEqual(stringValueExpression, Expression.Constant(null, typeof(string))),
                            Expression.Constant(null, type),
                            parseStringExpression
                        )
                    );
                }
                else
                {
                    exprs.Add(
                        Expression.IfThen(
                            Expression.ReferenceEqual(stringValueExpression, Expression.Constant(null, typeof(string))),
                            Expression.Throw(
                                Expression.New(
                                    typeof(NoNullAllowedException).GetConstructor(new[] { typeof(string) }),
                                    Expression.Constant(errorMessage)
                                )
                            )
                        )
                    );
                    exprs.Add(parseStringExpression);
                }

                body = Expression.Block(type, new[] { boxedValueExpression, stringValueExpression }, exprs);
            }
            else if (xFormers.Any() || unboxedExpression == null)
            {
                var exprs = new List<Expression>();

                exprs.Add(boxedExpression);

                Expression res;
                if (xFormers.Any())
                {
                    AddTransformers(type, boxedValueExpression, attributeExpression, xFormers, exprs);
                    res = CreateUnboxingExpression(unnulledType, isNullable, boxedValueExpression, exprs, errorMessage);
                }
                else
                {
                    res = CreateUnboxingExpression(dbColumnType, isNullable, boxedValueExpression, exprs, errorMessage);

                    if (dbColumnType != expectedType)
                    {
                        if (convertNumeric)
                            res = Expression.Convert(res, expectedType);
                        else
                            throw new StoredProcedureColumnException(expectedType, dbColumnType, propertyName);
                    }

                    if (expectedType != underlying)
                        res = Expression.Convert(res, underlying);
                }

                exprs.Add(Expression.Convert(res, type));

                body = Expression.Block(type, new[] { boxedValueExpression }, exprs);
            }
            else
            {
                if (dbColumnType != expectedType)
                {
                    if (convertNumeric)
                        body = CreateUnboxedRetrieval<T>(dataReaderExpression, indexExpression, transformers, dbColumnType);
                    else
                        throw new StoredProcedureColumnException(type, dbColumnType, propertyName);
                }
                else
                    body = unboxedExpression;
            }

            return body;
        }
    }
}
