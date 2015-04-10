﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Contracts;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using Microsoft.CSharp.RuntimeBinder;

namespace CodeOnlyStoredProcedure.Dynamic
{
    internal class DynamicStoredProcedure : DynamicObject
    {
        internal const string asyncParameterDirectionError = "Can not execute a stored procedure asynchronously if called with a ref or out parameter. You can retrieve output or return values from the stored procedure if you pass in an input class, which the library can parse into the correct properties.";
        private static readonly Func<CSharpArgumentInfo, string>                  getParameterName;
        private static readonly Func<CSharpArgumentInfo, ParameterDirection>      getParameterDirection;
        private static readonly Func<InvokeMemberBinder, int, CSharpArgumentInfo> getArgumentInfo;
        private        readonly IDbConnection                                     connection;
        private        readonly IEnumerable<IDataTransformer>                     transformers;
        private        readonly string                                            schema;
        private        readonly CancellationToken                                 token;
        private        readonly int                                               timeout;
        private        readonly DynamicExecutionMode                              executionMode;

        static DynamicStoredProcedure()
        {
            getArgumentInfo       = CreateArgumentInfoGetter();
            getParameterDirection = CreateParameterDirectionGetter();
            getParameterName      = CreateParameterNameGetter();
        }

        public DynamicStoredProcedure(IDbConnection                 connection,
                                      IEnumerable<IDataTransformer> transformers,
                                      CancellationToken             token,
                                      int                           timeout,
                                      DynamicExecutionMode          executionMode)
        {
            Contract.Requires(connection   != null);
            Contract.Requires(transformers != null);

            this.connection    = connection;
            this.transformers  = transformers;
            this.token         = token;
            this.timeout       = timeout;
            this.executionMode = executionMode;
        }

        private DynamicStoredProcedure(IDbConnection                 connection,
                                       IEnumerable<IDataTransformer> transformers,
                                       CancellationToken             token,
                                       int                           timeout,
                                       string                        schema,
                                       DynamicExecutionMode          executionMode)
        {
            Contract.Requires(connection   != null);
            Contract.Requires(transformers != null);
            Contract.Requires(!string.IsNullOrEmpty(schema));

            this.connection    = connection;
            this.transformers  = transformers;
            this.schema        = schema;
            this.token         = token;
            this.timeout       = timeout;
            this.executionMode = executionMode;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (!string.IsNullOrEmpty(schema))
                throw new StoredProcedureException(string.Format("Schema already specified once. \n\tExisting schema: {0}\n\tAdditional schema: {1}", schema, binder.Name));

            result = new DynamicStoredProcedure(connection, transformers, token, timeout, binder.Name, executionMode);
            return true;
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            var parameters = new List<IStoredProcedureParameter>();
            
            for (int i = 0; i < binder.CallInfo.ArgumentCount; ++i)
            {
                // the first item in ICSharpInvokeOrInvokeMemberBinder.Arguments seems to be this object (c-style method calling)
                var argument  = getArgumentInfo(binder, i + 1);
                var direction = getParameterDirection(argument);
                var parmName  = getParameterName(argument);
                var idx       = i;  // store the value, otherwise when it is lifted to lambdas, i will be the binder.CallInfo.ArgumentCount
                var argType   = args[idx].GetType();

                if (argType.IsEnumeratedType())
                {
                    // it is a TableValuedParameter, so we need to get the SQL Server type name
                    var itemType = argType .GetEnumeratedType();
                    var attr     = itemType.GetCustomAttributes(false)
                                           .OfType<TableValuedParameterAttribute>()
                                           .FirstOrDefault();

                    if (attr == null)
                        throw new NotSupportedException("You must apply the TableValuedParameter attribute to a class to use as a Table Valued Parameter when using the dynamic syntax.");

                    parameters.Add(
                        new TableValuedParameter(attr.Name ?? parmName,
                                                 (IEnumerable)args[idx],
                                                 itemType,
                                                 attr.TableName,
                                                 attr.Schema));
                }
                else if (argType.IsClass && argType != typeof(string))
                    parameters.AddRange(argType.GetParameters(args[idx]));
                else if (direction == ParameterDirection.Output)
                {
                    VerifySynchronousExecutionMode(executionMode);

                    if ("returnvalue".Equals(parmName, StringComparison.InvariantCultureIgnoreCase))
                        parameters.Add(new ReturnValueParameter(r => args[idx] = r));
                    else
                        parameters.Add(new OutputParameter(parmName, o => args[idx] = o, argType.InferDbType()));
                }
                else if (direction == ParameterDirection.InputOutput)
                {
                    VerifySynchronousExecutionMode(executionMode);

                    parameters.Add(new InputOutputParameter(parmName, o => args[idx] = o, args[idx], argType.InferDbType()));
                }
                else
                    parameters.Add(new InputParameter(parmName, args[idx], argType.InferDbType()));
            }

            result = new DynamicStoredProcedureResults(
                connection,
                schema ?? "dbo",
                binder.Name,
                timeout,
                parameters,
                transformers,
                executionMode,
                token);

            return true;
        }

        private static void VerifySynchronousExecutionMode(DynamicExecutionMode executionMode)
        {
            if (executionMode == DynamicExecutionMode.Asynchronous)
                throw new NotSupportedException(asyncParameterDirectionError);
        }

        private static Func<InvokeMemberBinder, int, CSharpArgumentInfo> CreateArgumentInfoGetter()
        {
            // this has to be done through reflection, because all the properties are internal to .NET :(
            var csBinder = typeof(RuntimeBinderException).Assembly
                                                         .GetType("Microsoft.CSharp.RuntimeBinder.ICSharpInvokeOrInvokeMemberBinder");

            if (csBinder != null)
            {
                var prop = csBinder.GetProperty("ArgumentInfo");

                if (!prop.CanRead)
                    return null;

                var idxParm = Expression.Parameter(typeof(int), "index");
                var objParm = Expression.Parameter(typeof(InvokeMemberBinder), "o");

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
            // this has to be done through reflection, because all the properties are internal to .NET :(
            var argument = Expression.Parameter(typeof(CSharpArgumentInfo), "argument");

            return Expression.Lambda<Func<CSharpArgumentInfo, ParameterDirection>>(
                Expression.Condition(Expression.IsTrue(Expression.Property(argument, "IsOut")),
                    Expression.Constant(ParameterDirection.Output),
                    Expression.Condition(Expression.IsTrue(Expression.Property(argument, "IsByRef")),
                        Expression.Constant(ParameterDirection.InputOutput),
                        Expression.Constant(ParameterDirection.Input)
                    )
                ), argument).Compile();
        }

        private static Func<CSharpArgumentInfo, string> CreateParameterNameGetter()
        {
            // this has to be done through reflection, because all the properties are internal to .NET :(
            var argument = Expression.Parameter(typeof(CSharpArgumentInfo), "argument");

            return Expression.Lambda<Func<CSharpArgumentInfo, string>>(Expression.Property(argument, "Name"), argument)
                             .Compile();
        }
    }
}
