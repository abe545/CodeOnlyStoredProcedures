using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CodeOnlyStoredProcedure
{
    internal static class PropertyInfoExtensions
    {
        public static bool IsOptional(this PropertyInfo pi)
        {
            return pi.GetCustomAttributes(false).OfType<OptionalResultAttribute>().Any();
        }

        public static string GetSqlColumnName(this PropertyInfo pi)
        {
            var col = pi.GetCustomAttributes(typeof(ColumnAttribute), false)
                        .OfType<ColumnAttribute>()
                        .FirstOrDefault();

            if (col != null && !string.IsNullOrWhiteSpace(col.Name))
                return col.Name;

            var attr = pi.GetCustomAttributes(typeof(StoredProcedureParameterAttribute), false)
                         .OfType<StoredProcedureParameterAttribute>()
                         .FirstOrDefault();

            if (attr != null && !string.IsNullOrWhiteSpace(attr.Name))
                return attr.Name;

            return pi.Name;
        }

        public static Func<TItem, TProp> CompileGetter<TItem, TProp>(this PropertyInfo prop)
        {
            var i = Expression.Parameter(typeof(TItem), "i");
            return Expression.Lambda<Func<TItem, TProp>>(Expression.Property(i, prop), i).Compile();
        }
    }
}
