using System;
using System.Data.SqlClient;

namespace CodeOnlyStoredProcedure
{
    [AttributeUsage(AttributeTargets.Property)]
    public class TableValuedParameterAttribute : StoredProcedureParameterAttribute
    {
        /// <summary>
        /// The schema where the table is in the database
        /// </summary>
        public string Schema { get; set; }

        /// <summary>
        /// The name of the table in the database
        /// </summary>
        public string TableName { get; set; }

        public TableValuedParameterAttribute()
        {
            Schema = "dbo";
            SqlDbType = System.Data.SqlDbType.Structured;
        }

        public override SqlParameter CreateSqlParameter(string propertyName)
        {
            var param = base.CreateSqlParameter(propertyName);

            param.TypeName = string.Format("{0}.{1}", Schema, TableName ?? propertyName);

            return param;
        }
    }
}
