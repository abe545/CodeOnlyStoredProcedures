using System;
using System.Data;
using System.Diagnostics.Contracts;

namespace CodeOnlyStoredProcedure
{
    internal class OutputParameter : IOutputStoredProcedureParameter
    {
        private readonly Action<object> setter;

        public   string  ParameterName { get; }
        public   DbType? DbType        { get; }
        internal int?    Size          { get; }
        internal byte?   Scale         { get; }
        internal byte?   Precision     { get; }

        public OutputParameter(string name, Action<object> setter, DbType? dbType = null, int? size = null, byte? scale = null, byte? precision = null)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(name));
            Contract.Requires(setter != null);

            this.ParameterName = name;
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

        public void TransferOutputValue(object value) => setter(value);

        public override string ToString() => $"[Out] @{ParameterName.FormatParameterName()}";
    }
}
