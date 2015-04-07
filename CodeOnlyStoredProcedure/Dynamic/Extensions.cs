using System;
using System.Diagnostics.Contracts;
using System.Threading;

namespace CodeOnlyStoredProcedure.Dynamic
{
    internal static class Extensions
    {
        public static void RunNoException(this Action action, SynchronizationContext targetContext = null)
        {
            Contract.Requires(action != null);

            try
            {
                if (targetContext == null)
                    action();
                else
                    targetContext.Post(o => ((Action)o)(), action);
            }
            catch (Exception ex)
            {
                ThrowAsync(ex, targetContext);
            }
        }

        private static void ThrowAsync(this Exception exception, SynchronizationContext targetContext)
        {
            Contract.Requires(exception != null);

            if (targetContext != null)
            {
                try
                {
                    targetContext.Post(e =>
                    {
                        throw (Exception)e;
                    }, exception);
                    return;
                }
                catch (Exception ex)
                {
                    exception = new AggregateException(new Exception[]
                    {
                        exception,
                        ex
                    });
                }
            }
            ThreadPool.QueueUserWorkItem(e =>
            {
                throw (Exception)e;
            }, exception);
        }
    }
}
