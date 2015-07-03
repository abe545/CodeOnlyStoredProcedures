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

        public abstract Expression CreateExpressionToGetValueFromReader(
            IDataReader reader, 
            IEnumerable<IDataTransformer> xFormers, 
            Type dbColumnType,
            CodeSteppingInfo codeSteppingInfo = null);

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
        
        private static Expression AddTransformers(
            ParameterExpression            val,
            DataTransformerAttributeBase[] xfAttrs,
            Type                           targetType,
            bool                           isNullable,
            CodeSteppingInfo               codeSteppingInfo = null)
        {
            Contract.Requires(val        != null);
            Contract.Requires(xfAttrs    != null && xfAttrs.Length > 0);
            Contract.Requires(targetType != null);

            var exprs = new List<Expression>();
            var arg1  = Expression.Constant(targetType);
            var arg2  = Expression.Constant(isNullable);

            if (codeSteppingInfo != null)
                exprs.AddRange(codeSteppingInfo.AddTransformers(xfAttrs, attrTransform.Value, val, arg1, arg2));
            else
            {
                for (int i = 0; i < xfAttrs.Length; i++)
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
            out string          exBody,
            CodeSteppingInfo    codeSteppingInfo = null)
        {
            Contract.Requires(dbReader != null);
            Contract.Requires(index    != null);
            Contract.Requires(value    != null);

            var exps = new List<Expression>();

            if (codeSteppingInfo != null)
            {
                exps.Add(codeSteppingInfo.MarkLine(
                    string.Format("object {0} = dbReader.IsDBNull({1}) ? null : dbReader.GetValue({1});", value.Name, GetIndexName(index))));
            }

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
                    exps.Add(AddTransformers(value, xformers, type, isNullable, codeSteppingInfo));
                }
            }
            else
                exBody = null;

            return Expression.Block(exps);
        }

        protected static Expression CreateUnboxingExpression(
            Type                type, 
            bool                isNullable, 
            ParameterExpression value, 
            List<Expression>    exprs, 
            string              notNullableNullValueMessage,
            CodeSteppingInfo    codeSteppingInfo = null)
        {
            Contract.Requires(type  != null);
            Contract.Requires(value != null);
            Contract.Requires(exprs != null);

            if (!isNullable)
            {
                if (codeSteppingInfo != null)
                {
                    exprs.Add(codeSteppingInfo.MarkLine(string.Format("if ({0} == null)", value.Name)));
                    exprs.Add(
                        Expression.IfThen(
                            Expression.ReferenceEqual(value, Expression.Constant(null, typeof(object))),
                            Expression.Block(
                                codeSteppingInfo.MarkLine(string.Format("throw new NoNullAllowedException(\"{0}\");", notNullableNullValueMessage), true),
                                Expression.Throw(
                                    Expression.New(
                                        typeof(NoNullAllowedException).GetConstructor(new[] { typeof(string) }),
                                        Expression.Constant(notNullableNullValueMessage)
                                    )
                                )
                            )
                        )
                    );
                }
                else
                {
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
            ConstantExpression            attributes,
            IEnumerable<IDataTransformer> transformers,
            List<Expression>              expressions,
            CodeSteppingInfo              codeSteppingInfo = null)
        {
            Contract.Requires(resultType   != null);
            Contract.Requires(value        != null);
            Contract.Requires(attributes   != null);
            Contract.Requires(transformers != null);
            Contract.Requires(expressions  != null);

            var isNullable = Expression.Constant(GetUnderlyingNullableType(ref resultType));
            var type       = Expression.Constant(resultType);

            if (codeSteppingInfo != null)
            {
                expressions.AddRange(codeSteppingInfo.AddTransformers(
                    transformers.ToArray(),
                    CanTransform,
                    Transform,
                    value,
                    isNullable,
                    resultType,
                    (Attribute[])attributes.Value,
                    ""));
            }
            else
            {
                foreach (var xf in transformers)
                {
                    // if (xf.CanTrasform(value, type, isNullable, attributes))
                    //     value = xf.Transform(value, type, isNullable, attributes);
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
        }

        protected static Expression CreateTypedRetrieval<T>(
            IDataReader                               reader,
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
            Contract.Requires(reader            != null);
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
                    return CreateUnboxedRetrieval<T>(reader, dbReader, index, xFormers, errorMessage, dbType);
                else
                    throw new StoredProcedureColumnException(typeof(T), dbType, propertyName);
            }

            return unboxedExpression;
        }

        protected static Expression CreateUnboxedRetrieval<T>(
            IDataReader                               reader,
            ParameterExpression                       dbReader,
            Expression                                index,
            IEnumerable<DataTransformerAttributeBase> xFormers,
            string                                    errorMessage,
            Type                                      expectedDbType = null,
            CodeSteppingInfo                          codeSteppingInfo = null)
        {
            Contract.Requires(dbReader != null);
            Contract.Requires(index    != null);
            Contract.Requires(xFormers != null);

            if (!xFormers.All(xf => xf is IDataTransformerAttribute<T>))
                return null;

            var converted  = xFormers.OfType<IDataTransformerAttribute<T>>().ToArray();
            var type       = typeof(T);
            var retVal     = Expression.Variable(type, "retVal");
            var body       = new List<Expression>();
            var dbType     = type;
            var isNullable = GetUnderlyingNullableType(ref dbType);
            var nonNull    = dbType;
            var isEnum     = GetUnderlyingEnumType(ref dbType);
            var unswitched = dbType;
            var switchSign = StripSignForDatabase(ref dbType);

            Expression res;
            string sourceCode;

            if (expectedDbType != null)
            {
                res = CreateCallExpression(reader, dbReader, index, expectedDbType, out sourceCode);
                if (res == null)
                    return null;

                if (nonNull == typeof(bool))
                {
                    if (codeSteppingInfo != null)
                        sourceCode = string.Format("{0} != 0", sourceCode);
                    res = Expression.NotEqual(res, Zero(expectedDbType));
                    if (isNullable)
                    {
                        if (codeSteppingInfo != null)
                            sourceCode = string.Format("(bool?)({0})", sourceCode);
                        res = Expression.Convert(res, type);
                    }
                }
                else if (expectedDbType != dbType)
                {
                    if (codeSteppingInfo != null)
                        sourceCode = string.Format("({0}){1}", dbType.Name, sourceCode);
                    res = Expression.Convert(res, dbType);
                }
            }
            else
            {
                res = CreateCallExpression(reader, dbReader, index, dbType, out sourceCode);
                if (res == null)
                    return null;
            }

            if (switchSign)
            {
                if (codeSteppingInfo != null)
                    sourceCode = string.Format("({0})({1})", unswitched.Name, sourceCode);
                res = Expression.Convert(res, unswitched);
            }
            if (isEnum)
            {
                if (codeSteppingInfo != null)
                    sourceCode = string.Format("({0})({1})", nonNull.Name, sourceCode);
                res = Expression.Convert(res, nonNull);
            }

            if (!converted.Any())
            {
                if (isNullable)
                {
                    var retExpr = Expression.Condition(
                        Expression.Call(dbReader, IsDbNullMethod, index),
                        Expression.Constant(null, type),
                        type.IsClass ? res : Expression.Convert(res, type)
                    );
                    if (codeSteppingInfo != null)
                    {
                        return Expression.Block(type,
                            codeSteppingInfo.MarkLine(string.Format("return {0}.IsDBNull({1}) ? default({2}?) : ({2}?){3}", 
                                                        dbReader.Name, GetIndexName(index), nonNull.Name, sourceCode)),
                            retExpr
                        );
                    }

                    return retExpr;
                }

                if (codeSteppingInfo != null)
                {
                    return Expression.Block(type,
                        codeSteppingInfo.MarkLine(string.Format("if ({0}.IsDBNull({1}))", dbReader.Name, GetIndexName(index))),
                        Expression.IfThen(
                            Expression.Call(dbReader, IsDbNullMethod, index),
                            Expression.Block(
                                codeSteppingInfo.MarkLine("throw new NoNullAllowedException(\"" + errorMessage + "\");", true),
                                Expression.Throw(
                                    Expression.New(
                                        typeof(NoNullAllowedException).GetConstructor(new[] { typeof(string) }),
                                        Expression.Constant(errorMessage)
                                    )
                                )
                            )
                        ),
                        Expression.Block(type,
                            codeSteppingInfo.MarkLine(string.Format("return {0}", sourceCode)),
                            res
                        )
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
                if (codeSteppingInfo != null)
                {
                    body.Add(codeSteppingInfo.MarkLine(string.Format("{0} retVal;", type.Name)));
                    body.Add(codeSteppingInfo.MarkLine(string.Format("if (dbReader.IsDBNull({0}))", GetIndexName(index))));
                    body.Add(Expression.IfThenElse(
                        Expression.Call(dbReader, IsDbNullMethod, index),
                        Expression.Block(
                            codeSteppingInfo.MarkLine("retVal = " + default(T) + ";", true),
                            Expression.Constant(default(T), type)
                        ),
                        Expression.Block(
                            codeSteppingInfo.MarkLine("else"),
                            codeSteppingInfo.MarkLine(string.Format("retVal = {0}", sourceCode), true),
                            Expression.Assign(retVal, res)
                        )
                    ));
                }
                else
                {
                    res = Expression.Condition(
                        Expression.Call(dbReader, IsDbNullMethod, index),
                        Expression.Constant(default(T), type),
                        Expression.Convert(res, type)
                    );
                    body.Add(Expression.Assign(retVal, res));
                }
            }
            else
            {
                if (codeSteppingInfo != null)
                {
                    body.Add(codeSteppingInfo.MarkLine(string.Format("{0} retVal;", type.Name)));
                    body.Add(codeSteppingInfo.MarkLine(string.Format("if (dbReader.IsDBNull({0}))", GetIndexName(index))));
                    body.Add(Expression.IfThenElse(
                        Expression.Call(dbReader, IsDbNullMethod, index),
                        Expression.Block(
                            codeSteppingInfo.MarkLine("throw new NoNullAllowedException(\"" + errorMessage + "\");", true),
                            Expression.Throw(
                                Expression.New(
                                    typeof(NoNullAllowedException).GetConstructor(new[] { typeof(string) }),
                                    Expression.Constant(errorMessage)
                                )
                            )
                        ),
                        Expression.Block(
                            codeSteppingInfo.MarkLine("else"),
                            codeSteppingInfo.MarkLine(string.Format("retVal = {0}", sourceCode), true),
                            Expression.Assign(retVal, res)
                        )
                    ));
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
            }

            var method = DataTransformerCache<T>.attrTransform;
            if (codeSteppingInfo != null)
                body.AddRange(codeSteppingInfo.AddTransformers(converted, method, retVal, "typedAttributeTransformers"));
            else
            {
                foreach (var xf in converted)
                    body.Add(Expression.Assign(retVal, Expression.Call(Expression.Constant(xf), method, retVal)));
            }

            if (codeSteppingInfo != null)
                body.Add(codeSteppingInfo.MarkLine(string.Format("return {0};", retVal.Name)));
            body.Add(retVal);

            return Expression.Block(type, new[] { retVal }, body);
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

        protected static Expression CreateCallExpression(
            IDataReader         reader, 
            ParameterExpression dbReader, 
            Expression          index, 
            Type                type,
            out string          sourceCode)
        {
            Contract.Requires(reader   != null);
            Contract.Requires(dbReader != null);
            Contract.Requires(index    != null);
            Contract.Requires(type     != null);
            Contract.Ensures(Contract.Result<Expression>() == null ^ Contract.ValueAtReturn(out sourceCode) != null);

            var typeCode = Type.GetTypeCode(type);
            var args     = new[] { typeof(int) };

            switch (typeCode)
            {
                case TypeCode.Boolean:
                case TypeCode.Byte:
                case TypeCode.Char:
                case TypeCode.DateTime:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.String:
                    sourceCode = string.Format("{0}.Get{1}({2});", dbReader.Name, typeCode, GetIndexName(index));
                    return Expression.Call(dbReader, typeof(IDataRecord).GetMethod("Get" + typeCode, args), index);
                case TypeCode.Single:
                    sourceCode = string.Format("{0}.GetFloat({1});", dbReader.Name, GetIndexName(index));
                    return Expression.Call(dbReader, typeof(IDataRecord).GetMethod("GetFloat", args), index);
                default:
                    // these can be supported by the data reader, but they don't exist
                    // on the interface. So, we have to handle them once we actually have the reader
                    var foundMethod = reader.GetType().GetMethod("Get" + type.Name, args);

                    if (foundMethod != null)
                    {
                        sourceCode = string.Format("{0}.{1}({2});", dbReader.Name, foundMethod.Name, GetIndexName(index));
                        return Expression.Call(Expression.Convert(dbReader, reader.GetType()), foundMethod, index);
                    }
                    else
                    {
                        sourceCode = null;
                        return null;
                    }
            }
        }

        private static string GetIndexName(Expression indexExpression)
        {
            if (indexExpression is ConstantExpression)
                return ((ConstantExpression)indexExpression).Value.ToString();
            else if (indexExpression is ParameterExpression)
                return ((ParameterExpression)indexExpression).Name;

            throw new NotSupportedException("Can not extract a name for the index expression of type " + indexExpression.GetType());
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
        public override Expression CreateExpressionToGetValueFromReader(
            IDataReader reader,
            IEnumerable<IDataTransformer> xFormers,
            Type dbColumnType,
            CodeSteppingInfo codeSteppingInfo = null)
        {
            Contract.Requires(reader                        != null);
            Contract.Requires(xFormers                      != null && Contract.ForAll(xFormers, x => x != null));
            Contract.Requires(dbColumnType                  != null);
            Contract.Ensures (Contract.Result<Expression>() != null);
            return null;
        }
    }
}
