using System;
using System.Collections.Generic;

namespace CodeOnlyStoredProcedure.DataTransformation
{
    /// <summary>
    /// An <see cref="IDataTransformer"/> that will intern all strings returned from a <see cref="StoredProcedure"/>.
    /// </summary>
    /// <seealso cref="IDataTransformer{T}"/>
    /// <remarks>If this transformer is used, all strings will be interned before being set on the model properties.</remarks>
    /// <example>
    /// <code language="C#" title="C#">
    /// public class DataModel
    /// {
    ///     public IEnumerable&lt;Person&gt; GetPeople_FluentSyntax(IDbConnection db)
    ///     {
    ///         return StoredProcedure.Create("usp_getPeople")
    ///                               .WithDataTransformer(new InternAllStringsTransformer())
    ///                               .WithResults&lt;Person&gt;()
    ///                               .Execute(db);
    ///     }
    /// 
    ///     public IEnumerable&lt;Person&gt; GetPeople_DynamicSyntax(IDbConnection db)
    ///     {
    ///         return db.Execute(new InternAllStringsTransformer()).usp_getPeople();
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
    public class InternAllStringsTransformer : IDataTransformer<string>
    {
        /// <summary>
        /// Returns true if the value is a non-null string, and the target type is a string.
        /// </summary>
        /// <param name="value">The value to attempt to transform</param>
        /// <param name="targetType">The type of the property</param>
        /// <param name="isNullable">If the target property is a nullable of type <paramref name="targetType"/></param>
        /// <param name="propertyAttributes">All attributes applied to the property</param>
        /// <returns>True if the value is a string, and so is targetType, false otherwise.</returns>
        public bool CanTransform(object value, Type targetType, bool isNullable, IEnumerable<Attribute> propertyAttributes)
        {
            if (targetType != typeof(string) || !(value is string))
                return false;

            return true;
        }

        /// <summary>
        /// Returns the interned version of the string
        /// </summary>
        /// <param name="value">The string to intern</param>
        /// <param name="targetType">The type of the property</param>
        /// <param name="isNullable">If the target property is a nullable of type <paramref name="targetType"/></param>
        /// <param name="propertyAttributes">All attributes applied to the property</param>
        /// <returns>The interned string</returns>
        public object Transform(object value, Type targetType, bool isNullable, IEnumerable<Attribute> propertyAttributes)
        {
            return string.Intern((string)value);
        }

        /// <summary>
        /// Interns the input string
        /// </summary>
        /// <param name="value">The string to intern</param>
        /// <param name="propertyAttributes">All attributes applied to the property</param>
        /// <returns>The interned string</returns>
        public string Transform(string value, IEnumerable<Attribute> propertyAttributes)
        {
            if (value == null)
                return value;

            return string.Intern(value);
        }
    }
}
