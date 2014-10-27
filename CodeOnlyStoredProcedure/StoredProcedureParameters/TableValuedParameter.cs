using System;
using System.Collections;
using System.Data;
using System.Data.SqlClient;

namespace CodeOnlyStoredProcedure
{
    internal class TableValuedParameter : IInputStoredProcedureParameter
    {
        private readonly string      typeName;
        private readonly IEnumerable values;
        private readonly Type        valueType;

        public string ParameterName { get; private set; }
        public object Value         { get { return values; } }

        public TableValuedParameter(string name, IEnumerable values, Type valueType, string tableTypeName, string tableTypeSchema = "dbo")
        {
            ParameterName  = name;
            this.values    = values;
            this.valueType = valueType;
            this.typeName  = string.Format("[{0}].[{1}]", tableTypeSchema, tableTypeName);
        }

        public IDbDataParameter CreateDbDataParameter(IDbCommand command)
        {
            var parm = command.CreateParameter() as SqlParameter;

            if (parm == null)
                throw new NotSupportedException("Can only use Table Valued Parameters with SQL Server");

            parm.ParameterName = ParameterName;
            parm.SqlDbType     = SqlDbType.Structured;
            parm.TypeName      = typeName;
            parm.Value         = values.ToTableValuedParameter(valueType);

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
    }
}
