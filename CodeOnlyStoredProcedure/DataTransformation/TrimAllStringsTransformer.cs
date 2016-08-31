using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeOnlyStoredProcedure.DataTransformation
{
    /// <summary>
    /// If you pass this transformer to a <see cref="StoredProcedure"/>, all string values will be trimmed before
    /// the model properties are set.
    /// </summary>
    /// <seealso cref="IDataTransformer{T}"/>
    /// <remarks>If this transformer is used, " Bar  " will be transformed into "Bar" when set on the model properties.</remarks>
    /// <example>
    /// <code language='cs'>
    /// public class DataModel
    /// {
    ///     public IEnumerable&lt;Person&gt; GetPeople_FluentSyntax(IDbConnection db)
    ///     {
    ///         return StoredProcedure.Create("usp_getPeople")
    ///                               .WithDataTransformer(new TrimAllStringsTransformer())
    ///                               .WithResults&lt;Person&gt;()
    ///                               .Execute(db);
    ///     }
    /// 
    ///     public IEnumerable&lt;Person&gt; GetPeople_DynamicSyntax(IDbConnection db)
    ///     {
    ///         return db.Execute(new TrimAllStringsTransformer()).usp_getPeople();
    ///     }
    ///     
    ///     public class Person
    ///     {
    ///         public string FirstName { get; set; }
    ///         public string LastName { get; set; }
    ///     }
    /// }
    /// </code>
    /// </example>
    public class TrimAllStringsTransformer : IDataTransformer<string>
    {
        /// <summary>
        /// Returns true if both value is a string, and targetType is typeof(string).
        /// </summary>
        /// <param name="value">The input value to transform.</param>
        /// <param name="targetType">The type of the property the value is being set on.</param>
        /// <param name="isNullable">If the target property is a nullable of type <paramref name="targetType"/></param>
        /// <param name="propertyAttributes">The attributes applied to the property.</param>
        /// <returns>True if both value is a string, and targetType is typeof(string); false otherwise.</returns>
        public bool CanTransform(object value, Type targetType, bool isNullable, IEnumerable<Attribute> propertyAttributes)
        {
            if (targetType != typeof(string))
                return false;

            if (ReferenceEquals(null, value))
                return true;

            return value is string;
        }

        /// <summary>
        /// Trims the whitespace from the input string
        /// </summary>
        /// <param name="value">The string to trim</param>
        /// <param name="targetType">Must be typeof(string)</param>
        /// <param name="isNullable">If the target property is a nullable of type <paramref name="targetType"/></param>
        /// <param name="propertyAttributes">The attributes applied to the property.</param>
        /// <returns>The trimmed string.</returns>
        public object Transform(object value, Type targetType, bool isNullable, IEnumerable<Attribute> propertyAttributes) => Transform((string)value, propertyAttributes);
        
        /// <summary>
        /// Trims the whitespace from the input string
        /// </summary>
        /// <param name="value">The string to trim</param>
        /// <param name="propertyAttributes">The attributes applied to the property.</param>
        /// <returns>The trimmed string.</returns>
        public string Transform(string value, IEnumerable<Attribute> propertyAttributes)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return value.Trim();
        }
    }
}
