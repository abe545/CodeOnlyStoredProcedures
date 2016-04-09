using System.Linq;
using System.Reflection;

namespace CodeOnlyStoredProcedure
{
    internal static class PropertyInfoExtensions
    {
        public static bool IsOptional(this PropertyInfo pi)
        {
            return pi.GetCustomAttributes(false).OfType<OptionalResultAttribute>().Any();
        }
    }
}
