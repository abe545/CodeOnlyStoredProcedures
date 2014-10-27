using System;
using System.Data;

namespace CodeOnlyStoredProcedure
{   
    internal class InputOutputParameter : IInputStoredProcedureParameter, IOutputStoredProcedureParameter
    {
        private readonly Action<object> setter;
        private readonly DbType?        dbType;
        private readonly int?           size;
        private readonly byte?          scale;
        private readonly byte?          precision;

        public string ParameterName { get; private set; }
        public object Value         { get; private set; }

        public InputOutputParameter(string name, Action<object> setter, object value, DbType? dbType = null, int? size = null, byte? scale = null, byte? precision = null)
        {
            ParameterName  = name;
            this.Value     = value;
            this.dbType    = dbType;
            this.size      = size;
            this.scale     = scale;
            this.precision = precision;
        }

        public IDbDataParameter CreateDbDataParameter(IDbCommand command)
        {
            var parm           = command.CreateParameter();
            parm.ParameterName = ParameterName;
            parm.Direction     = ParameterDirection.InputOutput;

            if (Value == null)
                parm.Value = DBNull.Value;
            else
                parm.Value = Value;

            if (dbType.HasValue)
                parm.DbType = dbType.Value;

            parm.AddPrecisison(size, scale, precision);

            return parm;
        }

        public void TransferOutputValue(object value)
        {
            setter(value);
        }

        public override string ToString()
        {
            return string.Format("[InOut] @{0} = '{1}'", ParameterName, Value ?? "{null}");
        }
    }
}
