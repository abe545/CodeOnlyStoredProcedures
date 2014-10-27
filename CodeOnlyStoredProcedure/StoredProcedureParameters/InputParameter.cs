using System;
using System.Data;

namespace CodeOnlyStoredProcedure
{
    internal class InputParameter : IInputStoredProcedureParameter
    {
        public string ParameterName { get; private set; }
        public DbType DbType        { get; private set; }
        public object Value         { get; private set; }

        public InputParameter(string name, object value, DbType dbType = DbType.Object)
        {
            Value         = value ?? DBNull.Value;
            ParameterName = name;
            DbType        = dbType;
        }

        public IDbDataParameter CreateDbDataParameter(IDbCommand command)
        {
            var parm           = command.CreateParameter();
            parm.ParameterName = ParameterName;
            parm.Value         = Value;
            parm.DbType        = DbType;
            parm.Direction     = ParameterDirection.Input;

            return parm;
        }

        public override string ToString()
        {
            return string.Format("@{0} = '{1}'", ParameterName, Value ?? "{null}");
        }
    }
}
