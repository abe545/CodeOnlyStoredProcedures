using System;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.Contracts;

namespace CodeOnlyStoredProcedure
{
    /// <summary>
    /// Attribute to control parameters passed to a <see cref="StoredProcedure"/> using the <see cref="StoredProcedureExtensions.WithInput"/>
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
        [DefaultValue(ParameterDirection.Input)]
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

        /// <summary>
        /// Creates a new StoredProcedureParameterAttribute.
        /// </summary>
        public StoredProcedureParameterAttribute()
        {
            Direction = ParameterDirection.Input;
        }

        /// <summary>
        /// Creates an <see cref="SqlParameter"/> that will be used to pass data to the <see cref="StoredProcedure"/>.
        /// </summary>
        /// <param name="propertyName">The name of the property that is decorated with this attribute.</param>
        /// <returns>A <see cref="SqlParameter"/> used to pass the property to the stored procedure.</returns>
        public virtual SqlParameter CreateSqlParameter(string propertyName)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(propertyName));
            Contract.Ensures (Contract.Result<SqlParameter>() != null);

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
