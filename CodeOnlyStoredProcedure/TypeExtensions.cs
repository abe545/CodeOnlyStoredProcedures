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

namespace CodeOnlyStoredProcedure
{
    /// <summary>
    /// Contains extension methods that operate on <see cref="Type"/>s.
    /// </summary>
    public static class TypeExtensions
    {
        internal static readonly Type[] integralTpes = new[]
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
                typeof(Char),
                typeof(Guid)
            };

        internal static readonly ConcurrentDictionary<Type, Type> interfaceMap =
            new ConcurrentDictionary<Type, Type>();

        internal static IEnumerable<PropertyInfo> GetMappedProperties(
            this Type t, 
                 bool requireWritable = false,
                 bool requireReadable = false)
        {
            Contract.Requires(t != null);
            Contract.Ensures (Contract.Result<IEnumerable<PropertyInfo>>() != null);

            var props = t.GetProperties().AsEnumerable();

            if (requireWritable)
                props = props.Where(p => p.CanWrite);
            if (requireReadable)
                props = props.Where(p => p.CanRead);

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

        internal static SqlDbType InferSqlType(this Type type)
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

        internal static IEnumerable<Tuple<PropertyInfo, SqlParameter>> GetParameters(this Type type, object instance)
        {
            Contract.Requires(type     != null);
            Contract.Requires(instance != null);
            Contract.Ensures (Contract.Result<IEnumerable<Tuple<PropertyInfo, SqlParameter>>>() != null);

            foreach (var pi in type.GetMappedProperties())
            {
                SqlParameter parameter;
                var tableAttr = pi.GetCustomAttributes(false)
                                  .OfType<TableValuedParameterAttribute>()
                                  .FirstOrDefault();
                var attr = pi.GetCustomAttributes(false)
                             .OfType<StoredProcedureParameterAttribute>()
                             .FirstOrDefault();

                // store table values, scalar value or null
                var value = pi.GetValue(instance, null);
                if (tableAttr == null && pi.PropertyType.IsEnumeratedType())
                {
                    tableAttr = pi.PropertyType
                                  .GetEnumeratedType()
                                  .GetCustomAttributes(false)
                                  .OfType<TableValuedParameterAttribute>()
                                  .FirstOrDefault();
                }

                if (tableAttr != null)
                    parameter = tableAttr.CreateSqlParameter(pi.Name);
                else if (attr != null)
                    parameter = attr.CreateSqlParameter(pi.Name);
                else
                    parameter = new SqlParameter(pi.Name, value);

                if (value == null)
                    parameter.Value = DBNull.Value;
                else if (parameter.SqlDbType == SqlDbType.Structured)
                {
                    // An IEnumerable type to be used as a Table-Valued Parameter
                    if (!(value is IEnumerable))
                        throw new InvalidCastException(string.Format("{0} must be an IEnumerable type to be used as a Table-Valued Parameter", pi.Name));

                    var baseType = value.GetType().GetEnumeratedType();

                    // generate table valued parameter
                    parameter.Value = ((IEnumerable)value).ToTableValuedParameter(baseType);
                }
                else
                    parameter.Value = value;

                yield return Tuple.Create(pi, parameter);
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

            return type.IsEnum || integralTpes.Contains(type);
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

            if (type.IsSimpleType() || interfaceMap.ContainsKey(type))
                return true;

            return type.GetConstructor(new Type[0]) != null;
        }
    }
}
