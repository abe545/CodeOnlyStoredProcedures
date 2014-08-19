using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;

namespace CodeOnlyStoredProcedure
{
    internal static class TypeExtensions
    {
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

        internal static Type GetEnumeratedType(this Type t)
        {
            Contract.Requires(t != null);

            return t.GetInterfaces()
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
    }
}
