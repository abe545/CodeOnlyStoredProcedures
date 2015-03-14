using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Contracts;
using System.Dynamic;
using System.Linq;
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

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            switch (binder.Name)
            {
                case "ConfigureAwait":
                    continueOnCaller = (bool)args[0];
                    result           = this;
                    return true;

                case "GetAwaiter":
                    if (executionMode == DynamicExecutionMode.Synchronous)
                        throw new NotSupportedException(DynamicStoredProcedure.asyncParameterDirectionError);

#if NET40
                    result = Activator.CreateInstance(getCompletionType.Value, this, resultTask, continueOnCaller);
#else
                    result = new DynamicStoredProcedureResultsAwaiter(this, resultTask, continueOnCaller);
#endif
                    return true;

                case "GetResult":
                    // just return this object; we'll figure out the actual results 
                    // in TryConvert
                    result = this;
                    return true;
            }

            return base.TryInvokeMember(binder, args, out result);
        }

        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            var retType  = binder.ReturnType;
            var taskType = typeof(Task);

            if (taskType.IsAssignableFrom(retType))
            {
                if (executionMode == DynamicExecutionMode.Synchronous)
                    throw new NotSupportedException(DynamicStoredProcedure.asyncParameterDirectionError);

                if (retType == taskType)
                {
                    // this is just a Task (no results). Because of this, we can return a continuation
                    // from our resultTask that does nothing.

                    result = resultTask.ContinueWith(_ => { });
                    return true;
                }

                // we are going to have to return a continuation from our task...
                // first figure out what the result is.
                retType = retType.GetGenericArguments().Single();
                if (retType.IsEnumeratedType())
                {
                    // there is only one result set. Return it from a continuation.
                    result = GetType().GetMethod("CreateSingleContinuation", BindingFlags.Instance | BindingFlags.NonPublic)
                                      .MakeGenericMethod(retType.GetGenericArguments().Single())
                                      .Invoke(this, new object[0]);
                    return true;
                }
                else if (retType.FullName.StartsWith(tupleName) &&
                         retType.GetGenericArguments().All(t => t.IsEnumeratedType()))
                {
                    // it is a tuple of enumerables
                    result = CreateMultipleContinuation(retType.GetGenericArguments());
                    return true;
                }
            }
            else if (retType.IsEnumeratedType())
            {
                // there is only one result set. Return it
                result = GetType().GetMethod("GetResults", BindingFlags.Instance | BindingFlags.NonPublic)
                                      .MakeGenericMethod(retType.GetGenericArguments().Single())
                                      .Invoke(this, new object[] { resultTask.Result });
                return true;
            }
            else if (retType.FullName.StartsWith(tupleName) &&
                     retType.GetGenericArguments().All(t => t.IsEnumeratedType()))
            {
                // it is a tuple of enumerables
                result = GetMultipleResults(resultTask.Result, retType.GetGenericArguments());
                return true;
            }

            return base.TryConvert(binder, out result);
        }

        private IEnumerable<T> GetResults<T>(IDataReader reader)
        {
            return RowFactory<T>.Create().ParseRows(reader, transformers, token);
        }

        private Task<IEnumerable<T>> CreateSingleContinuation<T>()
        {
            return resultTask.ContinueWith(r => GetResults<T>(r.Result), token);
        }

        private object GetMultipleResults(IDataReader reader, Type[] types)
        {
            Contract.Requires(types != null);

            // no need to protect this index... We have already been asked to cast to a tuple type,
            // so we know that there is a Create method with this many type parameters
            var create     = tupleCreates.Value[types.Length];
            var innerTypes = types.Select(t => t.GetEnumeratedType());

            var res = innerTypes.Select((t, i) =>
                {
                    if (i > 0)
                        reader.NextResult();

                    return GetType().GetMethod("GetResults", BindingFlags.Instance | BindingFlags.NonPublic)
                                    .MakeGenericMethod(t)
                                    .Invoke(this, new object[] { reader });
                }).ToArray();

            return create.MakeGenericMethod(types)
                         .Invoke(null, res);
        }

        private Task CreateMultipleContinuation(Type[] types)
        {
            Contract.Requires(types != null);
            Contract.Ensures (Contract.Result<Task>() != null);

            // no need to protect this index... We have already been asked to cast to a tuple type,
            // so we know that there is a Create method with this many type parameters
            var create     = tupleCreates.Value[types.Length];
            var parse      = create.MakeGenericMethod(types);
            var tuple      = parse.ReturnType;
            var innerTypes = types.Select(t => t.GetEnumeratedType()).ToArray();

            var tcs = (ITaskCompleter)Activator.CreateInstance(typeof(TaskCompleter<>).MakeGenericType(tuple));

            resultTask.ContinueWith(r =>
                {
                    if (r.Exception != null)
                        tcs.SetException(r.Exception.InnerExceptions);
                    else if (r.IsCanceled)
                        tcs.SetCanceled();
                    else
                        tcs.SetResult(GetMultipleResults(r.Result, types));
                }, token);

            return tcs.Task;
        }

        // this interface and generic implementation are annoyingly necessary. If TaskCompletionSource,
        // is used directly, we get runtime exceptions about how object doesn't have a Task property.
        private interface ITaskCompleter
        {
            Task Task { get; }
            void SetException(IEnumerable<Exception> exceptions);
            void SetCanceled();
            void SetResult(object result);
        }

        private class TaskCompleter<T> : ITaskCompleter
        {
            private readonly TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();

            public Task Task
            {
                get { return tcs.Task; }
            }

            public void SetException(IEnumerable<Exception> exceptions)
            {
                tcs.SetException(exceptions);
            }

            public void SetCanceled()
            {
                tcs.SetCanceled();
            }

            public void SetResult(object result)
            {
                tcs.SetResult((T)result);
            }
        }
    }
}
