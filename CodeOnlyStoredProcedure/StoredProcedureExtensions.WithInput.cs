using System;
using System.Collections;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.Contracts;
using System.Linq;

namespace CodeOnlyStoredProcedure
{
    public static partial class StoredProcedureExtensions
    {
        public static TSP WithInput<TSP, TInput>(this TSP sp, TInput input)
            where TSP : StoredProcedure
        {
            Contract.Requires(sp != null);
            Contract.Requires(input != null);
            Contract.Ensures(Contract.Result<TSP>() != null);

            foreach (var pi in typeof(TInput).GetMappedProperties())
            {
                SqlParameter parameter;
                var tableAttr = pi.GetCustomAttributes(typeof(TableValuedParameterAttribute), false)
                                  .OfType<TableValuedParameterAttribute>()
                                  .FirstOrDefault();
                var attr = pi.GetCustomAttributes(typeof(StoredProcedureParameterAttribute), false)
                             .OfType<StoredProcedureParameterAttribute>()
                             .FirstOrDefault();

                if (tableAttr != null)
                    parameter = tableAttr.CreateSqlParameter(pi.Name);
                else if (attr != null)
                    parameter = attr.CreateSqlParameter(pi.Name);
                else
                    parameter = new SqlParameter(pi.Name, pi.GetValue(input, null));

                // store table values, scalar value or null
                var value = pi.GetValue(input, null);
                if (value == null)
                    parameter.Value = DBNull.Value;
                else if (parameter.SqlDbType == SqlDbType.Structured)
                {
                    // An IEnumerable type to be used as a Table-Valued Parameter
                    if (!(value is IEnumerable))
                        throw new InvalidCastException(string.Format("{0} must be an IEnumerable type to be used as a Table-Valued Parameter", pi.Name));

                    var baseType = value.GetType().GetEnumeratedType();

                    // generate table valued parameter
                    parameter.Value = ((IEnumerable)value).ToTableValuedParameter(baseType);
                }
                else
                    parameter.Value = value;

                switch (parameter.Direction)
                {
                    case ParameterDirection.Input:
                        sp = (TSP)sp.CloneWith(parameter);
                        break;

                    case ParameterDirection.InputOutput:
                    case ParameterDirection.Output:
                        sp = (TSP)sp.CloneWith(parameter, o => pi.SetValue(input, o, null));
                        break;

                    case ParameterDirection.ReturnValue:
                        if (pi.PropertyType != typeof(int))
                            throw new NotSupportedException("Can only use a ReturnValue of type int.");
                        sp = (TSP)sp.CloneWith(parameter, o => pi.SetValue(input, o, null));
                        break;
                }
            }

            return sp;
        }
    }
}
