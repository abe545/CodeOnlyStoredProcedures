using System;

namespace CodeOnlyStoredProcedure
{
    [AttributeUsage(AttributeTargets.Property)]
    public class OptionalResultAttribute : Attribute
    {
    }
}
