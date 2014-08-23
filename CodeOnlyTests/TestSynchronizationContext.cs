using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

#if NET40
namespace CodeOnlyTests.Net40
#else
namespace CodeOnlyTests
#endif
{
    public sealed class TestSynchronizationContext : SynchronizationContext
    {
        private readonly Thread worker;
        private readonly BlockingCollection<KeyValuePair<SendOrPostCallback,object>> queue =
                new BlockingCollection<KeyValuePair<SendOrPostCallback, object>>();

        public Thread Worker { get { return worker; } }

        public TestSynchronizationContext()
        {
            worker = new Thread(RunOnCurrentThread);
            worker.Start();
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            queue.Add(new KeyValuePair<SendOrPostCallback, object>(d, state));
        }

        private void RunOnCurrentThread(object _)
        {
            KeyValuePair<SendOrPostCallback, object> workItem;
            while (queue.TryTake(out workItem, Timeout.Infinite))
                workItem.Key(workItem.Value);
        }
    }
}
