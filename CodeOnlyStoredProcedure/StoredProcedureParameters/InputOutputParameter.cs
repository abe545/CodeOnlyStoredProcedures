using System;
using System.Data;
using System.Diagnostics.Contracts;

namespace CodeOnlyStoredProcedure
{   
    internal class InputOutputParameter : IInputStoredProcedureParameter, IOutputStoredProcedureParameter
    {
        private readonly Action<object> setter;

        public   string  ParameterName { get; private set; }
        public   object  Value         { get; private set; }
        public   DbType? DbType        { get; private set; }
        internal int?    Size          { get; private set; }
        internal byte?   Scale         { get; private set; }
        internal byte?   Precision     { get; private set; }

        public InputOutputParameter(string name, Action<object> setter, object value, DbType? dbType = null, int? size = null, byte? scale = null, byte? precision = null)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
            Contract.Requires(setter != null);

            ParameterName  = name.StartsWith("@") ? name.Substring(1) : name;
            this.Value     = value;
            this.DbType    = dbType;
            this.setter    = setter;
            this.Size      = size;
            this.Scale     = scale;
            this.Precision = precision;
        }

        public IDbDataParameter CreateDbDataParameter(IDbCommand command)
        {
            var parm           = command.CreateParameter();
            parm.ParameterName = ParameterName;
            parm.Direction     = ParameterDirection.InputOutput;
            parm.Value         = Value ?? DBNull.Value;

            if (DbType.HasValue)
                parm.DbType = DbType.Value;
            else if (Value != null)
                parm.DbType = Value.GetType().InferDbType();

            parm.AddPrecisison(Size, Scale, Precision);

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
