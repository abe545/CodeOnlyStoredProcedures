using System;
using System.Data;

namespace CodeOnlyStoredProcedure
{
    internal class InputParameter : IInputStoredProcedureParameter
    {
        public string  ParameterName { get; }
        public DbType? DbType        { get; }
        public object  Value         { get; }

        public InputParameter(string name, object value, DbType? dbType = null)
        {
            Value         = value;
            ParameterName = name;
            DbType        = dbType;
        }

        public IDbDataParameter CreateDbDataParameter(IDbCommand command)
        {
            var parm           = command.CreateParameter();
            parm.ParameterName = ParameterName;
            parm.Value         = Value ?? DBNull.Value;
            parm.DbType        = DbType ?? Value?.GetType().InferDbType() ?? System.Data.DbType.Object;
            parm.Direction     = ParameterDirection.Input;

            return parm;
        }

        public override string ToString() => $"@{ParameterName.FormatParameterName()} = {Value.FormatParameterValue()}";
    }
}
