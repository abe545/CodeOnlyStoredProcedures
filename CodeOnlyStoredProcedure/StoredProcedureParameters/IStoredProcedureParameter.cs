using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeOnlyStoredProcedure
{
    /// <summary>
    /// Interface that must be implemented in order to be used as a parameter for a <see cref="StoredProcedure"/>.
    /// </summary>
    [ContractClass(typeof(IStoredProcedureParameterContract))]
    public interface IStoredProcedureParameter
    {
        /// <summary>
        /// Gets the name of the parameter passed to the stored procedure
        /// </summary>
        [Pure]
        string ParameterName { get; }

        /// <summary>
        /// Creates the <see cref="IDbDataParameter"/> to pass to the stored procedure
        /// </summary>
        /// <param name="command">The <see cref="IDbCommand"/> that will be used to execute the stored procedure. 
        /// This parameter is mostly used to create the <see cref="IDbDataParameter"/> itself, as each
        /// database implementation requires a different parameter type.</param>
        /// <returns>An <see cref="IDbDataParameter"/> to pass to the stored procedure that represents the data in this parameter.</returns>
        IDbDataParameter CreateDbDataParameter(IDbCommand command);
        
        /// <summary>
        /// Required for implementations to override their ToString() method, so they will provide a string representation of what is being
        /// sent to the Database.
        /// </summary>
        /// <returns>A string representation of what is being sent to the Database.</returns>
        string ToString();
    }

    [ContractClassFor(typeof(IStoredProcedureParameter))]
    abstract class IStoredProcedureParameterContract : IStoredProcedureParameter
    {
        string IStoredProcedureParameter.ParameterName
        {
            get
            {
                Contract.Ensures(!string.IsNullOrWhiteSpace(Contract.Result<string>()));
                return null;
            }
        }

        IDbDataParameter IStoredProcedureParameter.CreateDbDataParameter(IDbCommand command)
        {
            Contract.Requires(command != null);
            Contract.Ensures(Contract.Result<IDbDataParameter>() != null &&
                             Contract.Result<IDbDataParameter>().ParameterName == ((IStoredProcedureParameter)this).ParameterName);

            return null;
        }

        string IStoredProcedureParameter.ToString()
        {
            Contract.Ensures(!string.IsNullOrWhiteSpace(Contract.Result<string>()));
            return null;
        }
    }
}
