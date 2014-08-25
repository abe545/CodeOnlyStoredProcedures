using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Contracts;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using Microsoft.CSharp.RuntimeBinder;

namespace CodeOnlyStoredProcedure
{
    internal class DynamicStoredProcedure : DynamicObject
    {
        internal const string asyncParameterDirectionError = "Out and Ref parameters will not work when calling asynchronously. You can get the return value and/or output parameters if you create a class to use as input, and mark the properties with the appropriate StoredProcedureParameterAttribute.";
        private static readonly Func<InvokeMemberBinder, IList<Type>>             getTypeArguments      = null;
        private static readonly Func<CSharpArgumentInfo, string>                  getParameterName      = null;
        private static readonly Func<CSharpArgumentInfo, ParameterDirection>      getParameterDirection = null;
        private static readonly Func<InvokeMemberBinder, int, CSharpArgumentInfo> getArgumentInfo       = null;
        private        readonly IDbConnection                                     connection;
        private        readonly bool                                              isAsync;
        private        readonly string                                            schema;
        private        readonly CancellationToken                                 token;
        private        readonly int                                               timeout;


        static DynamicStoredProcedure()
        {
            getTypeArguments      = CreateTypeArgumentsGetter();
            getArgumentInfo       = CreateArgumentInfoGetter();
            getParameterDirection = CreateParameterDirectionGetter();
            getParameterName      = CreateParameterNameGetter();
        }

        public DynamicStoredProcedure(IDbConnection     connection,
                                      bool              isAsync, 
                                      CancellationToken token,
                                      int               timeout = 30,
                                      string            schema = "dbo")
        {
            Contract.Requires(connection != null);
            Contract.Requires(!string.IsNullOrEmpty(schema));

            this.connection = connection;
            this.isAsync    = isAsync;
            this.schema     = schema;
            this.token      = token;
            this.timeout    = timeout;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = new DynamicStoredProcedure(connection, isAsync, token, timeout, binder.Name);
            return true;
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            StoredProcedure sp;
            var             types = getTypeArguments(binder);

            if (types != null && types.Count > 0)
            {
                Type spType;

                switch (types.Count)
                {
                    case 1:
                        spType = typeof(StoredProcedure<>).MakeGenericType(types.ToArray());
                        break;
                    case 2:
                        spType = typeof(StoredProcedure<,>).MakeGenericType(types.ToArray());
                        break;
                    case 3:
                        spType = typeof(StoredProcedure<,,>).MakeGenericType(types.ToArray());
                        break;
                    case 4:
                        spType = typeof(StoredProcedure<,,,>).MakeGenericType(types.ToArray());
                        break;
                    case 5:
                        spType = typeof(StoredProcedure<,,,,>).MakeGenericType(types.ToArray());
                        break;
                    case 6:
                        spType = typeof(StoredProcedure<,,,,,>).MakeGenericType(types.ToArray());
                        break;
                    case 7:
                        spType = typeof(StoredProcedure<,,,,,,>).MakeGenericType(types.ToArray());
                        break;

                    default:
                        throw new NotSupportedException("Only 7 result sets are supported, due to limitations in the .NET tuple.");
                }

                sp = (StoredProcedure)Activator.CreateInstance(spType, schema, binder.Name);
            }
            else
                sp = new StoredProcedure(schema, binder.Name);

            for (int i = 0; i < binder.CallInfo.ArgumentCount; ++i)
            {
                // the first item in ICSharpInvokeOrInvokeMemberBinder.Arguments seems to be this object (c-style method calling)
                var argument  = getArgumentInfo(binder, i + 1);
                var direction = getParameterDirection(argument);
                var parmName  = getParameterName(argument);
                var idx       = i;

                if (isAsync && direction != ParameterDirection.Input)
                    throw new NotSupportedException(asyncParameterDirectionError);

                var argType = args[idx].GetType();
                if (argType.IsClass && argType != typeof(string))
                    sp = sp.WithInput(args[idx], argType);
                else if ("returnvalue".Equals(parmName, StringComparison.InvariantCultureIgnoreCase) && direction != ParameterDirection.Input)
                    sp = sp.WithReturnValue(r => args[idx] = r);
                else if (direction == ParameterDirection.Output)
                    sp = sp.WithOutputParameter(parmName, r => args[idx] = r);
                else if (direction == ParameterDirection.InputOutput)
                    sp = sp.WithInputOutputParameter(parmName, args[i], r => args[idx] = r);
                else
                    sp = sp.WithParameter(parmName, args[i]);
            }

            if (isAsync)
            {
                result = sp.InternalCallAsync(connection, token, timeout);
                return true;
            }
            else
            {
                result = sp.InternalCall(connection, timeout);
                return true;
            }
        }
        
        private static Func<InvokeMemberBinder, IList<Type>> CreateTypeArgumentsGetter()
        {
            var csBinder = typeof(RuntimeBinderException).Assembly
                                                         .GetType("Microsoft.CSharp.RuntimeBinder.ICSharpInvokeOrInvokeMemberBinder");

            if (csBinder != null)
            {
                var prop = csBinder.GetProperty("TypeArguments");

                if (!prop.CanRead)
                    return null;

                var objParm = Expression.Parameter(typeof(InvokeMemberBinder), "o");

                return Expression.Lambda<Func<InvokeMemberBinder, IList<Type>>>(
                    Expression.TypeAs(
                        Expression.Property(
                            Expression.TypeAs(objParm, csBinder),
                            prop.Name),
                        typeof(IList<Type>)), objParm).Compile();
            }

            return null;
        }

        private static Func<InvokeMemberBinder, int, CSharpArgumentInfo> CreateArgumentInfoGetter()
        {
            var csBinder = typeof(RuntimeBinderException).Assembly
                                                         .GetType("Microsoft.CSharp.RuntimeBinder.ICSharpInvokeOrInvokeMemberBinder");

            if (csBinder != null)
            {
                var prop = csBinder.GetProperty("ArgumentInfo");

                if (!prop.CanRead)
                    return null;

                var idxParm  = Expression.Parameter(typeof(int), "index");
                var objParm  = Expression.Parameter(typeof(InvokeMemberBinder), "o");

                return Expression.Lambda<Func<InvokeMemberBinder, int, CSharpArgumentInfo>>(
                    Expression.MakeIndex(
                        Expression.Property(Expression.TypeAs(objParm, csBinder), prop.Name),
                        typeof(IList<CSharpArgumentInfo>).GetProperty("Item"), 
                        new Expression[] { idxParm } ), 
                    objParm,
                    idxParm).Compile();
            }

            return null;
        }

        private static Func<CSharpArgumentInfo, ParameterDirection> CreateParameterDirectionGetter()
        {
            var argument = Expression.Parameter(typeof(CSharpArgumentInfo), "argument");
            var retLoc   = Expression.Label(typeof(ParameterDirection), "returnValue");

            return Expression.Lambda<Func<CSharpArgumentInfo, ParameterDirection>>(
                Expression.Block(
                    typeof(ParameterDirection),
                    new ParameterExpression[0],
                    Expression.IfThenElse(Expression.IsTrue(Expression.Property(argument, "IsOut")),
                        Expression.Return(retLoc, Expression.Constant(ParameterDirection.Output)),
                        Expression.IfThen(Expression.IsTrue(Expression.Property(argument, "IsByRef")),
                            Expression.Return(retLoc, Expression.Constant(ParameterDirection.InputOutput))
                        )
                    ),
                    Expression.Label(retLoc, Expression.Constant(ParameterDirection.Input))),
                    argument).Compile();
        }

        private static Func<CSharpArgumentInfo, string> CreateParameterNameGetter()
        {
            var argument = Expression.Parameter(typeof(CSharpArgumentInfo), "argument");
            var retLoc   = Expression.Label(typeof(string), "returnValue");

            return Expression.Lambda<Func<CSharpArgumentInfo, string>>(
                Expression.TypeAs(
                    Expression.Property(argument, "Name"),
                    typeof(string)), argument).Compile();
        }
    }
}
