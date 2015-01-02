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
    internal abstract class AccessorFactoryBase
    {
        private static Lazy<MethodInfo> isDbNull     = new Lazy<MethodInfo>(() => typeof(IDataRecord)     .GetMethod("IsDBNull"));
        private static Lazy<MethodInfo> getValue     = new Lazy<MethodInfo>(() => typeof(IDataRecord)     .GetMethod("GetValue"));
        private static Lazy<MethodInfo> canTransform = new Lazy<MethodInfo>(() => typeof(IDataTransformer).GetMethod("CanTransform"));
        private static Lazy<MethodInfo> transform    = new Lazy<MethodInfo>(() => typeof(IDataTransformer).GetMethod("Transform"));

        protected static MethodInfo IsDbNullMethod { get { return isDbNull    .Value; } }
        protected static MethodInfo GetValueMethod { get { return getValue    .Value; } }
        protected static MethodInfo CanTransform   { get { return canTransform.Value; } }
        protected static MethodInfo Transform      { get { return transform   .Value; } }

        public abstract Expression CreateExpressionToGetValueFromReader(IDataReader reader, IEnumerable<IDataTransformer> xFormers, Type dbColumnType);

        protected static bool IsNullable(Type type)
        {
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

            var meth  = typeof(DataTransformerAttributeBase).GetMethod("Transform");
            var exprs = new List<Expression>();
            var arg1  = Expression.Constant(targetType);
            var arg2  = Expression.Constant(isNullable);

            for (int i = 0; i < xfAttrs.Length; i++)
            {
                // val = xfAttrs[i].Transform(val, targetType, isNullable);
                exprs.Add(Expression.Assign(val, Expression.Call(Expression.Constant(xfAttrs[i]), meth, val, arg1, arg2)));
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

        protected static Expression CreateUnboxedRetrieval<T>(
            ParameterExpression                       dbReader,
            Expression                                index,
            IEnumerable<DataTransformerAttributeBase> xFormers,
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
                res = Expression.Convert(res, dbType);
            }
            else
                res = Expression.Call(dbReader, typeof(IDataRecord).GetMethod("Get" + Type.GetTypeCode(dbType)), index);

            if (switchSign)
                res = Expression.Convert(res, unswitched);
            if (isEnum)
                res = Expression.Convert(res, nonNull);

            if (isNullable)
            {
                res = Expression.Convert(res, type);

                body.Add(
                    Expression.Assign(
                        retVal,
                        Expression.Condition(
                            Expression.Call(dbReader, IsDbNullMethod, index),
                            Expression.Constant(default(T), type),
                            res
                        )
                    )
                );
            }
            else
            {
                body.Add(
                    Expression.IfThenElse(
                        Expression.Call(dbReader, IsDbNullMethod, index),
                        Expression.Throw(
                            Expression.New(
                                typeof(NoNullAllowedException).GetConstructor(new[] { typeof(string) }),
                                Expression.Constant("Expected result of type " + dbType + " for single column result set.")
                            )
                        ),
                        Expression.Assign(retVal, res)
                    )
                );
            }

            var method = typeof(IDataTransformerAttribute<T>).GetMethod("Transform");
            foreach (var xf in converted)
                body.Add(Expression.Assign(retVal, Expression.Call(Expression.Constant(xf), method, retVal)));

            body.Add(retVal);

            return Expression.Block(type, parms, body);
        }
    }
}
