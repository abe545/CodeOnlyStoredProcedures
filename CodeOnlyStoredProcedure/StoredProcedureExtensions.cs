using System.Data;
using System.Diagnostics.Contracts;

namespace CodeOnlyStoredProcedure
{
    /// <summary>
    /// Defines extension methods on the <see cref="StoredProcedure"/> classes.
    /// </summary>
    public static partial class StoredProcedureExtensions
    {
        // In case you're wondering where all the other methods are in the solution, thare are grouped
        // underneath in visual studio

        internal static IDbDataParameter AddPrecisison(this IDbDataParameter parameter,
            int? size,
            byte? scale,
            byte? precision)
        {
            Contract.Requires(parameter != null);

            if (size.HasValue)      parameter.Size      = size.Value;
            if (scale.HasValue)     parameter.Scale     = scale.Value;
            if (precision.HasValue) parameter.Precision = precision.Value;

            return parameter;
        }
    }
}
