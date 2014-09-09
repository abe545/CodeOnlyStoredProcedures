using System;
using System.ComponentModel;
using System.Data.SqlClient;

namespace CodeOnlyStoredProcedure
{
    /// <summary>
    /// Attribute used to decorate an input property (or class that will be passed) to denote that it
    /// is a Table Valued Parameter.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
    public class TableValuedParameterAttribute : StoredProcedureParameterAttribute
    {
        /// <summary>
        /// The schema of the table that will be passed as a parameter.
        /// </summary>
        [DefaultValue("dbo")]
        public string Schema { get; set; }

        /// <summary>
        /// The name of the table that will be passed as a parameter. If not set, the name of the Property that
        /// this attribute decorates will be used.
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// Creates a new TableValuedParameterAttribute.
        /// </summary>
        public TableValuedParameterAttribute()
        {
            Schema    = "dbo";
            SqlDbType = System.Data.SqlDbType.Structured;
        }

        /// <summary>
        /// Overridden. Creates an <see cref="SqlParameter"/> that will be used to pass data to the <see cref="StoredProcedure"/>.
        /// </summary>
        /// <param name="propertyName">The name of the property that is decorated with this attribute.</param>
        /// <returns>A <see cref="SqlParameter"/> used to pass the property to the stored procedure.</returns>
        public override SqlParameter CreateSqlParameter(string propertyName)
        {
            var param      = base.CreateSqlParameter(propertyName);
            param.TypeName = string.Format("[{0}].[{1}]", Schema, TableName ?? propertyName);

            return param;
        }
    }
}
