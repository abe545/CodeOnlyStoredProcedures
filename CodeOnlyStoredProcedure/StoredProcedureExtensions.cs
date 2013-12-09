using Microsoft.SqlServer.Server;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace CodeOnlyStoredProcedure
{
    public static partial class StoredProcedureExtensions
    {
        #region WithParameter
        public static TSP WithParameter<TSP, TValue>(this TSP sp, string name, TValue value)
            where TSP : StoredProcedure
        {
            return (TSP)sp.CloneWith(new SqlParameter(name, value));
        }

        public static TSP WithParameter<TSP, TValue>(this TSP sp, string name, TValue value, SqlDbType dbType)
            where TSP : StoredProcedure
        {
            return (TSP)sp.CloneWith(new SqlParameter(name, value) { SqlDbType = dbType } );
        }
        #endregion

        #region WithOutputParameter
        public static TSP WithOutputParameter<TSP, TValue>(this TSP sp, 
            string name,
            Action<TValue> setter,
            int? size = null,
            byte? scale = null,
            byte? precision = null)
            where TSP : StoredProcedure
        {
            return (TSP)sp.CloneWith(
                new SqlParameter
                {
                    ParameterName = name,
                    Direction = ParameterDirection.Output
                }.AddPrecisison(size, scale, precision), 
                o => setter((TValue)o));
        }

        public static TSP WithOutputParameter<TSP, TValue>(this TSP sp, 
            string name,
            Action<TValue> setter,
            SqlDbType dbType,
            int? size = null,
            byte? scale = null,
            byte? precision = null)
            where TSP : StoredProcedure
        {
            return (TSP)sp.CloneWith(
                new SqlParameter
                {
                    ParameterName = name,
                    Direction = ParameterDirection.Output,
                    SqlDbType = dbType
                }.AddPrecisison(size, scale, precision), 
                o => setter((TValue)o));
        }
        #endregion

        #region WithInputOutputParameter
        public static TSP WithInputOutputParameter<TSP, TValue>(this TSP sp,
            string name,
            TValue value,
            Action<TValue> setter,
            int? size = null,
            byte? scale = null,
            byte? precision = null)
            where TSP : StoredProcedure
        {
            return (TSP)sp.CloneWith(
                new SqlParameter(name, value)
                {
                    Direction = ParameterDirection.InputOutput
                }.AddPrecisison(size, scale, precision),
                o => setter((TValue)o));
        }

        public static TSP WithInputOutputParameter<TSP, TValue>(this TSP sp,
            string name,
            TValue value,
            Action<TValue> setter,
            SqlDbType dbType,
            int? size = null,
            byte? scale = null,
            byte? precision = null)
            where TSP : StoredProcedure
        {
            return (TSP)sp.CloneWith(
                new SqlParameter(name, value)
                {
                    Direction = ParameterDirection.InputOutput,
                    SqlDbType = dbType
                }.AddPrecisison(size, scale, precision),
                o => setter((TValue)o));
        }
        #endregion

        #region WithReturnValue
        public static TSP WithReturnValue<TSP>(this TSP sp, Action<int> returnValue)
            where TSP : StoredProcedure
        {
            return (TSP)sp.CloneWith(new SqlParameter
                {
                    ParameterName = "_Code_Only_Stored_Procedures_Auto_Generated_Return_Value_",
                    Direction = ParameterDirection.ReturnValue
                }, 
                o => returnValue((int)o));
        }
        #endregion

        #region WithInput
        public static TSP WithInput<TSP, TInput>(this TSP sp, TInput input)
            where TSP : StoredProcedure
        {
            foreach (var pi in typeof(TInput).GetMappedProperties())
            {
                SqlParameter parameter;
                var tableAttr = pi.GetCustomAttribute<TableValuedParameterAttribute>();
                var attr = pi.GetCustomAttribute<StoredProcedureParameterAttribute>();

                if (tableAttr != null)
                    parameter = tableAttr.CreateSqlParameter(pi.Name);
                else if (attr != null)
                    parameter = attr.CreateSqlParameter(pi.Name);
                else
                    parameter = new SqlParameter(pi.Name, pi.GetValue(input));

                // store table values, scalar value or null
                var value = pi.GetValue(input, null);
                if (value == null)
                    parameter.Value = DBNull.Value;
                else if (parameter.SqlDbType == SqlDbType.Structured)
                {
                    // An IEnumerable type to be used as a Table-Valued Parameter
                    if (!(value is IEnumerable))
                        throw new InvalidCastException(string.Format("{0} must be an IEnumerable type to be used as a Table-Valued Parameter", pi.Name));

                    var baseType = value.GetType().GetEnumeratedType();
                    if (tableAttr == null)
                    {
                        tableAttr = baseType.GetCustomAttribute<TableValuedParameterAttribute>();
                        if (tableAttr != null)
                            parameter = tableAttr.CreateSqlParameter(pi.Name);
                    }

                    // generate table valued parameter
                    parameter.Value = ((IEnumerable)value).ToTableValuedParameter(baseType);
                }
                else
                    parameter.Value = value;

                switch (parameter.Direction)
                {
                    case ParameterDirection.Input:
                        sp = (TSP)sp.CloneWith(parameter);
                        break;

                    case ParameterDirection.InputOutput:
                    case ParameterDirection.Output:
                        sp = (TSP)sp.CloneWith(parameter, o => pi.SetValue(input, o));
                        break;

                    case ParameterDirection.ReturnValue:
                        if (pi.PropertyType != typeof(int))
                            throw new NotSupportedException("Can only use a ReturnValue of type int.");
                        sp = (TSP)sp.CloneWith(parameter, o => pi.SetValue(input, o));
                        break;
                }
            }

            return sp;
        }
        #endregion

        #region WithTableValuedParameter
        public static TSP WithTableValuedParameter<TSP, TRow>(this TSP sp, 
            string name,
            IEnumerable<TRow> table)
            where TSP : StoredProcedure
        {
            var p = new SqlParameter
            {
                ParameterName = name,
                SqlDbType = SqlDbType.Structured,
                Value = table.ToTableValuedParameter(typeof(TRow))
            };

            return (TSP)sp.CloneWith(p);
        }
        #endregion

        internal static IDictionary<Type, IList> Execute(this IDbConnection connection,
            string procName,
            CancellationToken token,
            int? commandTimeout = null,
            IEnumerable<SqlParameter> parms = null,
            IEnumerable<Type> outputTypes = null)
        {
            try
            {
                // if we don't create a new connection, connection.Open may throw
                // an exception in multi-threaded scenarios. If we don't Open it first,
                // then the connection may be closed, and it will throw an exception. 
                // We could track the connection state ourselves, but if any other code
                // uses the connection (like an EF DbSet), we could possibly close
                // the connection while a transaction is in process.
                connection = new SqlConnection(connection.ConnectionString);
                connection.Open();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = procName;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = commandTimeout ?? 10;

                    // move parameters to command object
                    if (parms != null)
                    {
                        foreach (var p in parms)
                            cmd.Parameters.Add(p);
                    }

                    if (outputTypes != null && outputTypes.Any())
                        return cmd.Execute(token, outputTypes);
                    else
                    {
                        token.ThrowIfCancellationRequested();
                        cmd.ExecuteNonQuery();
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error reading from stored proc " + procName + ": " + ex.Message, ex);
            }
            finally
            {
                connection.Close();
            }
        }

        #region Private Helpers
        private static SqlParameter AddPrecisison(this SqlParameter parameter,
            int? size,
            byte? scale,
            byte? precision)
        {
            if (size.HasValue)
                parameter.Size = size.Value;
            if (scale.HasValue)
                parameter.Scale = scale.Value;
            if (precision.HasValue)
                parameter.Precision = precision.Value;

            return parameter;
        }

        private static IDictionary<string, PropertyInfo> GetMappedPropertiesBySqlName(this Type t)
        {
            var mappedProperties = new Dictionary<string, PropertyInfo>();

            foreach (var pi in t.GetMappedProperties())
            {
                var name = pi.Name;
                var tableAttr = pi.GetCustomAttribute<TableValuedParameterAttribute>();
                var attr = pi.GetCustomAttribute<StoredProcedureParameterAttribute>();

                if (tableAttr != null && !string.IsNullOrWhiteSpace(tableAttr.Name))
                    name = tableAttr.Name;
                else if (attr != null && !string.IsNullOrWhiteSpace(attr.Name))
                    name = attr.Name;

                mappedProperties.Add(name, pi);
            }

            return mappedProperties;
        }

        static IEnumerable<PropertyInfo> GetMappedProperties(this Type t)
        {
            return t.GetProperties()
                    .Where(p => p.GetCustomAttribute<NotMappedAttribute>() == null)
                    .ToArray();
        }

        static Type GetEnumeratedType(this Type t)
        {
            return t.GetInterfaces()
                    .Where(i => i.GetGenericTypeDefinition().Equals(typeof(IEnumerable<>)))
                    .Select(i => i.GetGenericArguments().First())
                    .FirstOrDefault();
        }

        static IEnumerable<SqlDataRecord> ToTableValuedParameter(this IEnumerable table, Type enumeratedType)
        {
            var recordList = new List<SqlDataRecord>();
            var columnList = new List<SqlMetaData>();
            var props = enumeratedType.GetMappedProperties();
            var mapping = new Dictionary<string, string>();
            string name;
            SqlDbType coltype;

            foreach (var pi in props)
            {
                var attr = pi.GetCustomAttribute<StoredProcedureParameterAttribute>();
                if (attr != null && !string.IsNullOrWhiteSpace(attr.Name))
                    name = attr.Name;
                else
                    name = pi.Name;

                mapping.Add(name, pi.Name);

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

                        if(attr != null)
                        {
                            if (attr.Precision.HasValue)
                                precision = attr.Precision.Value;
                            if (attr.Scale.HasValue)
                                scale = attr.Scale.Value;
                        }
                        column = new SqlMetaData(name, coltype, precision, scale);
                        break;

                    default:
                        column = new SqlMetaData(name, coltype);
                        break;
                }

                columnList.Add(column);
            }

            var propMap = props.ToDictionary(p => p.Name);
            // copy the input list into a list of SqlDataRecords return table.
            foreach (var row in table)
            {
                var record = new SqlDataRecord(columnList.ToArray());
                for (int i = 0; i < columnList.Count; i++)
                {
                    // locate the value of the matching property
                    var value = propMap[mapping[columnList[i].Name]].GetValue(row, null);
                    record.SetValue(i, value);
                }

                recordList.Add(record);
            }

            return recordList;
        }

        static SqlDbType InferSqlType(this Type type)
        {
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

        static IDictionary<Type, IList> Execute(this IDbCommand cmd,
            CancellationToken token,
            IEnumerable<Type> outputTypes)
        {
            PropertyInfo pi;
            token.ThrowIfCancellationRequested();
            var reader = cmd.ExecuteReader();
            var results = new Dictionary<Type, IList>();

            foreach (var currentType in outputTypes)
            {
                // process results - repeat this loop for each result set returned
                // by the stored proc for which we have a result type specified
                var props = currentType.GetMappedPropertiesBySqlName();
                var values = new object[reader.FieldCount];
                var output = (IList)Activator.CreateInstance(
                    typeof(List<>).MakeGenericType(currentType));

                // process the result set
                while (reader.Read())
                {
                    token.ThrowIfCancellationRequested();
                    reader.GetValues(values);
                    var row = Activator.CreateInstance(currentType);

                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var name = reader.GetName(i);
                        if (props.TryGetValue(name, out pi))
                        {
                            pi.SetValue(row, values[i]);
                            props.Remove(name);
                        }
                    }

                    if(props.Count > 0)
                        throw new StoredProcedureResultsException(currentType, props.Keys.ToArray());

                    output.Add(row);
                }

                results.Add(currentType, output);

                if (!reader.NextResult())
                    break;
            }

            reader.Close();
            return results;
        }
        #endregion
    }
}
