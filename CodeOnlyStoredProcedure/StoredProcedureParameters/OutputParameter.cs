using System;
using System.Data;
using System.Diagnostics.Contracts;

namespace CodeOnlyStoredProcedure
{
    internal class OutputParameter : IOutputStoredProcedureParameter
    {
        private readonly Action<object> setter;

        public   string  ParameterName { get; private set; }
        public   DbType? DbType        { get; private set; }
        internal int?    Size          { get; private set; }
        internal byte?   Scale         { get; private set; }
        internal byte?   Precision     { get; private set; }

        public OutputParameter(string name, Action<object> setter, DbType? dbType = null, int? size = null, byte? scale = null, byte? precision = null)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
            Contract.Requires(setter != null);

            this.ParameterName = name.StartsWith("@") ? name.Substring(1) : name;
            this.setter        = setter;
            this.DbType        = dbType;
            this.Size          = size;
            this.Scale         = scale;
            this.Precision     = precision;
        }

        public IDbDataParameter CreateDbDataParameter(IDbCommand command)
        {
            var parm           = command.CreateParameter();
            parm.ParameterName = ParameterName;
            parm.Direction     = ParameterDirection.Output;

            if (DbType.HasValue)
                parm.DbType = DbType.Value;

            parm.AddPrecisison(Size, Scale, Precision);

            return parm;
        }

        public void TransferOutputValue(object value)
        {
            setter(value);
        }

        public override string ToString()
        {
            return string.Format("[Out] @{0}", ParameterName);
        }
    }
}
