using System;
using System.Threading;
using System.Threading.Tasks;

namespace CodeOnlyStoredProcedure.Dynamic
{
    internal class DynamicStoredProcedureResultsAwaiter
#if !NET40
        : System.Runtime.CompilerServices.ICriticalNotifyCompletion
#endif
    {
        private readonly DynamicStoredProcedureResults results;
        private readonly Task                          toWait;
        private readonly bool                          continueOnCaller;

        public bool IsCompleted { get { return toWait.IsCompleted; } }

        public DynamicStoredProcedureResultsAwaiter(
            DynamicStoredProcedureResults results, 
            Task toWait,
            bool continueOnCaller = true)
        {
            this.results          = results;
            this.toWait           = toWait;
            this.continueOnCaller = continueOnCaller;
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            if (continuation == null)
                throw new ArgumentNullException("continuation");

            OnCompleted(continuation);
        }

        public dynamic GetResult()
        {
            return results;
        }

        public void OnCompleted(Action continuation)
        {
            if (continuation == null)
                throw new ArgumentNullException("continuation");

            var sc = continueOnCaller ? SynchronizationContext.Current : null;
            var taskScheduler = continueOnCaller ? TaskScheduler.Current : TaskScheduler.Default;

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
