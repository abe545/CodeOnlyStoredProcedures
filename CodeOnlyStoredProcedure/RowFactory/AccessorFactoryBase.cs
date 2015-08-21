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
        private static Lazy<MethodInfo> isDbNull = new Lazy<MethodInfo>(() => typeof(IDataRecord).GetMethod("IsDBNull"));
        private static Lazy<MethodInfo> getValue = new Lazy<MethodInfo>(() => typeof(IDataRecord).GetMethod("GetValue"));

        protected static MethodInfo IsDbNullMethod { get { return isDbNull.Value; } }
        protected static MethodInfo GetValueMethod { get { return getValue.Value; } }

        public abstract Expression CreateExpressionToGetValueFromReader(
            IDataReader reader, 
            IEnumerable<IDataTransformer> xFormers, 
            Type dbColumnType,
            CodeSteppingInfo codeSteppingInfo = null);

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

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.UInt16:
                    type = typeof(short);
                    return true;

                case TypeCode.UInt32:
                    type = typeof(int);
                    return true;

                case TypeCode.UInt64:
                    type = typeof(long);
                    return true;

                case TypeCode.SByte:
                    type = typeof(byte);
                    return true;
            }

            return false;
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
                exps.Add(codeSteppingInfo.MarkLine("object {0} = dbReader.IsDBNull({1}) ? null : dbReader.GetValue({1});", 
                                                   value.Name, 
                                                   GetIndexName(index)));
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
                    var isNullable = type.GetUnderlyingNullableType(out type);

                    if (codeSteppingInfo != null)
                        exps.Add(codeSteppingInfo.AddTransformers(xformers, value, type, isNullable));
                    else
                    {
                        var arg1  = Expression.Constant(type);
                        var arg2  = Expression.Constant(isNullable);

                        for (int i = 0; i < xformers.Length; i++)
                        {
                            exps.Add(
                                Expression.Assign(
                                    value, 
                                    Expression.Call(
                                        Expression.Constant(xformers[i]), 
                                        DataTransformerCache.AttributeTransform, 
                                        value, 
                                        arg1, 
                                        arg2)));
                        }
                    }
                }
            }
            else
                exBody = null;

            return Expression.Block(exps);
        }

        protected static IEnumerable<Expression> CreateUnboxingExpression<T>(
            Type                dbColumnType, 
            Type                expectedDbType,
            Type                underlyingType,
            bool                isNullable, 
            ParameterExpression value, 
            string              notNullableNullValueMessage,
            string              propertyName,
            bool                convertNumeric,
            bool                isTransformed,
            CodeSteppingInfo    codeSteppingInfo = null)
        {
            Contract.Requires(dbColumnType != null);
            Contract.Requires(value != null);
            Contract.Ensures(Contract.Result<IEnumerable<Expression>>() != null);

            var exprs = new List<Expression>();
            var hasStepper = codeSteppingInfo != null;

            if (!isNullable)
            {
                exprs.Add(MarkIfNecessary(
                    () => string.Format("if ({0} == null)", value.Name),
                    Expression.ReferenceEqual(value, Expression.Constant(null, typeof(object))),
                    () => string.Format("throw new NoNullAllowedException(\"{0}\");", notNullableNullValueMessage),
                    Expression.Throw(
                        Expression.New(
                            typeof(NoNullAllowedException).GetConstructor(new[] { typeof(string) }),
                            Expression.Constant(notNullableNullValueMessage)
                        )
                    ),
                    codeSteppingInfo
                ));
            }

            string ret = null;
            Expression res;
            var easyConvert = typeof(IConvertible).IsAssignableFrom(underlyingType);
            
            if (easyConvert)
            {
                if (hasStepper)
                    ret = string.Format("Convert.To{0}({1})", underlyingType.Name, value.Name);
                res = Expression.Call(null, typeof(Convert).GetMethod("To" + underlyingType.Name, new[] { typeof(object) }), value);
            }
            else
            {
                if (hasStepper)
                    ret = string.Format("({0}){1}", dbColumnType.GetCSharpName(), value.Name);
                if (dbColumnType.IsClass)
                    res = Expression.Convert(value, dbColumnType);
                else
                    res = Expression.Unbox(value, dbColumnType);
            }

            if (isNullable)
            {
                if (hasStepper)
                    ret = string.Format("{0} == null ? default(1) : {2}", value.Name, dbColumnType.GetCSharpName(), ret);
                res = Expression.Condition(
                    Expression.ReferenceEqual(value, Expression.Constant(null, typeof(object))),
                    dbColumnType.IsClass ? Expression.Constant(null, dbColumnType) : Expression.Constant(null, typeof(Nullable<>).MakeGenericType(dbColumnType)),
                    dbColumnType.IsClass ? res : Expression.Convert(res, typeof(Nullable<>).MakeGenericType(dbColumnType))
                );
            }

            if (dbColumnType != expectedDbType && !easyConvert)
            {
                if (convertNumeric)
                {
                    if (expectedDbType == typeof(bool))
                    {
                        if (isNullable)
                        {
                            if (hasStepper)
                                ret = string.Format("{0} == null ? default(bool?) : {0} != 0", ret);

                            res = Expression.Condition(
                                Expression.Equal(res, Expression.Constant(null)),
                                Expression.Constant(default(bool?), typeof(bool?)),
                                Expression.Convert(Expression.NotEqual(res, Zero(dbColumnType, true)), typeof(bool?)));
                        }
                        else
                        {
                            if (hasStepper)
                                ret += " != 0";

                            res = Expression.NotEqual(res, Zero(dbColumnType));
                        }
                    }
                    else
                    {
                        if (hasStepper)
                            ret = string.Format("({0}){1}", expectedDbType.GetCSharpName(), ret);

                        res = Expression.Convert(res, typeof(T));
                    }
                }
                else
                    throw new StoredProcedureColumnException(typeof(T), dbColumnType, propertyName);
            }

            // since there doesn't seem to be unsigned values in sql, the expected type will always
            // be signed. In order for unsigned values to be returned, we have to convert them,
            // even if ConvertNumeric is not specified.
            if (underlyingType != expectedDbType)
            {
                if (hasStepper)
                    ret = string.Format("({0}){1}", underlyingType.GetCSharpName(), ret);
                res = Expression.Convert(res, underlyingType);
            }

            // if the result type is an enum, cast the result!
            if (typeof(T) != underlyingType)
            {
                if (hasStepper)
                    ret = string.Format("({0}){1}", typeof(T).GetCSharpName(), ret);
                res = Expression.Convert(res, typeof(T));
            }

            if (hasStepper)
                exprs.Add(codeSteppingInfo.MarkLine(string.Format("return {0};", ret)));
            exprs.Add(res);

            return exprs;
        }

        protected static void AddTransformers(
            Type                          resultType,
            ParameterExpression           value,
            Attribute[]                   attributes,
            IEnumerable<IDataTransformer> transformers,
            List<Expression>              expressions,
            CodeSteppingInfo              codeSteppingInfo = null)
        {
            Contract.Requires(resultType   != null);
            Contract.Requires(value        != null);
            Contract.Requires(attributes   != null);
            Contract.Requires(transformers != null);
            Contract.Requires(expressions  != null);

            var isNullable = Expression.Constant(resultType.GetUnderlyingNullableType(out resultType));
            var type       = Expression.Constant(resultType);

            if (codeSteppingInfo != null)
            {
                expressions.AddRange(codeSteppingInfo.AddTransformers(
                    transformers.ToArray(),
                    value,
                    isNullable,
                    resultType,
                    attributes,
                    ""));
            }
            else
            {
                var a = Expression.Constant(attributes);
                foreach (var xf in transformers)
                {
                    // if (xf.CanTrasform(value, type, isNullable, attributes))
                    //     value = xf.Transform(value, type, isNullable, attributes);
                    expressions.Add(Expression.IfThen(
                        Expression.IsTrue(Expression.Call(Expression.Constant(xf),
                                                          DataTransformerCache.CanTransform,
                                                          value,
                                                          type,
                                                          isNullable,
                                                          a)),
                        Expression.Assign(value, Expression.Call(Expression.Constant(xf),
                                                                 DataTransformerCache.Transform,
                                                                 value,
                                                                 type,
                                                                 isNullable,
                                                                 a))
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
            bool                                      convertNumeric,
            CodeSteppingInfo                          codeSteppingInfo = null)
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

            if (dbType != expectedDbType || codeSteppingInfo != null)
            {
                if (convertNumeric || dbType == expectedDbType)
                    return CreateUnboxedRetrieval<T>(reader, dbReader, index, xFormers, errorMessage, dbType, codeSteppingInfo);
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
            var isNullable = dbType.GetUnderlyingNullableType(out dbType);
            var nonNull    = dbType;
            var isEnum     = GetUnderlyingEnumType(ref dbType);
            var unswitched = dbType;
            var switchSign = StripSignForDatabase(ref dbType);

            Expression res;
            string sourceCode;
            var generateSourceCode = codeSteppingInfo != null;

            if (expectedDbType != null)
            {
                res = CreateCallExpression(reader, dbReader, index, expectedDbType, out sourceCode);
                if (res == null)
                    return null;

                if (nonNull == typeof(bool))
                {
                    if (generateSourceCode)
                        sourceCode = string.Format("{0} != 0", sourceCode);
                    res = Expression.NotEqual(res, Zero(expectedDbType));
                    if (isNullable)
                    {
                        if (generateSourceCode)
                            sourceCode = string.Format("(bool?)({0})", sourceCode);
                        res = Expression.Convert(res, type);
                    }
                }
                else if (expectedDbType != dbType)
                {
                    if (generateSourceCode)
                        sourceCode = string.Format("({0}){1}", dbType.GetCSharpName(), sourceCode);
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
                if (generateSourceCode)
                    sourceCode = string.Format("({0})({1})", unswitched.GetCSharpName(), sourceCode);
                res = Expression.Convert(res, unswitched);
            }
            if (isEnum)
            {
                if (generateSourceCode)
                    sourceCode = string.Format("({0})({1})", nonNull.GetCSharpName(), sourceCode);
                res = Expression.Convert(res, nonNull);
            }

            if (!converted.Any())
            {
                if (isNullable)
                {
                    return MarkIfNecessary(
                        () => string.Format("return {0}.IsDBNull({1}) ? default({2}) : {3};",
                                            dbReader.Name, GetIndexName(index), type.GetCSharpName(), sourceCode),
                        Expression.Condition(
                            Expression.Call(dbReader, IsDbNullMethod, index),
                            Expression.Constant(null, type),
                            type.IsClass ? res : Expression.Convert(res, type)
                        ),
                        codeSteppingInfo,
                        returnType: type
                    );
                }

                return Expression.Block(
                    type,
                    MarkIfNecessary(
                        () => string.Format("if ({0}.IsDBNull({1}))", dbReader.Name, GetIndexName(index)),
                        Expression.Call(dbReader, IsDbNullMethod, index),
                        () => "throw new NoNullAllowedException(\"" + errorMessage + "\");",
                        Expression.Throw(
                            Expression.New(
                                typeof(NoNullAllowedException).GetConstructor(new[] { typeof(string) }),
                                Expression.Constant(errorMessage)
                            )
                        ),
                        codeSteppingInfo
                    ),
                    MarkIfNecessary(
                        () => string.Format("return {0};", sourceCode),
                        res,
                        codeSteppingInfo,
                        returnType: type
                    )
                );
            }
            else if (isNullable)
            {
                body.Add(MarkIfNecessary(
                    () => string.Format("{0} = {1}.IsDBNull({2}) ? default({3}) : {4};", 
                                        retVal.Name, dbReader.Name, GetIndexName(index), type.GetCSharpName(), sourceCode),
                    Expression.Condition(
                        Expression.Call(dbReader, IsDbNullMethod, index),
                        Expression.Constant(default(T), type),
                        Expression.Convert(res, type)
                    ),
                    codeSteppingInfo
                ));
            }
            else
            {
                if (generateSourceCode)
                    body.Add(codeSteppingInfo.MarkLine("{0} retVal;", type.GetCSharpName()));

                body.Add(MarkIfNecessary(
                    () => string.Format("if (dbReader.IsDBNull({0}))", GetIndexName(index)),
                    Expression.Call(dbReader, IsDbNullMethod, index),
                    () => string.Format("throw new NoNullAllowedException(\"{0}\");", errorMessage),
                    Expression.Throw(
                        Expression.New(
                            typeof(NoNullAllowedException).GetConstructor(new[] { typeof(string) }),
                            Expression.Constant(errorMessage)
                        )
                    ),
                    codeSteppingInfo,
                    () => string.Format("retVal = {0};", sourceCode),
                    Expression.Assign(retVal, res)
                ));
            }

            if (generateSourceCode)
                body.Add(codeSteppingInfo.AddTransformers(converted, retVal, "typedAttributeTransformers"));
            else
            {
                foreach (var xf in converted)
                    body.Add(Expression.Assign(retVal, Expression.Call(Expression.Constant(xf), DataTransformerCache<T>.AttributeTransform, retVal)));
            }

            if (generateSourceCode)
                body.Add(codeSteppingInfo.MarkLine("return {0};", retVal.Name));
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

        protected static void AddTypedTransformers<T>(IEnumerable<IDataTransformer> xFormers, Attribute[] attributes, ref Expression expr)
        {
            Contract.Requires(xFormers                         != null);
            Contract.Requires(attributes                       != null);
            Contract.Requires(expr                             != null);
            Contract.Ensures (Contract.ValueAtReturn(out expr) != null);

            var a = Expression.Constant(attributes);
            foreach (var x in xFormers.OfType<IDataTransformer<T>>())
                expr = Expression.Call(Expression.Constant(x), DataTransformerCache<T>.Transform, expr, a);
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
                    sourceCode = string.Format("{0}.Get{1}({2})", dbReader.Name, typeCode, GetIndexName(index));
                    return Expression.Call(dbReader, typeof(IDataRecord).GetMethod("Get" + typeCode, args), index);
                case TypeCode.Single:
                    sourceCode = string.Format("{0}.GetFloat({1})", dbReader.Name, GetIndexName(index));
                    return Expression.Call(dbReader, typeof(IDataRecord).GetMethod("GetFloat", args), index);
                default:
                    // these can be supported by the data reader, but they don't exist
                    // on the interface. So, we have to handle them once we actually have the reader
                    var foundMethod = reader.GetType().GetMethod("Get" + type.Name, args);

                    if (foundMethod != null)
                    {
                        sourceCode = string.Format("{0}.{1}({2})", dbReader.Name, foundMethod.Name, GetIndexName(index));
                        return Expression.Call(Expression.Convert(dbReader, reader.GetType()), foundMethod, index);
                    }
                    else
                    {
                        sourceCode = null;
                        return null;
                    }
            }
        }

        protected static Expression MarkIfNecessary(
            Func<string> marker,
            Expression actual, 
            CodeSteppingInfo stepper, 
            bool indentOneMore = false,
            Type returnType = null)
        {
            Contract.Requires(marker != null);
            Contract.Requires(actual != null);

            if (stepper != null)
            {
                if (returnType != null)
                    return Expression.Block(returnType, stepper.MarkLine(marker(), indentOneMore), actual);

                return Expression.Block(stepper.MarkLine(marker(), indentOneMore), actual);
            }

            return actual;
        }

        protected static Expression MarkIfNecessary(
            Func<string> ifMarker,
            Expression condition,
            Func<string> bodyMarker,
            Expression body,
            CodeSteppingInfo stepper,
            Func<string> elseMarker = null,
            Expression elseBody = null,
            Type returnType = null)
        {
            Contract.Requires(ifMarker   != null);
            Contract.Requires(condition  != null);
            Contract.Requires(bodyMarker != null);
            Contract.Requires(body       != null);

            var exprs = new List<Expression>();
            if (stepper != null)
            {
                exprs.Add(stepper.MarkLine(ifMarker()));
                body = Expression.Block(stepper.MarkLine(bodyMarker(), true), body);
                if (elseBody != null && elseMarker != null)
                    elseBody = Expression.Block(stepper.MarkLine("else"), stepper.MarkLine(elseMarker(), true), elseBody);
            }

            if (elseBody != null)
                exprs.Add(Expression.IfThenElse(condition, body, elseBody));
            else
                exprs.Add(Expression.IfThen(condition, body));

            if (exprs.Count == 1)
                return exprs[0];

            return Expression.Block(exprs);
        }

        private static string GetIndexName(Expression indexExpression)
        {
            if (indexExpression is ConstantExpression)
                return ((ConstantExpression)indexExpression).Value.ToString();
            else if (indexExpression is ParameterExpression)
                return ((ParameterExpression)indexExpression).Name;

            throw new NotSupportedException("Can not extract a name for the index expression of type " + indexExpression.GetType());
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
