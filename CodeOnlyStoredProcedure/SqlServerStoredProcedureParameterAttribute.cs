using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Server;

namespace CodeOnlyStoredProcedure
{
    /// <summary>
    /// Metadata to apply to a parameter that is passed to a Stored Procedure in Sql Server. Allows you
    /// to specify the <see cref="SqlDbType"/> of the parameter.
    /// </summary>
    public class SqlServerStoredProcedureParameterAttribute : StoredProcedureParameterAttribute
    {
        private SqlDbType? sqlDbType;

        /// <summary>
        /// Gets or sets the <see cref="SqlDbType"/> of the value that is passed to SQL Server
        /// </summary>
        public SqlDbType SqlDbType
        {
            get { return sqlDbType ?? SqlDbType.Variant; }
            set { sqlDbType = value; }
        }

        /// <summary>
        /// Creates an <see cref="IDbDataParameter"/> that will be used to pass data to the <see cref="StoredProcedure"/>.
        /// </summary>
        /// <param name="propertyName">The name of the property that is decorated with this attribute.</param>
        /// <param name="cmd">The <see cref="IDbCommand"/> to create the <see cref="IDbDataParameter"/> for.</param>
        /// <param name="propertyType">The type of the property that this attribute was applied on.</param>
        /// <returns>A <see cref="IDbDataParameter"/> used to pass the property to the stored procedure.</returns>
        public override IDbDataParameter CreateDataParameter(string propertyName, IDbCommand cmd, Type propertyType)
        {
            var res = base.CreateDataParameter(propertyName, cmd, propertyType) as SqlParameter;

            if (res == null)
                throw new NotSupportedException("Can only apply a SqlServerStoredProcedureParameterAttribute to a parameter passed to SQL Server.");

            if (sqlDbType.HasValue)
                res.SqlDbType = sqlDbType.Value;

            return res;
        }

        internal override SqlMetaData CreateSqlMetaData(string propertyName, Type propertyType)
        {
            return propertyType.CreateSqlMetaData(Name ?? propertyName, sqlDbType, ExplicitSize, ExplicitScale, ExplicitPrecision);
        }
    }
}
