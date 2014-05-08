using System;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;

namespace CodeOnlyStoredProcedure.DataTransformation
{
    [ContractClass(typeof(IGlobalTransformerContract))]
    internal interface IGlobalTransformer
    {
        bool       CanTransform        (Type targetType);
        Expression CreateTransformation(Type targetType, ParameterExpression value);
    }

    [ContractClassFor(typeof(IGlobalTransformer))]
    abstract class IGlobalTransformerContract : IGlobalTransformer
    {
        bool IGlobalTransformer.CanTransform(Type targetType)
        {
            Contract.Requires(targetType != null);

            return false;
        }

        Expression IGlobalTransformer.CreateTransformation(Type targetType, ParameterExpression value)
        {
            Contract.Requires(targetType                    != null);
            Contract.Requires(value                         != null);
            Contract.Ensures (Contract.Result<Expression>() != null);

            return null;
        }
    }
}
