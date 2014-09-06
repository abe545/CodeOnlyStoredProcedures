using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CodeOnlyStoredProcedure.Dynamic
{
    internal class DynamicStoredProcedureResults : DynamicObject
    {
        private readonly Task<IEnumerable<StoredProcedureExtensions.StoredProcedureResult>> resultTask;
        private readonly IDbConnection                                                      connection;
        private readonly IDbCommand                                                         command;
        private readonly bool                                                               canBeAsync;
        private readonly CancellationToken                                                  token;

#if NET40
        private static Lazy<MethodInfo> getAwaiter = new Lazy<MethodInfo>(() =>
        {
            var assembly = Assembly.GetEntryAssembly();
            var msTasks  = assembly.GetReferencedAssemblies()
                                    .FirstOrDefault(an => an.Name == "Microsoft.Threading.Tasks");

            if (msTasks == null)
                throw new NotSupportedException("Can only await a Dynamic Stored Procedure in .NET 4.0 if you reference the Microsoft Async NuGet package.");

            var taskAssembly    = Assembly.Load(msTasks);
            var awaitExtensions = taskAssembly.GetType("AwaitExtensions");

            return awaitExtensions.GetMethods(BindingFlags.Static | BindingFlags.Public)
                                  .Where(mi => mi.IsGenericMethodDefinition)
                                  .Where(mi => mi.Name == "GetAwaiter")
                                  .Single();
        });
#endif

        public DynamicStoredProcedureResults(
            IDbConnection connection,
            string schema,
            string name,
            int timeout,
            List<Tuple<SqlParameter, Action<SqlParameter>>> parameters,
            bool canBeAsync,
            CancellationToken token)
        {
            this.canBeAsync = canBeAsync;
            this.command    = connection.CreateCommand(schema, name, timeout, out this.connection);
            this.token      = token;

            foreach (var t in parameters)
                command.Parameters.Add(t.Item1);

            this.resultTask = Task.Factory.StartNew(() => command.Execute(token),
                                                    token,
                                                    TaskCreationOptions.None,
                                                    TaskScheduler.Default)
                                           .ContinueWith(r =>
                                           {
                                               foreach (var x in parameters.Where(t => t.Item1.Direction != ParameterDirection.Input))
                                                   x.Item2(x.Item1);

                                               return r.Result;
                                           }, token);

            if (!canBeAsync)
            {
                // there are out, in/out, or return values, so we must
                // execute the stored proc before returning. Otherwise, the values won't be set.
                resultTask.Wait();
            }
        }

        object GetResults()
        {
            throw new NotImplementedException();
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            switch (binder.Name)
            {
                case "ConfigureAwait":
                    result = new DynamicStoredProcedureResultsAwaiter(this, resultTask, (bool)args[0]);
                    return true;

                case "GetAwaiter":
                    if (!canBeAsync)
                        throw new NotSupportedException(DynamicStoredProcedure.asyncParameterDirectionError);

                    var cont = resultTask.ContinueWith(_ => this);
#if NET40
                        result = getAwaiter.Value
                                           .MakeGenericMethod(GetType())
                                           .Invoke(null, new object[] { cont });

#else
                    result = new DynamicStoredProcedureResultsAwaiter(this, resultTask);
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
            var retType = binder.ReturnType;
            if (typeof(Task).IsAssignableFrom(retType))
            {
                if (!canBeAsync)
                    throw new NotSupportedException(DynamicStoredProcedure.asyncParameterDirectionError);

                if (!retType.IsGenericType)
                {
                    // this is just a Task (no results). We can just return a task that will
                    // be complete when the execution finishes.

                    result = resultTask;
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
                else if (retType.FullName.StartsWith("System.Tuple`") &&
                         retType.GetGenericArguments().All(t => t.IsEnumeratedType()))
                {
                    // if this isn't a tuple of enumerables, we can't convert it
                    result = CreateMultipleContinuation(retType.GetGenericArguments()
                                                               .Select(t => t.GetEnumeratedType())
                                                               .ToArray());
                    if (result != null)
                        return true;
                }
            }
            else if (retType.IsEnumeratedType())
            {
                // there is only one result set. Return it
                result = GetSingleResult(retType.GetGenericArguments().Single());
                return true;
            }
            else if (retType.FullName.StartsWith("System.Tuple`") &&
                     retType.GetGenericArguments().All(t => t.IsEnumeratedType()))
            {
                // if this isn't a tuple of enumerables, we can't convert it
                result = GetMultipleResults(retType.GetGenericArguments()
                                                   .Select(t => t.GetEnumeratedType())
                                                   .ToArray());
                if (result != null)
                    return true;
            }

            return base.TryConvert(binder, out result);
        }

        private object GetSingleResult(Type itemType)
        {
            return resultTask.Result
                             .Parse(new[] { itemType },
                                    Enumerable.Empty<IDataTransformer>())[itemType];
        }

        private Task<IEnumerable<T>> CreateSingleContinuation<T>()
        {
            return resultTask.ContinueWith(_ => (IEnumerable<T>)GetSingleResult(typeof(T)), token);
        }

        private object GetMultipleResults(Type[] types)
        {
            var create = typeof(Tuple).GetMethods(BindingFlags.Static | BindingFlags.Public)
                                      .FirstOrDefault(mi => mi.Name == "Create" &&
                                                            mi.GetGenericArguments().Count() == types.Length);

            if (create == null)
                return null;

            var parsed = resultTask.Result.Parse(types, Enumerable.Empty<IDataTransformer>());

            return create.MakeGenericMethod(types.Select(t => typeof(IEnumerable<>).MakeGenericType(t)).ToArray())
                         .Invoke(null, types.Select(t => parsed[t]).ToArray());
        }

        private Task CreateMultipleContinuation(Type[] types)
        {
            var create = typeof(Tuple).GetMethods(BindingFlags.Static | BindingFlags.Public)
                                      .FirstOrDefault(mi => mi.Name == "Create" &&
                                                            mi.GetGenericArguments().Count() == types.Length);

            var cont = resultTask.GetType()
                                 .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                                 .FirstOrDefault(mi => mi.IsGenericMethod && 
                                                       mi.Name == "ContinueWith" &&
                                                       mi.GetParameters().Length == 1);

            if (create == null || cont == null)
                return null;

            var parse = create.MakeGenericMethod(types.Select(t => typeof(IEnumerable<>).MakeGenericType(t)).ToArray());
            var tuple = parse.ReturnType;

            var tcs = (ITaskCompleter)Activator.CreateInstance(typeof(TaskCompleter<>).MakeGenericType(tuple));

            resultTask.ContinueWith(r =>
                {
                    if (r.Exception != null)
                        tcs.SetException(r.Exception.InnerExceptions);
                    else if (r.IsCanceled)
                        tcs.SetCanceled();
                    else
                    {
                        var res = r.Result.Parse(types, Enumerable.Empty<IDataTransformer>());
                        tcs.SetResult(parse.Invoke(null, types.Select(t => res[t]).ToArray()));
                    }
                });

            return tcs.Task;
        }

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
