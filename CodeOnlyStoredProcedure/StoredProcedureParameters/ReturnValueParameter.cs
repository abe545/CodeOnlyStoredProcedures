using System;
using System.Data;
using System.Diagnostics.Contracts;

namespace CodeOnlyStoredProcedure
{
    internal class ReturnValueParameter : IOutputStoredProcedureParameter
    {
        private readonly Action<int> action;

        public string ParameterName
        {
            get { return "_Code_Only_Stored_Procedures_Auto_Generated_Return_Value_"; }
        }

        public ReturnValueParameter(Action<int> action)
        {
            Contract.Requires(action != null);

            this.action = action;
        }

        public IDbDataParameter CreateDbDataParameter(IDbCommand command)
        {
            var parm           = command.CreateParameter();
            parm.ParameterName = ParameterName;
            parm.Direction     = ParameterDirection.ReturnValue;
            parm.DbType        = DbType.Int32;

            return parm;
        }

        public void TransferOutputValue(object value)
        {
            action((int)value);
        }

        public override string ToString()
        {
            return "@returnValue";
        }
    }
}
