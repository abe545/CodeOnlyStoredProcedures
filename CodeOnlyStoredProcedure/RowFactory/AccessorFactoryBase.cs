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
    [ContractClass(typeof(AccessorFactoryBaseContract))]
    internal abstract class AccessorFactoryBase
    {
        private static Lazy<MethodInfo> isDbNull      = new Lazy<MethodInfo>(() => typeof(IDataRecord)                 .GetMethod("IsDBNull"));
        private static Lazy<MethodInfo> getValue      = new Lazy<MethodInfo>(() => typeof(IDataRecord)                 .GetMethod("GetValue"));
        private static Lazy<MethodInfo> canTransform  = new Lazy<MethodInfo>(() => typeof(IDataTransformer)            .GetMethod("CanTransform"));
        private static Lazy<MethodInfo> transform     = new Lazy<MethodInfo>(() => typeof(IDataTransformer)            .GetMethod("Transform"));
        private static Lazy<MethodInfo> attrTransform = new Lazy<MethodInfo>(() => typeof(DataTransformerAttributeBase).GetMethod("Transform"));

        protected static MethodInfo IsDbNullMethod { get { return isDbNull    .Value; } }
        protected static MethodInfo GetValueMethod { get { return getValue    .Value; } }
        protected static MethodInfo CanTransform   { get { return canTransform.Value; } }
        protected static MethodInfo Transform      { get { return transform   .Value; } }

        public abstract Expression CreateExpressionToGetValueFromReader(IDataReader reader, IEnumerable<IDataTransformer> xFormers, Type dbColumnType);

        protected static bool IsNullable(Type type)
        {
            Contract.Requires(type != null);

            return type.IsClass || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));
        }

        protected static bool GetUnderlyingNullableType(ref Type type)
        {
            Contract.Requires(type != null);
            Contract.Ensures(Contract.ValueAtReturn(out type) != null);

            if (type.IsClass)
                return true;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = type.GetGenericArguments().Single();
                return true;
            }

            return false;
        }

        protected static bool GetUnderlyingEnumType(ref Type type)
        {
            Contract.Requires(type != null);
            Contract.Ensures(Contract.ValueAtReturn(out type) != null);

            if (type.IsEnum)
            {
                type = type.GetEnumUnderlyingType();
                return true;
            }

            return false;
        }

        protected static bool StripSignForDatabase(ref Type type)
        {
            Contract.Requires(type != null);
            Contract.Ensures(Contract.ValueAtReturn(out type) != null);

            if (type == typeof(UInt32))
            {
                type = typeof(Int32);
                return true;
            }

            if (type == typeof(UInt16))
            {
                type = typeof(Int16);
                return true;
            }

            if (type == typeof(UInt64))
            {
                type = typeof(Int64);
                return true;
            }

            if (type == typeof(SByte))
            {
                type = typeof(Byte);
                return true;
            }

            return false;
        }
        
        private static Expression AddTransformers(ParameterExpression val, DataTransformerAttributeBase[] xfAttrs, Type targetType, bool isNullable)
        {
            Contract.Requires(val        != null);
            Contract.Requires(xfAttrs    != null && xfAttrs.Length > 0);
            Contract.Requires(targetType != null);

            var exprs = new List<Expression>();
            var arg1  = Expression.Constant(targetType);
            var arg2  = Expression.Constant(isNullable);

            for (int i = 0; i < xfAttrs.Length; i++)
            {
                // val = xfAttrs[i].Transform(val, targetType, isNullable);
                exprs.Add(Expression.Assign(val, Expression.Call(Expression.Constant(xfAttrs[i]), attrTransform.Value, val, arg1, arg2)));
            }

            if (exprs.Count == 1)
                return exprs[0];

            return Expression.Block(exprs);
        }

        protected static Expression CreateBoxedRetrieval(
            ParameterExpression dbReader,
            Expression          index,
            ParameterExpression value,
            PropertyInfo        propertyInfo,
            string              columnName,
            out string          exBody)
        {
            Contract.Requires(dbReader != null);
            Contract.Requires(index    != null);
            Contract.Requires(value    != null);

            var exps = new List<Expression>();

            // value = dbReader.IsDBNull(index) ? null : dbReader.GetValue(index);
            exps.Add(
                Expression.Assign(
                    value,
                    Expression.Condition(
                        Expression.Call    (dbReader, IsDbNullMethod, index),
                        Expression.Constant(null,     typeof(object)),
                        Expression.Call    (dbReader, GetValueMethod, index)
                    )
                )
            );

            if (propertyInfo != null)
            {
                exBody = propertyInfo.Name + " is not nullable, but null was returned from the database for column " + columnName + ".";

                var xformers = propertyInfo.GetCustomAttributes(false)
                                           .OfType<DataTransformerAttributeBase>()
                                           .OrderBy(x => x.Order)
                                           .ToArray();

                if (xformers.Length > 0)
                {
                    if (propertyInfo != null && columnName != null)
                        exBody = propertyInfo.Name + " is not nullable, but null was the value for column " + columnName + " after data transformation.";

                    var type = propertyInfo.PropertyType;
                    var isNullable = GetUnderlyingNullableType(ref type);
                    exps.Add(AddTransformers(value, xformers, type, isNullable));
                }
            }
            else
                exBody = null;

            return Expression.Block(exps);
        }

        protected static Expression CreateUnboxingExpression(Type type, bool isNullable, ParameterExpression value, List<Expression> exprs, string notNullableNullValueMessage)
        {
            Contract.Requires(type  != null);
            Contract.Requires(value != null);
            Contract.Requires(exprs != null);

            if (!isNullable)
            {
                // if (value == null)
                //     throw new NoNullAllowedException("Expected result of type " + typeof(T) + " for single column result set.");
                exprs.Add(
                    Expression.IfThen(
                        Expression.ReferenceEqual(value, Expression.Constant(null, typeof(object))),
                        Expression.Throw(
                            Expression.New(
                                typeof(NoNullAllowedException).GetConstructor(new[] { typeof(string) }),
                                Expression.Constant(notNullableNullValueMessage)
                            )
                        )
                    )
                );
            }

            // var t = (T)value;
            Expression res;
            if (type.IsClass)
                res = Expression.Convert(value, type);
            else
                res = Expression.Unbox(value, type);

            if (isNullable)
            {
                // return value == null ? default(T) : (T)value;
                return Expression.Condition(
                    Expression.ReferenceEqual(value, Expression.Constant(null, typeof(object))),
                    type.IsClass ? Expression.Constant(null, type) : Expression.Constant(null, typeof(Nullable<>).MakeGenericType(type)),
                    type.IsClass ? res : Expression.Convert(res, typeof(Nullable<>).MakeGenericType(type))
                );
            }
            
            return res;
        }

        protected static void AddTransformers(
            Type                          resultType,
            ParameterExpression           value,
            Expression                    attributes,
            IEnumerable<IDataTransformer> transformers,
            List<Expression>              expressions)
        {
            Contract.Requires(resultType   != null);
            Contract.Requires(value        != null);
            Contract.Requires(attributes   != null);
            Contract.Requires(transformers != null);
            Contract.Requires(expressions  != null);

            var isNullable = Expression.Constant(GetUnderlyingNullableType(ref resultType));
            var type       = Expression.Constant(resultType);

            foreach (var xf in transformers)
            {
                expressions.Add(Expression.IfThen(
                    Expression.IsTrue(Expression.Call(Expression.Constant(xf),
                                                      CanTransform,
                                                      value,
                                                      type,
                                                      isNullable,
                                                      attributes)),
                    Expression.Assign(value, Expression.Call(Expression.Constant(xf),
                                                             Transform,
                                                             value,
                                                             type,
                                                             isNullable,
                                                             attributes))
                ));
            }
        }

        protected static Expression CreateTypedRetrieval<T>(
            ParameterExpression                       dbReader,
            Expression                                index,
            Expression                                unboxedExpression,
            IEnumerable<DataTransformerAttributeBase> xFormers,
            string                                    propertyName,
            string                                    errorMessage,
            Type                                      dbType,
            Type                                      expectedDbType,
            bool                                      convertNumeric)
        {
            Contract.Requires(dbReader          != null);
            Contract.Requires(index             != null);
            Contract.Requires(unboxedExpression != null);
            Contract.Requires(xFormers          != null && Contract.ForAll(xFormers, x => x != null));
            Contract.Requires(!string.IsNullOrWhiteSpace(propertyName));
            Contract.Requires(!string.IsNullOrWhiteSpace(errorMessage));
            Contract.Requires(dbType            != null);
            Contract.Requires(expectedDbType    != null);

            if (dbType != expectedDbType)
            {
                if (convertNumeric)
                    return CreateUnboxedRetrieval<T>(dbReader, index, xFormers, errorMessage, dbType);
                else
                    throw new StoredProcedureColumnException(typeof(T), dbType, propertyName);
            }

            return unboxedExpression;
        }

        protected static Expression CreateUnboxedRetrieval<T>(
            ParameterExpression                       dbReader,
            Expression                                index,
            IEnumerable<DataTransformerAttributeBase> xFormers,
            string                                    errorMessage,
            Type                                      expectedDbType = null)
        {
            Contract.Requires(dbReader != null);
            Contract.Requires(index    != null);
            Contract.Requires(xFormers != null);

            if (!xFormers.All(xf => xf is IDataTransformerAttribute<T>))
                return null;

            var converted  = xFormers.OfType<IDataTransformerAttribute<T>>().ToArray();
            var type       = typeof(T);
            var retVal     = Expression.Variable(type);
            var body       = new List<Expression>();
            var parms      = new List<ParameterExpression> { retVal };
            var dbType     = type;
            var isNullable = GetUnderlyingNullableType(ref dbType);
            var nonNull    = dbType;
            var isEnum     = GetUnderlyingEnumType(ref dbType);
            var unswitched = dbType;
            var switchSign = StripSignForDatabase(ref dbType);

            Expression res;

            if (expectedDbType != null)
            {
                res = Expression.Call(dbReader, typeof(IDataRecord).GetMethod("Get" + Type.GetTypeCode(expectedDbType)), index);
                if (type == typeof(bool) || type == typeof(bool?))
                {
                    res = Expression.NotEqual(res, Zero(expectedDbType));
                    if (type == typeof(bool?))
                        res = Expression.Convert(res, type);
                }
                else
                    res = Expression.Convert(res, dbType);
            }
            else
                res = Expression.Call(dbReader, typeof(IDataRecord).GetMethod("Get" + Type.GetTypeCode(dbType)), index);

            if (switchSign)
                res = Expression.Convert(res, unswitched);
            if (isEnum)
                res = Expression.Convert(res, nonNull);

            if (!converted.Any())
            {
                if (isNullable)
                {
                    return Expression.Condition(
                            Expression.Call(dbReader, IsDbNullMethod, index),
                            Expression.Constant(default(T), type),
                            Expression.Convert(res, type)
                        );
                }

                return Expression.Block(type,
                    Expression.IfThen(
                        Expression.Call(dbReader, IsDbNullMethod, index),
                        Expression.Throw(
                            Expression.New(
                                typeof(NoNullAllowedException).GetConstructor(new[] { typeof(string) }),
                                Expression.Constant(errorMessage)
                            )
                        )
                    ),
                    res
                );
            }
            else if (isNullable)
            {
                res = Expression.Condition(
                            Expression.Call(dbReader, IsDbNullMethod, index),
                            Expression.Constant(default(T), type),
                            Expression.Convert(res, type)
                        );
                body.Add(Expression.Assign(retVal, res));
            }
            else
            {
                body.Add(
                    Expression.IfThenElse(
                        Expression.Call(dbReader, IsDbNullMethod, index),
                        Expression.Throw(
                            Expression.New(
                                typeof(NoNullAllowedException).GetConstructor(new[] { typeof(string) }),
                                Expression.Constant(errorMessage)
                            )
                        ),
                        Expression.Assign(retVal, res)
                    )
                );
            }

            var method = DataTransformerCache<T>.attrTransform;
            foreach (var xf in converted)
                body.Add(Expression.Assign(retVal, Expression.Call(Expression.Constant(xf), method, retVal)));

            body.Add(retVal);

            return Expression.Block(type, parms, body);
        }

        protected static Expression Zero(Type dbType, bool isNullable = false)
        {
            if (dbType == typeof(short))
                return Expression.Constant((short)0, isNullable ? typeof(short?) : dbType);
            if (dbType == typeof(byte))
                return Expression.Constant((byte)0, isNullable ? typeof(byte?) : dbType);
            if (dbType == typeof(double))
                return Expression.Constant(0.0, isNullable ? typeof(double?) : dbType);
            if (dbType == typeof(float))
                return Expression.Constant(0f, isNullable ? typeof(float?) : dbType);
            if (dbType == typeof(decimal))
                return Expression.Constant(0M, isNullable ? typeof(decimal?) : dbType);
            if (dbType == typeof(long))
                return Expression.Constant(0L, isNullable ? typeof(long?) : dbType);

            return Expression.Constant(0, isNullable ? typeof(int?) : dbType);
        }

        protected static bool IsTypedTransformer(IDataTransformer transformer)
        {
            Contract.Requires(transformer != null);

            return transformer.GetType()
                              .GetInterfaces()
                              .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDataTransformer<>));
        }

        protected static void AddTypedTransformers<T>(IEnumerable<IDataTransformer> xFormers, Expression attributeExpression, ref Expression expr)
        {
            Contract.Requires(xFormers                         != null);
            Contract.Requires(attributeExpression              != null);
            Contract.Requires(expr                             != null);
            Contract.Ensures (Contract.ValueAtReturn(out expr) != null);

            foreach (var x in xFormers.OfType<IDataTransformer<T>>())
                expr = Expression.Call(Expression.Constant(x), DataTransformerCache<T>.transform, expr, attributeExpression);
        }

        private static class DataTransformerCache<T>
        {
            public static readonly MethodInfo transform     = typeof(IDataTransformer<T>)         .GetMethod("Transform");
            public static readonly MethodInfo attrTransform = typeof(IDataTransformerAttribute<T>).GetMethod("Transform");
        }
    }

    [ContractClassFor(typeof(AccessorFactoryBase))]
    abstract class AccessorFactoryBaseContract : AccessorFactoryBase
    {
        public override Expression CreateExpressionToGetValueFromReader(IDataReader reader, IEnumerable<IDataTransformer> xFormers, Type dbColumnType)
        {
            Contract.Requires(reader                        != null);
            Contract.Requires(xFormers                      != null && Contract.ForAll(xFormers, x => x != null));
            Contract.Requires(dbColumnType                  != null);
            Contract.Ensures (Contract.Result<Expression>() != null);
            return null;
        }
    }
}
