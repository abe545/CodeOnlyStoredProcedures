using System;
using System.Collections.Generic;
using System.Data;
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

#if NET40
        /// <summary>
        /// The type to use as an awaiter. Because the INotifyCompletion interface is available
        /// to .NET 4.0 with the Microsoft Async package, we can support awaiting a dynamic
        /// stored procedure if we implement that interface. I don't want to require async
        /// for CodeOnlyStoerdProcedure in .NET 4.0, so this is a decent compromise. All the 
        /// functionality is in the DynamicStoredProcedureResultsAwaiter, so all this dynamic
        /// type is doing is calling the implementation.
        /// </summary>
        private static Lazy<Type> getCompletionType = new Lazy<Type>(() =>
            {
                var baseType  = typeof(DynamicStoredProcedureResultsAwaiter);
                var ifaceType = Type.GetType("System.Runtime.CompilerServices.INotifyCompletion, System.Threading.Tasks");

                if (ifaceType == null)
                    throw new NotSupportedException("Could not find the interface required for using await in .NET 4. Please make sure that System.Threading.Tasks is loaded in your process before trying to await a dynamic stored procedure.");

                var ab = AppDomain.CurrentDomain.DefineDynamicAssembly(
                    new AssemblyName("CodeOnlyStoredProcedures.Net40Async"),
                    AssemblyBuilderAccess.Run);

                var mod  = ab.DefineDynamicModule("CodeOnlyStoredProcedures.Net40Async");
                var type = mod.DefineType("DynamicStoredProcedureResultsAwaiterImpl", 
                                          TypeAttributes.Class | TypeAttributes.NotPublic,
                                          baseType,
                                          new[] { ifaceType });

                var comp = type.DefineMethod("OnCompleted", MethodAttributes.Public, typeof(void), new[] { typeof(Action) });
                var il   = comp.GetILGenerator();

                il.Emit(OpCodes.Ldarg_0); // push "this"
                il.Emit(OpCodes.Ldarg_1); // push continuation Action
                il.Emit(OpCodes.Call, baseType.GetMethod("OnCompleted"));
                il.Emit(OpCodes.Ret);

                var ctorArgs = new[] 
                               {
                                   typeof(DynamicStoredProcedureResults),
                                   typeof(Task),
                                   typeof(bool)
                               };

                var ctor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, ctorArgs);
                il       = ctor.GetILGenerator();
                
                il.Emit(OpCodes.Ldarg_0); // push "this"
                il.Emit(OpCodes.Ldarg_1); // push the rest of the parameters
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Ldarg_3);
                il.Emit(OpCodes.Call, baseType.GetConstructor(ctorArgs));
                il.Emit(OpCodes.Ret);

                return type.CreateType();
            });
#endif

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

                try
                {
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

                    tcs.SetResult(res);
                }
                catch (AggregateException ag)
                {
                    tcs.SetException(ag.Flatten().InnerExceptions);
                }
                catch (TaskCanceledException)
                {
                    tcs.SetCanceled();
                }
                catch (OperationCanceledException)
                {
                    tcs.SetCanceled();
                }

                this.resultTask = tcs.Task;
            }
            else
            {
                this.resultTask = Task.Factory.StartNew(() => command.ExecuteReader(),
                                                        token,
                                                        TaskCreationOptions.None,
                                                        TaskScheduler.Default)
                                               .ContinueWith(r =>
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

        private IEnumerable<T> GetResults<T>()
        {
            var rdr = resultTask.Result;
            return RowFactory<T>.Create().ParseRows(rdr, transformers, token);
        }

        private Task<IEnumerable<T>> CreateSingleContinuation<T>()
        {
            return resultTask.ContinueWith(_ => GetResults<T>(), token);
        }

        private T GetMultipleResults<T>()
        {
            Contract.Ensures(Contract.Result<T>() != null);
            
            // no need to protect this index... We have already been asked to cast to a tuple type,
            // so we know that there is a Create method with this many type parameters
            var types = typeof(T).GetGenericArguments();

            // no need to protect this index... We have already been asked to cast to a tuple type,
            // so we know that there is a Create method with this many type parameters
            var create     = tupleCreates.Value[types.Length];
            var innerTypes = types.Select(t => t.GetEnumeratedType());

            var res = innerTypes.Select((t, i) =>
                {
                    if (i > 0)
                        resultTask.Result.NextResult();

                    return GetType().GetMethod("GetResults", BindingFlags.Instance | BindingFlags.NonPublic)
                                    .MakeGenericMethod(t)
                                    .Invoke(this, new object[0]);
                }).ToArray();

            return (T)create.MakeGenericMethod(types)
                            .Invoke(null, res);
        }

        private Task<T> CreateMultipleContinuation<T>()
        {
            Contract.Ensures(Contract.Result<Task<T>>() != null);

            var tcs = new TaskCompletionSource<T>();

            resultTask.ContinueWith(r =>
                {
                    if (r.Exception != null)
                        tcs.SetException(r.Exception.InnerExceptions);
                    else if (r.IsCanceled)
                        tcs.SetCanceled();
                    else
                        tcs.SetResult(GetMultipleResults<T>());
                }, token);

            return tcs.Task;
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
#if NET40
            return Activator.CreateInstance(getCompletionType.Value, this, resultTask, continueOnCaller);
#else
            return new DynamicStoredProcedureResultsAwaiter(this, resultTask, continueOnCaller);
#endif
        }

        private class Meta : DynamicMetaObject
        {
            private  DynamicStoredProcedureResults results;

            public Meta(Expression expression, DynamicStoredProcedureResults value)
                : base(expression, BindingRestrictions.Empty, value)
            {
                this.results = value;
            }

            public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args)
            {
                var instance = Expression.Convert(Expression, typeof(DynamicStoredProcedureResults));
                var restrict = BindingRestrictions.GetTypeRestriction(Expression, typeof(DynamicStoredProcedureResults));

                switch (binder.Name)
                {
                    case "ConfigureAwait":
                        return new DynamicMetaObject(
                            Expression.Call(instance,
                                            typeof(DynamicStoredProcedureResults).GetMethod("InternalConfigureAwait", BindingFlags.Instance | BindingFlags.NonPublic),
                                            args[0].Expression),
                            restrict);

                    case "GetAwaiter":
                        return new DynamicMetaObject(
                            Expression.Call(instance, typeof(DynamicStoredProcedureResults).GetMethod("InternalGetAwaiter", BindingFlags.Instance | BindingFlags.NonPublic)),
                            restrict);

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

                        var method = typeof(DynamicStoredProcedureResults)
                            .GetMethod("CreateSingleContinuation", BindingFlags.Instance | BindingFlags.NonPublic)
                            .MakeGenericMethod(retType.GetGenericArguments().Single());

                        e = Expression.Call(instance, method);
                    }
                    else if (retType.FullName.StartsWith(tupleName) &&
                             retType.GetGenericArguments().All(t => t.IsEnumeratedType()))
                    {
                        var method = typeof(DynamicStoredProcedureResults)
                            .GetMethod("CreateMultipleContinuation", BindingFlags.Instance | BindingFlags.NonPublic)
                            .MakeGenericMethod(retType);

                        e = Expression.Call(instance, method);
                    }
                }
                else if (retType.IsEnumeratedType())
                {
                    // there is only one result set. Return it
                    var method = typeof(DynamicStoredProcedureResults)
                        .GetMethod("GetResults", BindingFlags.Instance | BindingFlags.NonPublic)
                        .MakeGenericMethod(retType.GetGenericArguments().Single());

                    e = Expression.Call(instance, method);
                }
                else if (retType.FullName.StartsWith(tupleName) &&
                         retType.GetGenericArguments().All(t => t.IsEnumeratedType()))
                {
                    // it is a tuple of enumerables
                    var method = typeof(DynamicStoredProcedureResults)
                        .GetMethod("GetMultipleResults", BindingFlags.Instance | BindingFlags.NonPublic)
                        .MakeGenericMethod(retType);

                    e = Expression.Call(instance, method);
                }

                if (e != null)
                    return new DynamicMetaObject(e, restrict);

                return base.BindConvert(binder);
            }
        }
    }
}