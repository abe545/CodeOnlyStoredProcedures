using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.Contracts;
using System.Linq;
using Microsoft.SqlServer.Server;

namespace CodeOnlyStoredProcedure
{
    internal class TableValuedParameter : IInputStoredProcedureParameter
    {
        private readonly IEnumerable values;
        private readonly Type        valueType;

        public   string ParameterName { get; }
        public   object Value         { get { return values; } }
        internal string TypeName      { get; }

        public TableValuedParameter(string name, IEnumerable values, Type valueType, string tableTypeName, string tableTypeSchema = "dbo")
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
            Contract.Requires(values    != null);
            Contract.Requires(valueType != null);
            Contract.Requires(!string.IsNullOrWhiteSpace(tableTypeName));
            Contract.Requires(!string.IsNullOrWhiteSpace(tableTypeSchema));

            ParameterName  = name;
            this.values    = values;
            this.valueType = valueType;
            this.TypeName  = $"[{tableTypeSchema}].[{tableTypeName}]";
        }

        public IDbDataParameter CreateDbDataParameter(IDbCommand command)
        {
            var parm = command.CreateParameter() as SqlParameter;

            if (parm == null)
                throw new NotSupportedException("Can only use Table Valued Parameters with SQL Server");

            parm.ParameterName = ParameterName;
            parm.SqlDbType     = SqlDbType.Structured;
            parm.TypeName      = TypeName;

            if (values.Cast<object>().Any())
                parm.Value = CrateValuedParameter(values, valueType);

            return parm;
        }

        public override string ToString()
        {
            return $"@{ParameterName.FormatParameterName()} = IEnumerable<{valueType}> ({GetValueCount()} items)";
        }

        private int GetValueCount()
        {
            int i = 0;
            foreach (var o in values)
                ++i;

            return i;
        }

        private static IEnumerable<SqlDataRecord> CrateValuedParameter(IEnumerable table, Type enumeratedType)
        {
            Contract.Requires(table != null);
            Contract.Requires(enumeratedType != null);
            Contract.Ensures(Contract.Result<IEnumerable<SqlDataRecord>>() != null);

            var recordList = new List<SqlDataRecord>();
            var columnList = new List<SqlMetaData>();
            var props      = enumeratedType.GetMappedProperties(requireReadable: true).ToList();

            foreach (var pi in props)
            {
                var attr = pi.GetCustomAttributes(false)
                             .OfType<StoredProcedureParameterAttribute>()
                             .FirstOrDefault();

                if (attr != null)
                    columnList.Add(attr.CreateSqlMetaData(pi.Name, pi.PropertyType));
                else
                    columnList.Add(pi.PropertyType.CreateSqlMetaData(pi.Name, null, null, null, null));
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
    }
}
