using Microsoft.SqlServer.Server;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace CodeOnlyStoredProcedure
{
    /// <summary>
    /// Defines extension methods on the <see cref="StoredProcedure"/> classes.
    /// </summary>
    public static partial class StoredProcedureExtensions
    {
        private static readonly Type[] integralTpes = new[]
            {
                typeof(String),
                typeof(Int16),
                typeof(Int32),
                typeof(Int64),
                typeof(Decimal),
                typeof(Double),
                typeof(Single),
                typeof(Boolean),
                typeof(Byte),
                typeof(DateTime),
                typeof(Guid)
            };

        internal static IDictionary<Type, IList> Execute(
            this IDbCommand               cmd,
            CancellationToken             token,
            IEnumerable<Type>             outputTypes,
            IEnumerable<IDataTransformer> transformers)
        {
            Contract.Requires(cmd          != null);
            Contract.Requires(outputTypes  != null && Contract.ForAll(outputTypes, t => t != null));
            Contract.Requires(transformers != null);
            Contract.Ensures (Contract.Result<IDictionary<Type, IList>>() != null);

            var first   = true;
            var results = new Dictionary<Type, IList>();
            var reader  = cmd.DoExecute(c => c.ExecuteReader(), token);

            token.ThrowIfCancellationRequested();

            foreach (var currentType in outputTypes)
            {
                if (first)
                    first = false;
                else if (!reader.NextResult())
                    throw new InvalidOperationException("The StoredProcedure returned a different number of result sets than expected. Expected results of type " + outputTypes.Select(t => t.Name).Aggregate((s1, s2) => s1 + ", " + s2));
                
                // process results - repeat this loop for each result set returned
                // by the stored proc for which we have a result type specified
                var values = new object[reader.FieldCount];
                var output = (IList)Activator.CreateInstance (typeof(List<>).MakeGenericType(currentType));

                // process the result set
                if (reader.FieldCount == 1 && (currentType.IsEnum || integralTpes.Contains(currentType)))
                {
                    var  targetType = currentType;
                    bool isNullable = false;
                    if (currentType.IsGenericType && currentType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        isNullable = true;
                        targetType = currentType.GetGenericArguments().Single();
                    }

                    while (reader.Read())
                    {
                        token .ThrowIfCancellationRequested();
                        reader.GetValues(values);

                        var value = values[0];
                        if (DBNull.Value.Equals(value))
                            value = null;

                        foreach (var xform in transformers)
                        {
                            if (xform.CanTransform(value, targetType, isNullable, Enumerable.Empty<Attribute>()))
                                value = xform.Transform(value, targetType, isNullable, Enumerable.Empty<Attribute>());
                        }

                        if (value is string && currentType.IsEnum)
                            value = Enum.Parse(currentType, (string)value);

                        output.Add(value);
                    }
                }
                else
                {
                    var row   = (IRowFactory)Activator.CreateInstance(typeof(RowFactory<>).MakeGenericType(currentType));
                    var names = Enumerable.Range(0, reader.FieldCount)
                                          .Select(i => reader.GetName(i))
                                          .ToArray();

                    while (reader.Read())
                    {
                        token .ThrowIfCancellationRequested();
                        reader.GetValues(values);
                        output.Add(row.CreateRow(names, values, transformers));
                    }

                    if (output.Count > 0)
                    {
                        // throw an exception if the result set didn't include a mapped property
                        if (row.UnfoundPropertyNames.Any())
                            throw new StoredProcedureResultsException(currentType, row.UnfoundPropertyNames.ToArray());
                    }
                }

                results.Add(currentType, output);
            }

            reader.Close();
            return results;
        }

        internal static T DoExecute<T>(this IDbCommand cmd, Func<IDbCommand, T> exec, CancellationToken token)
        {
            Contract.Requires(cmd  != null);
            Contract.Requires(exec != null);
            Contract.Ensures (Contract.Result<T>() != null);

            // execute in a background task, so we can cancel the command if the 
            // CancellationToken is cancelled, or the command times out
            var readerTask = Task.Factory.StartNew(() => exec(cmd), token);

            try
            {
                var timeout = cmd.CommandTimeout;
                if (timeout > 0)
                {
                    if (!readerTask.Wait(timeout * 1000, token) && !readerTask.IsCompleted)
                    {
                        cmd.Cancel();
                        throw new TimeoutException();
                    }

                    if (token.IsCancellationRequested && !readerTask.IsCompleted)
                        cmd.Cancel();
                }
                else
                    readerTask.Wait(token);
            }
            catch (AggregateException ex)
            {
                if (ex.InnerExceptions.Count() == 1)
                {
                    if (!(ex.InnerException is OperationCanceledException))
                        throw;
                    else
                        cmd.Cancel();
                }
                else
                    throw;
            }
            catch (OperationCanceledException)
            {
                cmd.Cancel();
                throw;
            }

            token.ThrowIfCancellationRequested();

            var res = readerTask.Result;
            if (res == null)
                throw new NotSupportedException("The stored procedure did not return any results.");

            return res;
        }

        #region Private Helpers
        private static SqlParameter AddPrecisison(this SqlParameter parameter,
            int? size,
            byte? scale,
            byte? precision)
        {
            Contract.Requires(parameter != null);

            if (size.HasValue)      parameter.Size      = size.Value;
            if (scale.HasValue)     parameter.Scale     = scale.Value;
            if (precision.HasValue) parameter.Precision = precision.Value;

            return parameter;
        }

        private static IDictionary<string, PropertyInfo> GetMappedPropertiesBySqlName(this Type t)
        {
            Contract.Requires(t != null);
            Contract.Ensures(Contract.Result<IDictionary<string, PropertyInfo>>() != null);

            var mappedProperties = new Dictionary<string, PropertyInfo>();

            foreach (var pi in t.GetMappedProperties())
            {
                var name = pi.Name;
                var col = pi.GetCustomAttributes(typeof(ColumnAttribute), false)
                            .OfType<ColumnAttribute>()
                            .FirstOrDefault();
                var tableAttr = pi.GetCustomAttributes(typeof(TableValuedParameterAttribute), false)
                                  .OfType<TableValuedParameterAttribute>()
                                  .FirstOrDefault();
                var attr = pi.GetCustomAttributes(typeof(StoredProcedureParameterAttribute), false)
                             .OfType<StoredProcedureParameterAttribute>()
                             .FirstOrDefault();

                if (col != null && !string.IsNullOrWhiteSpace(col.Name))
                    name = col.Name;
                else if (tableAttr != null && !string.IsNullOrWhiteSpace(tableAttr.Name))
                    name = tableAttr.Name;
                else if (attr != null && !string.IsNullOrWhiteSpace(attr.Name))
                    name = attr.Name;

                mappedProperties.Add(name, pi);
            }

            return mappedProperties;
        }

        static IEnumerable<PropertyInfo> GetMappedProperties(this Type t)
        {
            Contract.Requires(t != null);
            Contract.Ensures(Contract.Result<IEnumerable<PropertyInfo>>() != null);

            return t.GetProperties()
                    .Where(p => !p.GetCustomAttributes(typeof(NotMappedAttribute), false).Any())
                    .ToArray();
        }

        static Type GetEnumeratedType(this Type t)
        {
            Contract.Requires(t != null);

            return t.GetInterfaces()
                    .Where (i => i.GetGenericTypeDefinition().Equals(typeof(IEnumerable<>)))
                    .Select(i => i.GetGenericArguments().First())
                    .FirstOrDefault();
        }

        static IEnumerable<SqlDataRecord> ToTableValuedParameter(this IEnumerable table, Type enumeratedType)
        {
            Contract.Requires(table != null);
            Contract.Requires(enumeratedType != null);
            Contract.Ensures(Contract.Result<IEnumerable<SqlDataRecord>>() != null);

            var recordList = new List<SqlDataRecord>();
            var columnList = new List<SqlMetaData>();
            var props = enumeratedType.GetMappedProperties().ToList();
            string name;
            SqlDbType coltype;

            foreach (var pi in props)
            {
                var attr = pi.GetCustomAttributes(false)
                             .OfType<StoredProcedureParameterAttribute>()
                             .FirstOrDefault();
                if (attr != null && !string.IsNullOrWhiteSpace(attr.Name))
                    name = attr.Name;
                else
                    name = pi.Name;

                if (attr != null && attr.SqlDbType.HasValue)
                    coltype = attr.SqlDbType.Value;
                else
                    coltype = pi.PropertyType.InferSqlType();

                SqlMetaData column;
                switch (coltype)
                {
                    case SqlDbType.Binary:
                    case SqlDbType.Char:
                    case SqlDbType.NChar:
                    case SqlDbType.Image:
                    case SqlDbType.VarChar:
                    case SqlDbType.NVarChar:
                    case SqlDbType.Text:
                    case SqlDbType.NText:
                    case SqlDbType.VarBinary:
                        var size = 50;
                        if (attr != null && attr.Size.HasValue)
                            size = attr.Size.Value;
                        column = new SqlMetaData(name, coltype, size);
                        break;

                    case SqlDbType.Decimal:
                        byte precision = 10;
                        byte scale = 2;

                        if (attr != null)
                        {
                            if (attr.Precision.HasValue) precision = attr.Precision.Value;
                            if (attr.Scale.HasValue)     scale     = attr.Scale.Value;
                        }
                        column = new SqlMetaData(name, coltype, precision, scale);
                        break;

                    default:
                        column = new SqlMetaData(name, coltype);
                        break;
                }

                columnList.Add(column);
            }

            // copy the input list into a list of SqlDataRecords
            foreach (var row in table)
            {
                var record = new SqlDataRecord(columnList.ToArray());
                for (int i = 0; i < columnList.Count; i++)
                {
                    // locate the value of the matching property
                    var value = props[i].GetValue(row, null);
                    record.SetValue(i, value);
                }

                recordList.Add(record);
            }

            return recordList;
        }

        static SqlDbType InferSqlType(this Type type)
        {
            Contract.Requires(type != null);

            if (type == typeof(Int32))
                return SqlDbType.Int;
            if (type == typeof(Double))
                return SqlDbType.Float;
            if (type == typeof(Decimal))
                return SqlDbType.Decimal;
            if (type == typeof(Boolean))
                return SqlDbType.Bit;
            if (type == typeof(String))
                return SqlDbType.NVarChar;
            if (type == typeof(DateTime))
                return SqlDbType.DateTime;
            if (type == typeof(Int64))
                return SqlDbType.BigInt;
            if (type == typeof(Int16))
                return SqlDbType.SmallInt;
            if (type == typeof(Byte))
                return SqlDbType.TinyInt;
            if (type == typeof(Single))
                return SqlDbType.Real;
            if (type == typeof(Guid))
                return SqlDbType.UniqueIdentifier;

            throw new NotSupportedException("Unable to determine the SqlDbType for the property. You can specify it by using a StoredProcedureParameterAttribute. Or prevent it from being mapped with the NotMappedAttribute.");
        }

        private interface IRowFactory
        {
            IEnumerable<string> UnfoundPropertyNames { get; }
            object CreateRow(string[] fieldNames, object[] values, IEnumerable<IDataTransformer> transformers);
        }

        private class RowFactory<T> : IRowFactory
            where T : new()
        {
            private static   IDictionary<string, Action<T, object, IEnumerable<IDataTransformer>>> setters = new Dictionary<string, Action<T, object, IEnumerable<IDataTransformer>>>();
            private readonly HashSet<string>                                                       unfoundProps;

            public IEnumerable<string> UnfoundPropertyNames { get { return unfoundProps; } }

            static RowFactory()
            {
                var tType         = typeof(T);
                var listType      = typeof(IEnumerable<IDataTransformer>);
                var row           = Expression.Parameter(tType,          "row");
                var val           = Expression.Parameter(typeof(object), "value");
                var gts           = Expression.Parameter(listType,       "globalTransformers");
                var xf            = typeof(DataTransformerAttributeBase).GetMethod("Transform");
                var props         = tType.GetMappedPropertiesBySqlName();
                var propertyAttrs = props.ToDictionary(kv => kv.Key,
                                                       kv => kv.Value
                                                               .GetCustomAttributes(false)
                                                               .Cast<Attribute>()
                                                               .ToArray()
                                                               .AsEnumerable());
                
                foreach (var kv in props)
                {
                    var currentType = kv.Value.PropertyType;
                    var expressions = new List<Expression>();
                    var iterType    = typeof(IEnumerator<IDataTransformer>);
                    var iter        = Expression.Variable(iterType, "iter");
                    var propType    = Expression.Constant(currentType, typeof(Type));             
                    var isNullable  = Expression.Constant(currentType.IsGenericType &&
                                                          currentType.GetGenericTypeDefinition() == typeof(Nullable<>));

                    if ((bool)isNullable.Value)
                        propType = Expression.Constant(kv.Value.PropertyType.GetGenericArguments().Single());

                    IEnumerable<Attribute> attrs;
                    if (propertyAttrs.TryGetValue(kv.Key, out attrs))
                    {
                        var propTransformers = attrs.OfType<DataTransformerAttributeBase>().OrderBy(a => a.Order);           
                        var xformType        = typeof(IDataTransformer);
                        var attrsExpr        = Expression.Constant(attrs, typeof(IEnumerable<Attribute>));
                        var transformer      = Expression.Variable(xformType, "transformer");
                        var endFor           = Expression.Label("endForEach");

                        var doTransform = Expression.IfThen(
                            Expression.Call(transformer, xformType.GetMethod("CanTransform"), val, propType, isNullable, attrsExpr),
                            Expression.Assign(val,
                                Expression.Call(transformer, xformType.GetMethod("Transform"), val, propType, isNullable, attrsExpr)));

                        expressions.Add(Expression.Assign(iter, Expression.Call(gts, listType.GetMethod("GetEnumerator"))));
                        var loopBody = Expression.Block(
                            new[] { transformer },
                            Expression.Assign(transformer, Expression.Property(iter, iterType.GetProperty("Current"))),
                            doTransform);

                        expressions.Add(Expression.Loop(
                            Expression.IfThenElse(Expression.Call(iter, typeof(IEnumerator).GetMethod("MoveNext")),
                                                  loopBody,
                                                  Expression.Break(endFor)),
                            endFor));
                                                
                        if (kv.Value.PropertyType.IsEnum)
                        {
                            // nulls are always false for a TypeIs operation, so no need to explicitly check for it
                            expressions.Add(Expression.IfThen(Expression.TypeIs(val, typeof(string)),
                                                              Expression.Assign(val, Expression.Call(typeof(Enum).GetMethod("Parse", new[] { typeof(Type), typeof(string) }),
                                                                                                     propType,
                                                                                                     Expression.Convert(val, typeof(string))))));
                        }
                        
                        foreach (var xform in propTransformers)
                            expressions.Add(Expression.Assign(val, Expression.Call(Expression.Constant(xform), xf, val, propType, isNullable)));
                    }

                    Expression conv;
                    if (kv.Value.PropertyType.IsValueType)
                        conv = Expression.Unbox(val, kv.Value.PropertyType);
                    else
                        conv = Expression.Convert(val, kv.Value.PropertyType);

                    expressions.Add(Expression.Assign(Expression.Property(row, kv.Value), conv));

                    var body   = Expression.Block(new[] { iter }, expressions.ToArray());
                    var lambda = Expression.Lambda<Action<T, object, IEnumerable<IDataTransformer>>>(body, row, val, gts);

                    setters.Add(kv.Key, lambda.Compile());
                }
            }

            public RowFactory()
            {
                unfoundProps = new HashSet<string>(setters.Keys);
            }

            public object CreateRow(
                string[]                      fieldNames, 
                object[]                      values,
                IEnumerable<IDataTransformer> transformers)
            {
                var row = new T();
                Action<T, object, IEnumerable<IDataTransformer>> set;

                for (int i = 0; i < values.Length; i++)
                {
                    var name = fieldNames[i];
                    if (setters.TryGetValue(name, out set))
                    {
                        var value = values[i];
                        if (DBNull.Value.Equals(value))
                            value = null;

                        set(row, value, transformers);
                        unfoundProps.Remove(name);
                    }
                }

                return row;
            }
        }
        #endregion
    }
}
