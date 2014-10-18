using System;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.Contracts;
using Microsoft.SqlServer.Server;

namespace CodeOnlyStoredProcedure
{
    /// <summary>
    /// Attribute to control parameters passed to a <see cref="StoredProcedure"/> using the <see cref="StoredProcedureExtensions.WithInput"/>
    /// extension method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class StoredProcedureParameterAttribute : Attribute
    {
        private int?       size;
        private byte?      precision;
        private byte?      scale;
        private SqlDbType? sqlDbType;

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
        public int Size
        {
            get { return size ?? 0; }
            set { size = value; }
        }

        /// <summary>
        /// Size in bytes of returned data. Should be used on output parameters.
        /// </summary>
        public byte Precision
        {
            get { return precision ?? 0; }
            set { precision = value; }
        }

        /// <summary>
        /// Size in bytes of returned data. Should be used on output parameters.
        /// </summary>
        public byte Scale
        {
            get { return scale ?? 0; }
            set { scale = value; }
        }

        /// <summary>
        /// Define the SqlDbType for the parameter corresponding to this property. If not set,
        /// the type will be inferred from the property type.
        /// </summary>
        public SqlDbType SqlDbType
        {
            get { return sqlDbType ?? SqlDbType.Variant; }
            set { sqlDbType = value; }
        }

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

            if (size.HasValue)
                res.Size = size.Value;
            if (precision.HasValue)
                res.Precision = precision.Value;
            if (scale.HasValue)
                res.Scale = scale.Value;
            if (sqlDbType.HasValue)
                res.SqlDbType = sqlDbType.Value;

            return res;
        }

        internal SqlDbType GetSqlDbType(Type valueType)
        {
            if (sqlDbType.HasValue)
                return sqlDbType.Value;

            return valueType.InferSqlType();
        }

        internal int GetSize(int defaultSize)
        {
            if (size.HasValue)
                return size.Value;

            return defaultSize;
        }

        internal byte GetPrecision(byte defaultPrecision)
        {
            if (precision.HasValue)
                return precision.Value;

            return defaultPrecision;
        }

        internal byte GetScale(byte defaultScale)
        {
            if (scale.HasValue)
                return scale.Value;

            return defaultScale;
        }
    }
}