using System;
using System.Diagnostics.Contracts;

namespace CodeOnlyStoredProcedure
{
    internal static class RowFactoryCreator
    {
        internal static IRowFactory CreateRowFactory(this Type type)
        {
            Contract.Requires(type != null);
            Contract.Ensures (Contract.Result<IRowFactory>() != null);

            return (IRowFactory)Activator.CreateInstance(typeof(DynamicRowFactory<>).MakeGenericType(type));
        }
    }
}
