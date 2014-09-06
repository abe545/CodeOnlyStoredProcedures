using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.Contracts;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CodeOnlyStoredProcedure.Dynamic;
using Microsoft.CSharp.RuntimeBinder;

namespace CodeOnlyStoredProcedure
{
    internal class DynamicStoredProcedure : DynamicObject
    {
        internal const string asyncParameterDirectionError = "Can not execute a stored procedure asynchronously if called with a ref or out parameter. You can retrieve output or return values from the stored procedure if you pass in an input class, which the library can parse into the correct properties.";
        private static readonly Func<CSharpArgumentInfo, string>                  getParameterName      = null;
        private static readonly Func<CSharpArgumentInfo, ParameterDirection>      getParameterDirection = null;
        private static readonly Func<InvokeMemberBinder, int, CSharpArgumentInfo> getArgumentInfo       = null;
        private static readonly Action<SqlParameter>                              none                  = _ => { };
        private        readonly IDbConnection                                     connection;
        private        readonly IEnumerable<IDataTransformer>                     transformers;
        private        readonly string                                            schema;
        private        readonly CancellationToken                                 token;
        private        readonly int                                               timeout;

        static DynamicStoredProcedure()
        {
            getArgumentInfo       = CreateArgumentInfoGetter();
            getParameterDirection = CreateParameterDirectionGetter();
            getParameterName      = CreateParameterNameGetter();
        }

        public DynamicStoredProcedure(IDbConnection                 connection,
                                      IEnumerable<IDataTransformer> transformers,
                                      CancellationToken             token,
                                      int                           timeout = StoredProcedure.defaultTimeout,
                                      string                        schema  = "dbo")
        {
            Contract.Requires(connection   != null);
            Contract.Requires(transformers != null);
            Contract.Requires(!string.IsNullOrEmpty(schema));

            this.connection   = connection;
            this.transformers = transformers;
            this.schema       = schema;
            this.token        = token;
            this.timeout      = timeout;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = new DynamicStoredProcedure(connection, transformers, token, timeout, binder.Name);
            return true;
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            var canBeAsync = true;
            var parameters = new List<Tuple<SqlParameter, Action<SqlParameter>>>();
            
            for (int i = 0; i < binder.CallInfo.ArgumentCount; ++i)
            {
                // the first item in ICSharpInvokeOrInvokeMemberBinder.Arguments seems to be this object (c-style method calling)
                var argument  = getArgumentInfo(binder, i + 1);
                var direction = getParameterDirection(argument);
                var parmName  = getParameterName(argument);
                var idx       = i;

                var argType = args[idx].GetType();
                if (argType.IsClass && argType != typeof(string))
                {
                    var item = args[idx];
                    parameters.AddRange(
                        argType.GetParameters(item)
                               .Select(t => Tuple.Create(t.Item2, new Action<SqlParameter>(p => t.Item1.SetValue(item, p.Value, new object[0])))));
                }
                else if ("returnvalue".Equals(parmName, StringComparison.InvariantCultureIgnoreCase) && direction != ParameterDirection.Input)
                {
                    parameters.Add(
                        Tuple.Create(new SqlParameter() { ParameterName = parmName, Direction = ParameterDirection.ReturnValue },
                                     new Action<SqlParameter>(p => args[idx] = p.Value)));
                    canBeAsync = false;
                }
                else if (direction == ParameterDirection.Output)
                {
                    parameters.Add(
                        Tuple.Create(new SqlParameter() { ParameterName = parmName, Direction = ParameterDirection.Output },
                                     new Action<SqlParameter>(p => args[idx] = p.Value)));
                    canBeAsync = false;
                }
                else if (direction == ParameterDirection.InputOutput)
                {
                    parameters.Add(
                        Tuple.Create(new SqlParameter(parmName, args[idx]) { Direction = ParameterDirection.InputOutput },
                                     new Action<SqlParameter>(p => args[idx] = p.Value)));
                    canBeAsync = false;
                }
                else
                    parameters.Add(Tuple.Create(new SqlParameter(parmName, args[i]), none));
            }

            result = new DynamicStoredProcedureResults(
                connection,
                schema,
                binder.Name,
                timeout,
                parameters,
                transformers,
                canBeAsync,
                token);

            return true;
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
