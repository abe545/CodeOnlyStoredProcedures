using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;

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
            Schema = "dbo";
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
            var param = base.CreateDataParameter(propertyName, cmd, propertyType) as SqlParameter;

            if (param == null)
                throw new NotSupportedException("Can only add a TableValued Parameter to a SQL Server Stored Procedure.");

            param.SqlDbType = SqlDbType.Structured;
            param.TypeName  = string.Format("[{0}].[{1}]", Schema, TableName ?? propertyName);

            return param;
        }

        internal override IStoredProcedureParameter CreateParameter(object input, PropertyInfo property)
        {
            return new TableValuedParameter(Name ?? property.Name,
                                            (IEnumerable)property.GetValue(input, null),
                                            property.PropertyType.GetEnumeratedType(),
                                            TableName,
                                            Schema);
        }
    }
}
