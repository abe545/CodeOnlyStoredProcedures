using System;
using System.Data;

namespace CodeOnlyStoredProcedure
{
    internal class InputParameter : IInputStoredProcedureParameter
    {
        public string  ParameterName { get; }
        public DbType? DbType        { get; }
        public object  Value         { get; }

        string FormattedParameterName => ParameterName.StartsWith("@") ? ParameterName.Substring(1) : ParameterName;

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

        public override string ToString() => $"@{FormattedParameterName} = '{Value ?? "{null}"}'";
    }
}
