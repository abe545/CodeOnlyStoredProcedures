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
    internal class EnumAccessorFactory<T> : AccessorFactoryBase
    {
        static readonly Type                                      dbType;
        static readonly bool                                      isNullable;
        static readonly Type                                      type                  = typeof(T);
        static readonly Lazy<Expression>                          parseStringExpression = new Lazy<Expression>(CreateParseStringExpression);
        static readonly ParameterExpression                       boxedValueExpression  = Expression.Variable (typeof(object));
        static readonly ParameterExpression                       stringValueExpression = Expression.Variable (typeof(string));
               readonly IEnumerable<DataTransformerAttributeBase> transformers          = Enumerable.Empty<DataTransformerAttributeBase>();
               readonly Lazy<UnaryExpression>                     throwNullException;
               readonly Lazy<Expression>                          getStringExpression;   
               readonly ParameterExpression                       dataReaderExpression;
               readonly Expression                                indexExpression;
               readonly Expression                                boxedExpression;
               readonly Expression                                unboxedExpression;
               readonly Expression                                attributeExpression;
               readonly string                                    errorMessage;
               readonly string                                    propertyName;
               readonly bool                                      convertNumeric = GlobalSettings.Instance.ConvertAllNumericValues;

        static EnumAccessorFactory()
        {
            dbType     = type;
            isNullable = GetUnderlyingNullableType(ref dbType);

            if (!dbType.IsEnum)
                throw new NotSupportedException("Can not use an EnumRowFactory on a type that is not an Enum.");
        }

        public EnumAccessorFactory(ParameterExpression dataReaderExpression, Expression index, PropertyInfo propertyInfo, string columnName)
        {
            Contract.Requires(dataReaderExpression != null);
            Contract.Requires(index                != null);

            this.indexExpression      = index;
            this.dataReaderExpression = dataReaderExpression;
            this.getStringExpression  = new Lazy<Expression>(() => Expression.Call(this.dataReaderExpression, typeof(IDataRecord).GetMethod("GetString"), this.indexExpression));
            
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

            unboxedExpression  = CreateUnboxedRetrieval<T>(dataReaderExpression, index, transformers, errorMessage);
            throwNullException = new Lazy<UnaryExpression>(() => Expression.Throw(Expression.New(
                                                                     typeof(NoNullAllowedException).GetConstructor(new[] { typeof(string) }),
                                                                     Expression.Constant(errorMessage))));
        }

        public override Expression CreateExpressionToGetValueFromReader(IDataReader reader, IEnumerable<IDataTransformer> xFormers, Type dbColumnType)
        {
            Expression body         = null;
            var        expectedType = dbType;
            GetUnderlyingEnumType(ref expectedType);
            var        underlying   = expectedType;
            StripSignForDatabase(ref expectedType);

            if (dbColumnType == typeof(string))
            {
                var exprs = new List<Expression>();
                var vars  = new List<ParameterExpression> { stringValueExpression };
                
                Expression nullValueExpression;
                if (isNullable || (xFormers.Any() && !xFormers.All(IsTypedTransformer)))
                    nullValueExpression = Expression.Assign(stringValueExpression, Expression.Constant(null, typeof(string)));
                else
                    nullValueExpression = throwNullException.Value;

                exprs.Add(
                    Expression.IfThenElse(
                        Expression.Call(dataReaderExpression, IsDbNullMethod, indexExpression),
                        nullValueExpression,
                        Expression.Assign(stringValueExpression, getStringExpression.Value)
                    )
                );

                if (xFormers.Any())
                {
                    if (xFormers.All(IsTypedTransformer))
                    {
                        Expression expr = stringValueExpression;
                        AddTypedTransformers<string>(xFormers, attributeExpression, ref expr);

                        if (expr != stringValueExpression)
                            exprs.Add(Expression.Assign(stringValueExpression, expr));
                    }
                    else
                    {
                        vars.Add(boxedValueExpression);
                        exprs.Add(Expression.Assign(boxedValueExpression, stringValueExpression));
                        AddTransformers(type, boxedValueExpression, attributeExpression, xFormers, exprs);
                        exprs.Add(Expression.Assign(stringValueExpression, Expression.Convert(boxedValueExpression, typeof(string))));
                    }

                    if (!isNullable)
                    {
                        exprs.Add(
                            Expression.IfThen(
                                Expression.ReferenceEqual(stringValueExpression, Expression.Constant(null, typeof(string))),
                                throwNullException.Value
                            )
                        );
                    }
                }

                if (isNullable)
                {
                    exprs.Add(
                        Expression.Condition(
                            Expression.ReferenceEqual(stringValueExpression, Expression.Constant(null, typeof(string))),
                            Expression.Constant(null, type),
                            parseStringExpression.Value
                        )
                    );
                }
                else
                    exprs.Add(parseStringExpression.Value);

                body = Expression.Block(type, vars, exprs);

                if (xFormers.All(IsTypedTransformer))
                    AddTypedTransformers<T>(xFormers, attributeExpression, ref body);
            }
            else if (unboxedExpression != null && xFormers.All(IsTypedTransformer))
            {
                body = CreateTypedRetrieval<T>(dataReaderExpression,
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

                Expression res;
                if (xFormers.Any())
                {
                    AddTransformers(type, boxedValueExpression, attributeExpression, xFormers, exprs);
                    res = CreateUnboxingExpression(dbType, isNullable, boxedValueExpression, exprs, errorMessage);
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
                body = CreateTypedRetrieval<T>(dataReaderExpression,
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

        static Expression CreateParseStringExpression()
        {
            var underlying = dbType;
            GetUnderlyingEnumType(ref underlying);

            var names    = Enum.GetNames(dbType);
            var values   = Enum.GetValues(dbType);
            var cases    = new List<SwitchCase>();
            var defCases = new List<SwitchCase>();
            var res      = Expression.Variable(underlying);
            var strings  = Expression.Variable(typeof(string[]));
            var idx      = Expression.Variable(typeof(int));

            for (int i = 0; i < names.Length; ++i)
            {
                var n   = Expression.Constant(names[i]);
                var obj = values.GetValue(i);
                var val = Expression.Constant(Convert.ChangeType(obj, underlying));

                cases   .Add(Expression.SwitchCase(Expression.Constant(obj), n));
                defCases.Add(Expression.SwitchCase(Expression.OrAssign(res, val), n));
            }

            var str     = Expression.Variable(typeof(string), "str");
            var endLoop = Expression.Label("endLoop");
            var excep   = Expression.Throw(
                              Expression.New(typeof(NotSupportedException).GetConstructor(new[] { typeof(string) }),
                                  Expression.Call(typeof(string).GetMethod("Format", new[] { typeof(string), typeof(object) }),
                                      Expression.Constant("Could not parse the string \"{0}\" into an enum of type " + dbType + "."),
                                      str
                                  )
                              ), underlying);
            var defExpr = Expression.Block(
                new[] { res, idx, strings, str },
                Expression.Assign(res, Expression.Constant(Convert.ChangeType(0, underlying))),
                Expression.Assign(strings, Expression.Call(stringValueExpression, typeof(string).GetMethod("Split", new[] { typeof(char[]) }), Expression.Constant(new char[] { ',' }))),
                Expression.Assign(idx, Expression.Constant(0)),
                Expression.Loop(
                    Expression.Block(
                        Expression.Assign(str, Expression.Call(Expression.ArrayIndex(strings, idx), typeof(string).GetMethod("Trim", new Type[0]))),
                        Expression.Switch(
                            str,
                            excep,
                            defCases.ToArray()
                        ),
                        Expression.PreIncrementAssign(idx),
                        Expression.IfThen(Expression.GreaterThanOrEqual(idx, Expression.ArrayLength(strings)), Expression.Break(endLoop))
                    )
                ),
                Expression.Label(endLoop),
                Expression.Convert(res, dbType)
            );

            Expression expr = Expression.Switch(
                    stringValueExpression,
                    defExpr,
                    cases.ToArray()
                );

            if (isNullable)
                expr = Expression.Convert(expr, type);

            return expr;
        }
    }
}
