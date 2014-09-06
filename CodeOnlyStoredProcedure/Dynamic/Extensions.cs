using System;
using System.Threading;

namespace CodeOnlyStoredProcedure.Dynamic
{
    internal static class Extensions
    {
        public static void RunNoException(this Action action)
        {
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
            if (targetContext != null)
            {
                try
                {
                    targetContext.Post(s =>
                    {
                        throw PrepareExceptionForRethrow((Exception)s);
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
            ThreadPool.QueueUserWorkItem(s =>
            {
                throw PrepareExceptionForRethrow((Exception)s);
            }, exception);
        }

        private static Exception PrepareExceptionForRethrow(Exception exception)
        {
            // this apparently copies the exception's stack trace so its stack trace isn't overwritten.
            return exception;
        }
    }
}
