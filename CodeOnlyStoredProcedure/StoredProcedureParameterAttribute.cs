using System;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.Contracts;
using System.Reflection;
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
        private int?    size;
        private byte?   precision;
        private byte?   scale;
        private DbType? dbType;

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
        /// Gets the size that has been explicitly set. Will be null if none has been set.
        /// </summary>
        protected int? ExplicitSize { get { return size; } }

        /// <summary>
        /// Size in bytes of returned data. Should be used on output parameters.
        /// </summary>
        public byte Precision
        {
            get { return precision ?? 0; }
            set { precision = value; }
        }

        /// <summary>
        /// Get the precision that has been explicitly set. Will be null if none has been set.
        /// </summary>
        protected byte? ExplicitPrecision { get { return precision; } }

        /// <summary>
        /// Size in bytes of returned data. Should be used on output parameters.
        /// </summary>
        public byte Scale
        {
            get { return scale ?? 0; }
            set { scale = value; }
        }

        /// <summary>
        /// Gets the scale that has been explicitly set. Will be null if none has been set.
        /// </summary>
        protected byte? ExplicitScale { get { return scale; } }

        /// <summary>
        /// Define the SqlDbType for the parameter corresponding to this property. If not set,
        /// the type will be inferred from the property type.
        /// </summary>
        public DbType DbType
        {
            get { return dbType ?? DbType.Object; }
            set { dbType = value; }
        }

        /// <summary>
        /// Gets the DbType that has been explicitly set. Will be null if none has been set.
        /// </summary>
        protected DbType? ExplicitDbType { get { return dbType; } }

        /// <summary>
        /// Creates a new StoredProcedureParameterAttribute.
        /// </summary>
        public StoredProcedureParameterAttribute()
        {
            Direction = ParameterDirection.Input;
        }

        /// <summary>
        /// Creates an <see cref="IDbDataParameter"/> that will be used to pass data to the <see cref="StoredProcedure"/>.
        /// </summary>
        /// <param name="propertyName">The name of the property that is decorated with this attribute.</param>
        /// <param name="cmd">The <see cref="IDbCommand"/> to create the <see cref="IDbDataParameter"/> for.</param>
        /// <param name="propertyType">The type of the property that this attribute was applied on.</param>
        /// <returns>A <see cref="IDbDataParameter"/> used to pass the property to the stored procedure.</returns>
        public virtual IDbDataParameter CreateDataParameter(string propertyName, IDbCommand cmd, Type propertyType)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(propertyName));
            Contract.Requires(cmd                                 != null);
            Contract.Requires(propertyType                        != null);
            Contract.Ensures (Contract.Result<IDbDataParameter>() != null);

            var res           = cmd.CreateParameter();
            res.ParameterName = Name ?? propertyName;
            res.Direction     = Direction;

            propertyType.SetTypePrecisionAndScale(res, dbType, size, scale, precision);

            return res;
        }

        internal virtual SqlMetaData CreateSqlMetaData(string propertyName, Type propertyType)
        {
            Contract.Requires(propertyType != null);
            Contract.Requires(!string.IsNullOrWhiteSpace(propertyName));
            Contract.Ensures(Contract.Result<SqlMetaData>() != null);

            return propertyType.CreateSqlMetaData(Name ?? propertyName, null, size, scale, precision);
        }

        internal virtual IStoredProcedureParameter CreateParameter(object input, PropertyInfo property)
        {
            switch (Direction)
            {
                case ParameterDirection.InputOutput:
                    return new InputOutputParameter(Name ?? property.Name, 
                                                    o => property.SetValue(input, o, null), 
                                                    property.GetValue(input, null),
                                                    dbType ?? property.PropertyType.InferDbType(),
                                                    size,
                                                    scale, 
                                                    precision);

                case ParameterDirection.Output:
                    return new OutputParameter(Name ?? property.Name,
                                               o => property.SetValue(input, o, null), 
                                               dbType ?? property.PropertyType.InferDbType(),
                                               size,
                                               scale, 
                                               precision);

                case ParameterDirection.ReturnValue:
                    return new ReturnValueParameter(i => property.SetValue(input, i, null));

                default:
                case ParameterDirection.Input:
                    return new InputParameter(Name ?? property.Name, 
                                              property.GetValue(input, null), 
                                              dbType ?? property.PropertyType.InferDbType());
            }
        }
    }
}