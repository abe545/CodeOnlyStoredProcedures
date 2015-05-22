using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.Contracts;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;

namespace CodeOnlyStoredProcedure.Dynamic
{
    internal class DynamicStoredProcedureResults : DynamicObject
    {
        private const string tupleName = "System.Tuple`";
        private static readonly Lazy<MethodInfo> getResultsMethod = new Lazy<MethodInfo>(() => typeof(DynamicStoredProcedureResults).GetMethod("GetResults", BindingFlags.Instance | BindingFlags.NonPublic));
        private static readonly Lazy<Dictionary<int, MethodInfo>> tupleCreates = new Lazy<Dictionary<int, MethodInfo>>(() =>
            {
                return typeof(Tuple).GetMethods(BindingFlags.Static | BindingFlags.Public)
                                    .Where(mi => mi.Name == "Create")
                                    .ToDictionary(mi => mi.GetGenericArguments().Count());
            });

        private readonly Task<IDataReader>             resultTask;
        private readonly IDbConnection                 connection;
        private readonly IDbCommand                    command;
        private readonly IEnumerable<IDataTransformer> transformers;
        private readonly DynamicExecutionMode          executionMode;
        private readonly CancellationToken             token;
        private          bool                          continueOnCaller = true;

        public DynamicStoredProcedureResults(
            IDbConnection                   connection,
            string                          schema,
            string                          name,
            int                             timeout,
            List<IStoredProcedureParameter> parameters,
            IEnumerable<IDataTransformer>   transformers,
            DynamicExecutionMode            executionMode,
            CancellationToken               token)
        {
            Contract.Requires(connection != null);
            Contract.Requires(!string.IsNullOrEmpty(schema));
            Contract.Requires(!string.IsNullOrEmpty(name));
            Contract.Requires(parameters != null);
            Contract.Requires(transformers != null);

            this.executionMode = executionMode;
            this.command       = connection.CreateCommand(schema, name, timeout, out this.connection);
            this.transformers  = transformers;
            this.token         = token;

            foreach (var p in parameters)
                command.Parameters.Add(p.CreateDbDataParameter(command));

            if (executionMode == DynamicExecutionMode.Synchronous)
            {
                var tcs = new TaskCompletionSource<IDataReader>();

                token.ThrowIfCancellationRequested();
                var res = command.ExecuteReader();
                token.ThrowIfCancellationRequested();

                foreach (IDbDataParameter p in command.Parameters)
                {
                    if (p.Direction != ParameterDirection.Input)
                    {
                        var x = parameters.OfType<IOutputStoredProcedureParameter>()
                                          .FirstOrDefault(sp => sp.ParameterName == p.ParameterName);
                        if (x != null)
                            x.TransferOutputValue(p.Value);
                    }
                }

                token.ThrowIfCancellationRequested();
                tcs.SetResult(res);
                resultTask = tcs.Task;
            }
            else
            {
#if !NET40
                var sqlCommand = command as SqlCommand;
                if (sqlCommand != null)
                    resultTask = sqlCommand.ExecuteReaderAsync(token).ContinueWith(r => (IDataReader)r.Result, token);
                else
#endif
                    resultTask = Task.Factory.StartNew(() => command.ExecuteReader(),
                                                       token,
                                                       TaskCreationOptions.None,
                                                       TaskScheduler.Default);

                resultTask = resultTask.ContinueWith(r =>
                                       {
                                           foreach (IDbDataParameter p in command.Parameters)
                                           {
                                               if (p.Direction != ParameterDirection.Input)
                                               {
                                                   var x = parameters.OfType<IOutputStoredProcedureParameter>()
                                                                     .FirstOrDefault(sp => sp.ParameterName == p.ParameterName);
                                                   if (x != null)
                                                       x.TransferOutputValue(p.Value);
                                               }
                                           }
                                       
                                           return r.Result;
                                       }, token);
            }
        }

        public override DynamicMetaObject GetMetaObject(Expression parameter)
        {
            return new Meta(parameter, this);
        }

        private IEnumerable<T> GetResults<T>(bool isSingle)
        {
            return RowFactory<T>.Create(isSingle).ParseRows(resultTask.Result, transformers, token);
        }

        private Task<IEnumerable<T>> CreateSingleContinuation<T>(bool isSingle)
        {
            return resultTask.ContinueWith(_ => GetResults<T>(isSingle), token);
        }

        private Task<T> CreateSingleRowContinuation<T>(bool isSingle)
        {
            return resultTask.ContinueWith(_ => GetResults<T>(isSingle).SingleOrDefault(), token);
        }

        private T GetMultipleResults<T>()
        {
            Contract.Ensures(Contract.Result<T>() != null);
            
            var types = typeof(T).GetGenericArguments();
            var res   = types.Select((t, i) =>
            {
                if (i > 0)
                    resultTask.Result.NextResult();

                token.ThrowIfCancellationRequested();

                return getResultsMethod.Value
                                       .MakeGenericMethod(t.GetEnumeratedType())
                                       .Invoke(this, new object[] { false });
            }).ToArray();

            return (T)tupleCreates.Value[types.Length]
                                  .MakeGenericMethod(types)
                                  .Invoke(null, res);
        }

        private Task<T> CreateMultipleContinuation<T>()
        {
            Contract.Ensures(Contract.Result<Task<T>>() != null);

            return resultTask.ContinueWith(r => GetMultipleResults<T>(), token);
        }
        
        private object InternalConfigureAwait(bool continueOnCapturedContext)
        {
            continueOnCaller = continueOnCapturedContext;
            return this;
        }

        private object InternalGetAwaiter()
        {
            if (executionMode == DynamicExecutionMode.Synchronous)
                throw new NotSupportedException(DynamicStoredProcedure.asyncParameterDirectionError);

            return DynamicStoredProcedureResultsAwaiter.Create(this, resultTask, continueOnCaller);
        }

        private class Meta : DynamicMetaObject
        {
            private static readonly Lazy<MethodInfo> configureAwait  = new Lazy<MethodInfo>(() => typeof(DynamicStoredProcedureResults).GetMethod("InternalConfigureAwait", BindingFlags.Instance | BindingFlags.NonPublic));
            private static readonly Lazy<MethodInfo> getAwaiter      = new Lazy<MethodInfo>(() => typeof(DynamicStoredProcedureResults).GetMethod("InternalGetAwaiter", BindingFlags.Instance | BindingFlags.NonPublic));
            private static readonly Lazy<MethodInfo> continueSingle  = new Lazy<MethodInfo>(() => typeof(DynamicStoredProcedureResults).GetMethod("CreateSingleContinuation", BindingFlags.Instance | BindingFlags.NonPublic));
            private static readonly Lazy<MethodInfo> continueMulti   = new Lazy<MethodInfo>(() => typeof(DynamicStoredProcedureResults).GetMethod("CreateMultipleContinuation", BindingFlags.Instance | BindingFlags.NonPublic));
            private static readonly Lazy<MethodInfo> getMultiResults = new Lazy<MethodInfo>(() => typeof(DynamicStoredProcedureResults).GetMethod("GetMultipleResults", BindingFlags.Instance | BindingFlags.NonPublic));
            private static readonly Lazy<MethodInfo> singleExtension = new Lazy<MethodInfo>(() => typeof(Enumerable).GetMethods().Where(m => m.Name == "SingleOrDefault" && m.GetParameters().Length == 1).Single());
            private static readonly Lazy<MethodInfo> singleRowAsync  = new Lazy<MethodInfo>(() => typeof(DynamicStoredProcedureResults).GetMethod("CreateSingleRowContinuation", BindingFlags.Instance | BindingFlags.NonPublic));

            private readonly DynamicStoredProcedureResults results;

            public Meta(Expression expression, DynamicStoredProcedureResults value)
                : base(expression, BindingRestrictions.Empty, value)
            {
                Contract.Requires(expression != null);
                Contract.Requires(value      != null);

                this.results = value;
            }

            public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args)
            {
                var instance = Expression.Convert(Expression, typeof(DynamicStoredProcedureResults));
                var restrict = BindingRestrictions.GetTypeRestriction(Expression, typeof(DynamicStoredProcedureResults));

                switch (binder.Name)
                {
                    case "ConfigureAwait":
                        return new DynamicMetaObject(Expression.Call(instance, configureAwait.Value, args[0].Expression), restrict);

                    case "GetAwaiter":
                        return new DynamicMetaObject(Expression.Call(instance, getAwaiter.Value), restrict);

                    case "GetResult":
                        return new DynamicMetaObject(instance, restrict);
                }

                return base.BindInvokeMember(binder, args);
            }

            public override DynamicMetaObject BindConvert(ConvertBinder binder)
            {
                var instance = Expression.Convert(Expression, typeof(DynamicStoredProcedureResults));
                var restrict = BindingRestrictions.GetTypeRestriction(Expression, typeof(DynamicStoredProcedureResults));
                var retType  = binder.ReturnType;
                var taskType = typeof(Task);
                Expression e = null;

                if (taskType.IsAssignableFrom(retType))
                {
                    if (results.executionMode == DynamicExecutionMode.Synchronous)
                        throw new NotSupportedException(DynamicStoredProcedure.asyncParameterDirectionError);

                    if (retType == taskType)
                    {
                        // this is just a Task (no results). Because of this, we can return a continuation
                        // from our resultTask that does nothing.
                        return new DynamicMetaObject(Expression.Constant(results.resultTask.ContinueWith(_ => { })), restrict);
                    }

                    // we are going to have to return a continuation from our task...
                    // first figure out what the result is.
                    retType = retType.GetGenericArguments().Single();
                     
                    if (retType.IsEnumeratedType())
                    {
                        // there is only one result set. Return it from a continuation.
                        e = Expression.Call(instance,
                            continueSingle.Value.MakeGenericMethod(retType.GetGenericArguments().Single()),
                            Expression.Constant(true));
                    }
                    else if (retType.FullName.StartsWith(tupleName) &&
                             retType.GetGenericArguments().All(t => t.IsEnumeratedType()))
                    {
                        e = Expression.Call(instance, continueMulti.Value.MakeGenericMethod(retType));
                    }
                    else
                    {
                        e = Expression.Call(instance,
                            singleRowAsync.Value.MakeGenericMethod(retType),
                            Expression.Constant(true));
                    }
                }
                else if (retType.IsEnumeratedType())
                {
                    // there is only one result set. Return it
                    e = Expression.Call(instance,
                        getResultsMethod.Value.MakeGenericMethod(retType.GetGenericArguments().Single()),
                        Expression.Constant(true));
                }
                else if (retType.FullName.StartsWith(tupleName) &&
                         retType.GetGenericArguments().All(t => t.IsEnumeratedType()))
                {
                    // it is a tuple of enumerables
                    e = Expression.Call(instance, getMultiResults.Value.MakeGenericMethod(retType));
                }
                else
                {
                    // there is only one result set (with one item). Return it from a continuation.
                    e = Expression.Call(instance,
                        getResultsMethod.Value.MakeGenericMethod(retType),
                        Expression.Constant(true));

                    // call Single()
                    e = Expression.Call(null, singleExtension.Value.MakeGenericMethod(retType), e);
                }

                if (e != null)
                    return new DynamicMetaObject(e, restrict);

                return base.BindConvert(binder);
            }
        }
    }
}