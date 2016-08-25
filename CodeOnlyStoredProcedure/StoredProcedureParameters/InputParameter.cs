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
            parm.DbType        = GetDbType();
            parm.Direction     = ParameterDirection.Input;

            return parm;
        }

        public override string ToString()
        {
            return string.Format("@{0} = '{1}'", ParameterName.StartsWith("@") ? ParameterName.Substring(1) : ParameterName, Value ?? "{null}");
        }

        private DbType GetDbType()
        {
            if (DbType.HasValue)
                return DbType.Value;

            if (Value != null)
                return Value.GetType().InferDbType();

            return System.Data.DbType.Object;
        }
    }
}
