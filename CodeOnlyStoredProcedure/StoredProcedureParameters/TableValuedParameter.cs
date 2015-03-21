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

        public   string ParameterName { get; private set; }
        public   object Value         { get { return values; } }
        internal string TypeName      { get; private set; }

        public TableValuedParameter(string name, IEnumerable values, Type valueType, string tableTypeName, string tableTypeSchema = "dbo")
        {
            ParameterName  = name;
            this.values    = values;
            this.valueType = valueType;
            this.TypeName  = string.Format("[{0}].[{1}]", tableTypeSchema, tableTypeName);
        }

        public IDbDataParameter CreateDbDataParameter(IDbCommand command)
        {
            var parm = command.CreateParameter() as SqlParameter;

            if (parm == null)
                throw new NotSupportedException("Can only use Table Valued Parameters with SQL Server");

            parm.ParameterName = ParameterName;
            parm.SqlDbType     = SqlDbType.Structured;
            parm.TypeName      = TypeName;
            parm.Value         = CrateValuedParameter(values, valueType);

            return parm;
        }

        public override string ToString()
        {
            return string.Format("@{0} = IEnumerable<{1}> ({2} items)", ParameterName, valueType, GetValueCount());
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
            var props      = enumeratedType.GetMappedProperties().ToList();

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
