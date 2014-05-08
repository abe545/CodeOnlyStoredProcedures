using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CodeOnlyStoredProcedure.DataTransformation
{
    /// <summary>
    /// Transforms a value in the db to an Enumuerated value
    /// </summary>
    class EnumValueTransformer : IGlobalTransformer
    {
        private static readonly MethodInfo parseMethod = typeof(Enum).GetMethod("Parse",    new[] { typeof(Type), typeof(string) });
        private static readonly MethodInfo toObjMethod = typeof(Enum).GetMethod("ToObject", new[] { typeof(Type), typeof(object) });

        /// <summary>
        /// Determines if the transformer knows how to transform an object to the <paramref name="targetType"/>.
        /// </summary>
        /// <param name="targetType">The type of the property that is receiving the value</param>
        /// <returns>True if the <paramref name="targetType"/> is an Enum type; false otherwise.</returns>
        public bool CanTransform(Type targetType)
        {
            return targetType.IsEnum;
        }

        /// <summary>
        /// Creates an Expression that will take an object input, and convert it to an Enum of the <paramref name="targetType"/>.
        /// </summary>
        /// <param name="targetType">The type of the property that is receiving the value</param>
        /// <param name="value">A <see cref="ParameterExpression"/> that is the value returned from the database to transform.</param>
        /// <returns>An <see cref="Expression"/> that represents a conversion from value to the <paramref name="targetType"/>.</returns>
        public Expression CreateTransformation(Type targetType, ParameterExpression value)
        {
            var valType  = Expression.Variable(typeof(Type), "valueType");
            var typeExpr = Expression.Constant(targetType);

            return Expression.IfThenElse(Expression.TypeIs(value, typeof(string)),
                                         Expression.Assign(value, Expression.Call(parseMethod, typeExpr, Expression.Convert(value, typeof(string)))),
                                         Expression.Assign(value, Expression.Call(toObjMethod, typeExpr, value)));
        }
    }
}
