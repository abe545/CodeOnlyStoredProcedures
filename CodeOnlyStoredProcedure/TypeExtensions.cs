using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using Microsoft.SqlServer.Server;

namespace CodeOnlyStoredProcedure
{
    /// <summary>
    /// Contains extension methods that operate on <see cref="Type"/>s.
    /// </summary>
    public static class TypeExtensions
    {
        private const byte defaultPrecision = 20;
        private const int  defaultSize      = 50;
        private const byte defaultScale     = 2;

        internal static readonly Type[] integralTypes = new[]
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
                typeof(DateTimeOffset),
                typeof(TimeSpan),
                typeof(Char),
                typeof(Guid)
            };

        internal static IEnumerable<PropertyInfo> GetMappedProperties(
            this Type t, 
                 bool requireWritable = false,
                 bool requireReadable = false)
        {
            Contract.Requires(t != null);
            Contract.Ensures (Contract.Result<IEnumerable<PropertyInfo>>() != null);

            var props = t.GetProperties().AsEnumerable();

            if (requireWritable)
                props = props.Where(p => p.GetSetMethod() != null);
            if (requireReadable)
                props = props.Where(p => p.GetGetMethod() != null);

            return props.Where(p => !p.GetCustomAttributes(typeof(NotMappedAttribute), false).Any())
                        .ToArray();
        }

        internal static bool IsEnumeratedType(this Type t)
        {
            Contract.Requires(t != null);

            return t.IsArray || typeof(IEnumerable).IsAssignableFrom(t) && t.IsGenericType;
        }

        internal static Type GetEnumeratedType(this Type t)
        {
            Contract.Requires(t != null);

            if (t.IsArray)
                return t.GetElementType();

            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return t.GetGenericArguments().Single();

            return t.GetInterfaces()
                    .Where(i => i.IsGenericType)
                    .Where(i => i.GetGenericTypeDefinition().Equals(typeof(IEnumerable<>)))
                    .Select(i => i.GetGenericArguments().First())
                    .FirstOrDefault();
        }

        internal static IDictionary<string, PropertyInfo> GetResultPropertiesBySqlName(this Type t)
        {
            Contract.Requires(t != null);
            Contract.Ensures (Contract.Result<IDictionary<string, PropertyInfo>>() != null);

            var mappedProperties = new Dictionary<string, PropertyInfo>();

            foreach (var pi in t.GetMappedProperties(requireWritable: true))
            {
                var name = pi.Name;
                var col = pi.GetCustomAttributes(typeof(ColumnAttribute), false)
                            .OfType<ColumnAttribute>()
                            .FirstOrDefault();
                var attr = pi.GetCustomAttributes(typeof(StoredProcedureParameterAttribute), false)
                             .OfType<StoredProcedureParameterAttribute>()
                             .FirstOrDefault();

                if (col != null && !string.IsNullOrWhiteSpace(col.Name))
                    name = col.Name;
                else if (attr != null && !string.IsNullOrWhiteSpace(attr.Name))
                    name = attr.Name;

                mappedProperties.Add(name, pi);
            }

            return mappedProperties;
        }

        internal static DbType InferDbType(this Type type)
        {
            Contract.Requires(type != null);

            UnwrapNullable(ref type);

            if (type == typeof(Int32))
                return DbType.Int32;
            else if (type == typeof(Double))
                return DbType.Double;
            else if (type == typeof(Decimal))
                return DbType.Decimal;
            else if (type == typeof(Boolean))
                return DbType.Boolean;
            else if (type == typeof(String))
                return DbType.String;
            else if (type == typeof(DateTime))
                return DbType.DateTime;
            else if (type == typeof(Int64))
                return DbType.Int64;
            else if (type == typeof(Int16))
                return DbType.Int16;
            else if (type == typeof(Byte))
                return DbType.Byte;
            else if (type == typeof(Single))
                return DbType.Single;
            else if (type == typeof(Guid))
                return DbType.Guid;
            else if (type == typeof(UInt16))
                return DbType.UInt16;
            else if (type == typeof(UInt32))
                return DbType.UInt32;
            else if (type == typeof(UInt64))
                return DbType.UInt64;
            else if (type == typeof(SByte))
                return DbType.SByte;
            else if (type == typeof(Char))
                return DbType.StringFixedLength;

            return DbType.Object;
        }

        internal static void SetTypePrecisionAndScale(this Type        type, 
                                                      IDbDataParameter parameter, 
                                                      DbType?          specifiedType,
                                                      int?             specifiedSize,
                                                      byte?            specifiedScale,
                                                      byte?            specifiedPrecision)
        {
            Contract.Requires(parameter != null);
            Contract.Requires(type      != null);

            UnwrapNullable(ref type);

            if (specifiedType != null)
                parameter.DbType = specifiedType.Value;
            else if (type == typeof(Char))
            {
                parameter.DbType = DbType.StringFixedLength;
                parameter.Size   = specifiedSize ?? 1;
            }
            else
                parameter.DbType = type.InferDbType();

            switch (parameter.DbType)
            {
                case DbType.AnsiStringFixedLength:
                case DbType.StringFixedLength:
                case DbType.AnsiString:
                case DbType.String:
                case DbType.Binary:
                    parameter.Size = specifiedSize ?? defaultSize;
                    break;

                case DbType.Currency:
                case DbType.Decimal:
                    parameter.Precision = specifiedPrecision ?? defaultPrecision;
                    parameter.Scale     = specifiedScale     ?? defaultScale;
                    break;
            }
        }

        internal static SqlMetaData CreateSqlMetaData(this Type  type, 
                                                      string     columnName, 
                                                      SqlDbType? specifiedSqlDbType,
                                                      int?       specifiedSize,
                                                      byte?      specifiedScale,
                                                      byte?      specifiedPrecision)
        {
            Contract.Requires(type != null);
            Contract.Requires(!string.IsNullOrWhiteSpace(columnName));
            Contract.Ensures(Contract.Result<SqlMetaData>() != null);

            if (!specifiedSqlDbType.HasValue)
            {
                UnwrapNullable(ref type);

                if (type == typeof(string))
                    specifiedSqlDbType = SqlDbType.NVarChar;
                else if (type == typeof(char))
                {
                    specifiedSqlDbType = SqlDbType.NChar;
                    specifiedSize = specifiedSize ?? 1;
                }
                else if (type == typeof(Decimal))
                    specifiedSqlDbType = SqlDbType.Decimal;
                else if (type == typeof(Int32))
                    specifiedSqlDbType = SqlDbType.Int;
                else if (type == typeof(Double))
                    specifiedSqlDbType = SqlDbType.Float;
                else if (type == typeof(Boolean))
                    specifiedSqlDbType = SqlDbType.Bit;
                else if (type == typeof(DateTime))
                    specifiedSqlDbType = SqlDbType.DateTime;
                else if (type == typeof(Int64))
                    specifiedSqlDbType = SqlDbType.BigInt;
                else if (type == typeof(Int16))
                    specifiedSqlDbType = SqlDbType.SmallInt;
                else if (type == typeof(Byte))
                    specifiedSqlDbType = SqlDbType.TinyInt;
                else if (type == typeof(Single))
                    specifiedSqlDbType = SqlDbType.Real;
                else if (type == typeof(Guid))
                    specifiedSqlDbType = SqlDbType.UniqueIdentifier;
                else
                    throw new NotSupportedException("Could not determine the type of " + columnName + " for the Table Valued Parameter. You can specify the desired type by decorating the property of your model with a SqlServerStoredProcedureParameter attribute.");
            }

            switch (specifiedSqlDbType.Value)
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
                    return new SqlMetaData(columnName, specifiedSqlDbType.Value, specifiedSize ?? defaultSize);

                case SqlDbType.Decimal:
                    return new SqlMetaData(columnName, specifiedSqlDbType.Value, specifiedPrecision ?? defaultPrecision, specifiedScale ?? defaultScale);

                default:
                    return new SqlMetaData(columnName, specifiedSqlDbType.Value);
            }
        }

        /// <summary>
        /// Gets if a given <see cref="Type"/> can be returned as a the result from a single
        /// column result set. In practice, this returns true for numeric, enum, char, bool, or strings.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> to test.</param>
        /// <returns>True if the Type can be used as the result for a single column stored procedure.</returns>
        [Pure]
        public static bool IsSimpleType(this Type type)
        {
            Contract.Requires(type != null);

            UnwrapNullable(ref type);

            return type.IsEnum || integralTypes.Contains(type);
        }

        /// <summary>
        /// Tests to see if a given <see cref="Type"/> can be returned from a <see cref="StoredProcedure" />.
        /// Will return true for interfaces mapped in <see cref="StoredProcedure.MapResultType"/>
        /// </summary>
        /// <param name="type">The <see cref="Type"/> to test.</param>
        /// <returns>True if a type can be returned from a StoredProcedure.</returns>
        [Pure]
        public static bool IsValidResultType(this Type type)
        {
            Contract.Requires(type != null);

            if (type.IsSimpleType() || GlobalSettings.Instance.InterfaceMap.ContainsKey(type))
                return true;

            return type.GetConstructor(new Type[0]) != null;
        }
        
        internal static IEnumerable<IStoredProcedureParameter> GetParameters(this Type type, object instance)
        {
            Contract.Requires(type     != null);
            Contract.Requires(instance != null);
            Contract.Ensures(Contract.Result<IEnumerable<IStoredProcedureParameter>>() != null);

            foreach (var pi in type.GetMappedProperties())
            {
                IStoredProcedureParameter parameter;
                var tableAttr = pi.GetCustomAttributes(false)
                                  .OfType<TableValuedParameterAttribute>()
                                  .FirstOrDefault();
                var attr = pi.GetCustomAttributes(false)
                             .OfType<StoredProcedureParameterAttribute>()
                             .FirstOrDefault();

                // store table values, scalar value or null
                if (tableAttr == null && pi.PropertyType.IsEnumeratedType())
                {
                    tableAttr = pi.PropertyType
                                  .GetEnumeratedType()
                                  .GetCustomAttributes(false)
                                  .OfType<TableValuedParameterAttribute>()
                                  .FirstOrDefault();
                }

                if (tableAttr != null)
                    parameter = tableAttr.CreateParameter(instance, pi);
                else if (pi.PropertyType.IsEnumeratedType())
                    parameter = CreateTableValuedParameter(pi.PropertyType.GetEnumeratedType(), pi.Name, pi.GetValue(instance, null));
                else if (attr != null)
                    parameter = attr.CreateParameter(instance, pi);
                else
                    parameter = new InputParameter(pi.Name, pi.GetValue(instance, null), pi.PropertyType.InferDbType());

                yield return parameter;
            }
        }

        private static void UnwrapNullable(ref Type type)
        {
            Contract.Requires(type                             != null);
            Contract.Ensures (Contract.ValueAtReturn(out type) != null);

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                type = type.GetGenericArguments()[0];
        }

        internal static IStoredProcedureParameter CreateTableValuedParameter(this Type itemType, string parmName, object items)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(parmName));
            Contract.Requires(itemType != null);
            Contract.Requires(items    != null);

            if (itemType == typeof(string))
                throw new NotSupportedException("You can not use a string as a Table-Valued Parameter, since you really need to use a class with properties.");
            else if (itemType.Name.StartsWith("<"))
                throw new NotSupportedException("You can not use an anonymous type as a Table-Valued Parameter, since you really need to match the type name with something in the database.");
            
            return new TableValuedParameter(parmName, (IEnumerable)items, itemType, itemType.Name);
        }
    }
}
