using System;
using System.Diagnostics.Contracts;
using System.Threading;

namespace CodeOnlyStoredProcedure.Dynamic
{
    internal static class Extensions
    {
        public static void RunNoException(this Action action)
        {
            Contract.Requires(action != null);

            try
            {
                action();
            }
            catch (Exception ex)
            {
                ThrowAsync(ex, null);
            }
        }

        public static void ThrowAsync(this Exception exception, SynchronizationContext targetContext)
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
