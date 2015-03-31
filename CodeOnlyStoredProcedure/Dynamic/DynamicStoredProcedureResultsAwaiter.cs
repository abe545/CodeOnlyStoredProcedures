using System;
using System.Diagnostics.Contracts;
using System.Dynamic;
using System.Threading;
using System.Threading.Tasks;

namespace CodeOnlyStoredProcedure.Dynamic
{
    internal class DynamicStoredProcedureResultsAwaiter : DynamicObject
#if !NET40
        , System.Runtime.CompilerServices.INotifyCompletion
#endif
    {
        private readonly DynamicStoredProcedureResults results;
        private readonly Task                          toWait;
        private readonly bool                          continueOnCaller;
        
        public DynamicStoredProcedureResultsAwaiter(
            DynamicStoredProcedureResults results, 
            Task toWait,
            bool continueOnCaller)
        {
            Contract.Requires(results != null);
            Contract.Requires(toWait  != null);

            this.results          = results;
            this.toWait           = toWait;
            this.continueOnCaller = continueOnCaller;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (binder.Name == "IsCompleted")
            {
                result = toWait.IsCompleted;
                return true;
            }

            return base.TryGetMember(binder, out result);
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            if (binder.Name == "GetResult")
            {
                result = results;
                return true;
            }

            return base.TryInvokeMember(binder, args, out result);
        }

        public virtual void OnCompleted(Action continuation)
        {
            var sc            = continueOnCaller ? SynchronizationContext.Current : null;
            var taskScheduler = continueOnCaller ? TaskScheduler         .Current : TaskScheduler.Default;

            if (sc != null && sc.GetType() != typeof(SynchronizationContext))
            {
                toWait.ContinueWith(_ =>
                {
                    try
                    {
                        sc.Post(delegate(object state)
                        {
                            ((Action)state).Invoke();
                        }, continuation);
                    }
                    catch (Exception exception)
                    {
                        exception.ThrowAsync(null);
                    }
                }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }
            else if (toWait.IsCompleted)
            {
                Task.Factory.StartNew(s =>
                {
                    ((Action)s).Invoke();
                }, continuation, CancellationToken.None, TaskCreationOptions.None, taskScheduler);
            }
            else if (taskScheduler != TaskScheduler.Default)
            {
                toWait.ContinueWith(_ =>
                {
                    continuation.RunNoException();
                }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, taskScheduler);
            }
            else
            {
                toWait.ContinueWith(_ =>
                {
                    if ((SynchronizationContext.Current == null ||
                         SynchronizationContext.Current.GetType() == typeof(SynchronizationContext)) &&
                        TaskScheduler.Current == TaskScheduler.Default)
                    {
                        continuation.RunNoException();
                    }
                    else
                    {
                        Task.Factory.StartNew(s =>
                        {
                            ((Action)s).RunNoException();
                        }, continuation, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
                    }
                }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }
        }
    }
}
