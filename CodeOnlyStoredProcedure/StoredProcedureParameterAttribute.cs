using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.Contracts;

namespace CodeOnlyStoredProcedure
{
    /// <summary>
    /// Attribute to control parameters passed to a <see cref="StoredProcedure"/> using the <see cref="WithInput"/>
    /// extension method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class StoredProcedureParameterAttribute : Attribute
    {
        /// <summary>
        /// Parameter name override. Default value for parameter name is the name of the 
        /// property. This overrides that default with a user defined name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Defines the direction of data flow for the property/parameter.
        /// </summary>
        public ParameterDirection Direction { get; set; }

        /// <summary>
        /// Size in bytes of returned data. Should be used on output parameters.
        /// </summary>
        public int? Size { get; set; }

        /// <summary>
        /// Size in bytes of returned data. Should be used on output parameters.
        /// </summary>
        public byte? Precision { get; set; }

        /// <summary>
        /// Size in bytes of returned data. Should be used on output parameters.
        /// </summary>
        public byte? Scale { get; set; }

        /// <summary>
        /// Define the SqlDbType for the parameter corresponding to this property. If not set,
        /// the type will be inferred from the property type.
        /// </summary>
        public SqlDbType? SqlDbType { get; set; }

        public StoredProcedureParameterAttribute()
        {
            Direction = ParameterDirection.Input;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public virtual SqlParameter CreateSqlParameter(string propertyName)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(propertyName));
            Contract.Ensures(Contract.Result<SqlParameter>() != null);

            var res = new SqlParameter
            {
                ParameterName = Name ?? propertyName,
                Direction = Direction
            };

            if (Size.HasValue)
                res.Size = Size.Value;
            if (Precision.HasValue)
                res.Precision = Precision.Value;
            if (Scale.HasValue)
                res.Scale = Scale.Value;
            if (SqlDbType.HasValue)
                res.SqlDbType = SqlDbType.Value;

            return res;
        }
    }
}
