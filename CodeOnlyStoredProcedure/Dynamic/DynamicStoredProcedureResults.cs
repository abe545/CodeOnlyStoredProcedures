﻿using System;
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
    internal class DynamicStoredProcedureResults : DynamicObject, IDisposable
    {
        private const string tupleName = "System.Tuple`";
        private static readonly Lazy<MethodInfo> getResultsMethod = new Lazy<MethodInfo>(() => typeof(DynamicStoredProcedureResults).GetMethod("GetResults", BindingFlags.Instance | BindingFlags.NonPublic));
        private static readonly Lazy<Dictionary<int, MethodInfo>> tupleCreates = new Lazy<Dictionary<int, MethodInfo>>(() =>
            {
                return typeof(Tuple).GetMethods(BindingFlags.Static | BindingFlags.Public)
                                    .Where(mi => mi.Name == "Create")
                                    .ToDictionary(mi => mi.GetGenericArguments().Count());
            });

        private readonly Task<IDataReader>               resultTask;
        private readonly Task                            nonQueryTask;
        private readonly IDbConnection                   connection;
        private readonly IDbCommand                      command;
        private readonly List<IStoredProcedureParameter> parameters;
        private readonly IEnumerable<IDataTransformer>   transformers;
        private readonly DynamicExecutionMode            executionMode;
        private readonly CancellationToken               token;
        private          bool                            continueOnCaller;

        public DynamicStoredProcedureResults(
            IDbConnection                   connection,
            string                          schema,
            string                          name,
            int                             timeout,
            List<IStoredProcedureParameter> parameters,
            IEnumerable<IDataTransformer>   transformers,
            DynamicExecutionMode            executionMode,
            bool                            hasResults,
            CancellationToken               token)
        {
            Contract.Requires(connection != null);
            Contract.Requires(!string.IsNullOrEmpty(schema));
            Contract.Requires(!string.IsNullOrEmpty(name));
            Contract.Requires(parameters != null);
            Contract.Requires(transformers != null);

            this.executionMode    = executionMode;
            this.command          = connection.CreateCommand(schema, name, timeout, out this.connection);
            this.parameters       = parameters;
            this.transformers     = transformers;
            this.token            = token;
            this.continueOnCaller = true;

            foreach (var p in parameters)
                command.Parameters.Add(p.CreateDbDataParameter(command));

            if (!hasResults)
            {
                if (executionMode == DynamicExecutionMode.Synchronous)
                {
                    command.ExecuteNonQuery();
                    TransferOutputParameters(parameters, token);
                    Dispose();
                }
                else
                {
#if !NET40
                    var sqlCommand = command as SqlCommand;
                    if (sqlCommand != null)
                        nonQueryTask = sqlCommand.ExecuteNonQueryAsync(token);
                    else
#endif
                        nonQueryTask = Task.Factory.StartNew(() => command.ExecuteNonQuery(),
                                                           token,
                                                           TaskCreationOptions.None,
                                                           TaskScheduler.Default);

                    nonQueryTask = nonQueryTask.ContinueWith(_ => TransferOutputParameters(parameters, token), token);
                }
            }
            else if (executionMode == DynamicExecutionMode.Synchronous)
            {
                var tcs = new TaskCompletionSource<IDataReader>();

                token.ThrowIfCancellationRequested();
                var res = command.ExecuteReader();

                // If there are any result sets, this won't actually have populated the parameters
                TransferOutputParameters(parameters, token, throwIfNonMatchingTypes: false);

                if (token.IsCancellationRequested)
                {
                    res.Dispose();
                    token.ThrowIfCancellationRequested();
                }
                else
                {
                    tcs.SetResult(res);
                    resultTask = tcs.Task;
                }
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
                    // If there are any result sets, the parameters won't actually have been set yet
                    TransferOutputParameters(parameters, token, throwIfNonMatchingTypes: false);
                    return r.Result;
                }, token);
            }
        }

        public override DynamicMetaObject GetMetaObject(Expression parameter) => new Meta(parameter, this);

        public void Dispose()
        {
            connection?.Close();
            command?.Dispose();
        }

        private IEnumerable<T> GetResults<T>(bool isSingle)
        {
            if (resultTask == null)
                throw new NotSupportedException("When calling the dynamic syntax with a NonQuery variant, no results are returned, so the value can not be cast to a result set.");

            try
            {
                return RowFactory<T>.Create(isSingle).ParseRows(resultTask.Result, transformers, token);
            }
            finally
            {
                if (isSingle)
                {
                    ReadToEnd();
                    resultTask.Result.Dispose();
                }
            }
        }

        private void ReadToEnd()
        {
            while (resultTask.Result.NextResult())
            {
                while (resultTask.Result.Read())
                    token.ThrowIfCancellationRequested();
            }

            TransferOutputParameters(parameters, token);
        }

        private Task ContinueNoResults()
        {
            if (nonQueryTask != null)
            {
                return nonQueryTask.ContinueWith(r =>
                {
                    Dispose();

                    if (r.Status == TaskStatus.Faulted)
                        throw r.Exception;
                });
            }

            return resultTask.ContinueWith(r =>
            {
                r.Result.Dispose();
                Dispose();

                if (r.Status == TaskStatus.Faulted)
                    throw r.Exception;
            });
        }

        private Task<IEnumerable<T>> CreateSingleContinuation<T>()
        {
            if (resultTask == null)
                throw new NotSupportedException("When calling the dynamic syntax with a NonQuery variant, no results are returned, so the value can not be cast to a result set.");

            return resultTask.ContinueWith(_ =>
            {
                try { return GetResults<T>(true); }
                finally { Dispose(); }
            });
        }

        private Task<T> CreateSingleRowContinuation<T>()
        {
            if (resultTask == null)
                throw new NotSupportedException("When calling the dynamic syntax with a NonQuery variant, no results are returned, so the value can not be cast to a result set.");

            return resultTask.ContinueWith(_ =>
            {
                try { return GetResults<T>(true).SingleOrDefault(); }
                finally { Dispose(); }
            }, token);
        }

        private T GetMultipleResults<T>()
        {
            Contract.Ensures(Contract.Result<T>() != null);

            if (resultTask == null)
                throw new NotSupportedException("When calling the dynamic syntax with a NonQuery variant, no results are returned, so the value can not be cast to a result set.");

            try
            {
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

                ReadToEnd();

                return (T)tupleCreates.Value[types.Length]
                                      .MakeGenericMethod(types)
                                      .Invoke(null, res);
            }
            finally
            {
                resultTask.Result.Dispose();
            }
        }

        private Task<T> CreateMultipleContinuation<T>()
        {
            Contract.Ensures(Contract.Result<Task<T>>() != null);

            if (resultTask == null)
                throw new NotSupportedException("When calling the dynamic syntax with a NonQuery variant, no results are returned, so the value can not be cast to a result set.");


            return resultTask.ContinueWith(_ =>
            {
                try { return GetMultipleResults<T>(); }
                finally { Dispose(); }
            }, token);
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

            return DynamicStoredProcedureResultsAwaiter.Create(this, nonQueryTask ?? resultTask, continueOnCaller);
        }

        private void TransferOutputParameters(List<IStoredProcedureParameter> parameters, CancellationToken token, bool throwIfNonMatchingTypes = true)
        {
            foreach (IDbDataParameter p in command.Parameters)
            {
                token.ThrowIfCancellationRequested();
                if (p.Direction != ParameterDirection.Input)
                {
                    try
                    {
                        parameters.OfType<IOutputStoredProcedureParameter>()
                                  .FirstOrDefault(sp => sp.ParameterName == p.ParameterName)
                                 ?.TransferOutputValue(p.Value);
                    }
                    catch (Exception ex)
                    {
                        if (throwIfNonMatchingTypes)
                            throw new StoredProcedureException($"Error setting the output parameter {p.ParameterName}.", ex);
                    }
                }
            }
        }

        private class Meta : DynamicMetaObject
        {
            private static readonly Lazy<MethodInfo> configureAwait  = new Lazy<MethodInfo>(() => typeof(DynamicStoredProcedureResults).GetMethod(nameof(InternalConfigureAwait),      BindingFlags.Instance | BindingFlags.NonPublic));
            private static readonly Lazy<MethodInfo> getAwaiter      = new Lazy<MethodInfo>(() => typeof(DynamicStoredProcedureResults).GetMethod(nameof(InternalGetAwaiter),          BindingFlags.Instance | BindingFlags.NonPublic));
            private static readonly Lazy<MethodInfo> continueSingle  = new Lazy<MethodInfo>(() => typeof(DynamicStoredProcedureResults).GetMethod(nameof(CreateSingleContinuation),    BindingFlags.Instance | BindingFlags.NonPublic));
            private static readonly Lazy<MethodInfo> continueMulti   = new Lazy<MethodInfo>(() => typeof(DynamicStoredProcedureResults).GetMethod(nameof(CreateMultipleContinuation),  BindingFlags.Instance | BindingFlags.NonPublic));
            private static readonly Lazy<MethodInfo> getMultiResults = new Lazy<MethodInfo>(() => typeof(DynamicStoredProcedureResults).GetMethod(nameof(GetMultipleResults),          BindingFlags.Instance | BindingFlags.NonPublic));
            private static readonly Lazy<MethodInfo> singleRowAsync  = new Lazy<MethodInfo>(() => typeof(DynamicStoredProcedureResults).GetMethod(nameof(CreateSingleRowContinuation), BindingFlags.Instance | BindingFlags.NonPublic));
            private static readonly Lazy<MethodInfo> continueNoRes   = new Lazy<MethodInfo>(() => typeof(DynamicStoredProcedureResults).GetMethod(nameof(ContinueNoResults),           BindingFlags.Instance | BindingFlags.NonPublic));
            private static readonly Lazy<MethodInfo> dispose         = new Lazy<MethodInfo>(() => typeof(IDisposable)                  .GetMethod(nameof(Dispose),                     BindingFlags.Instance | BindingFlags.Public));
            private static readonly Lazy<MethodInfo> singleExtension = new Lazy<MethodInfo>(() => typeof(Enumerable).GetMethods().Where(m => m.Name == nameof(Enumerable.SingleOrDefault) && m.GetParameters().Length == 1).Single());

            private readonly DynamicStoredProcedureResults results;
            private readonly BindingRestrictions restrict;

            public Meta(Expression expression, DynamicStoredProcedureResults value)
                : base(expression, BindingRestrictions.GetInstanceRestriction(expression, value), value)
            {
                Contract.Requires(expression != null);
                Contract.Requires(value      != null);

                results = value;
                restrict = BindingRestrictions.GetInstanceRestriction(expression, results);
            }

            public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args)
            {
                var instance = Expression.Convert(Expression, typeof(DynamicStoredProcedureResults));
                
                switch (binder.Name)
                {
                    case "ConfigureAwait":
                        return new DynamicMetaObject(Expression.Call(instance, configureAwait.Value, args[0].Expression), restrict);

                    case "GetAwaiter":
                        return new DynamicMetaObject(Expression.Call(instance, getAwaiter.Value), restrict);

                    case "GetResult":
                        return new DynamicMetaObject(instance, restrict);

                    case "Dispose":
                        return new DynamicMetaObject(
                            Expression.Block(Expression.Call(instance, dispose.Value), instance), restrict);
                }

                return base.BindInvokeMember(binder, args);
            }

            public override DynamicMetaObject BindConvert(ConvertBinder binder)
            {
                var instance = Expression.Convert(Expression, typeof(DynamicStoredProcedureResults));
                var restrict = BindingRestrictions.GetInstanceRestriction(Expression, results);
                var retType  = binder.ReturnType;
                var taskType = typeof(Task);
                Expression e;

                if (taskType.IsAssignableFrom(retType))
                {
                    if (results.executionMode == DynamicExecutionMode.Synchronous)
                        throw new NotSupportedException(DynamicStoredProcedure.asyncParameterDirectionError);

                    // this is just a Task (no results)
                    if (retType == taskType)
                        return new DynamicMetaObject(Expression.Call(instance, continueNoRes.Value), restrict);

                    // we are going to have to return a continuation from our task...
                    // first figure out what the result is.
                    retType = retType.GetGenericArguments().Single();

                    if (retType.IsEnumeratedType())
                    {
                        // there is only one result set. Return it from a continuation.
                        e = Expression.Call(instance,
                            continueSingle.Value.MakeGenericMethod(retType.GetGenericArguments().Single()));
                    }
                    else if (retType.FullName.StartsWith(tupleName) &&
                             retType.GetGenericArguments().All(t => t.IsEnumeratedType()))
                    {
                        // multiple result sets
                        e = Expression.Call(instance, continueMulti.Value.MakeGenericMethod(retType));
                    }
                    else
                    {
                        // a single row
                        e = Expression.Call(instance, singleRowAsync.Value.MakeGenericMethod(retType));
                    }
                }
                else
                {
                    // synchronous results
                    if (retType.IsEnumeratedType())
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
                        // there is only one result set (with one item). Return it
                        e = Expression.Call(instance,
                            getResultsMethod.Value.MakeGenericMethod(retType),
                            Expression.Constant(true));

                        // call Single()
                        e = Expression.Call(null, singleExtension.Value.MakeGenericMethod(retType), e);
                    }

                    // make sure to dispose the DynamicStoredProcedureResults
                    var res = Expression.Variable(retType);
                    e = Expression.Block(retType,
                        new[] { res },
                        Expression.Assign(res, e),
                        Expression.Call(Expression.Constant(results), dispose.Value),
                        res);
                }

                return new DynamicMetaObject(e, restrict);
            }
        }
    }
}