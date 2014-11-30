using System;

namespace CodeOnlyStoredProcedure
{
    /// <summary>
    /// Attribute to use to decorate a property if it is an optional column from a stored procedure. If a
    /// property is decorated with this attribute, the stored procedure will not fail if it does not return
    /// the column.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class OptionalResultAttribute : Attribute
    {
    }
}
