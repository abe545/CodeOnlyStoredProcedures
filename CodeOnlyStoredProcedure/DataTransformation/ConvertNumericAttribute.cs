using System;

namespace CodeOnlyStoredProcedure.DataTransformation
{
    /// <summary>
    /// Decorate a property with this attribute if the numeric type from the database does not match
    /// the type of the property. Be warned that you may lose some data if the value from the database
    /// doesn't fit in the numeric type you're converting to.
    /// </summary>
    /// <example>
    /// <code language='cs'>
    /// public class Person
    /// {
    ///     // The db returns a double
    ///     [ConvertNumeric]
    ///     public decimal Age { get; set; }
    /// }
    /// </code>
    /// </example>
    public class ConvertNumericAttribute : Attribute { }
}
