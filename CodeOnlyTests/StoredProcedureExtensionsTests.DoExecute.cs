using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using CodeOnlyStoredProcedure;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

#if NET40
namespace CodeOnlyTests.Net40
#else
namespace CodeOnlyTests
#endif
{
    public partial class StoredProcedureExtensionsTests
    {
        [TestMethod]
        public void TestCreateataReaderCancelsWhenCanceledBeforeExecuting()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();

            var command = new Mock<IDbCommand>();
            command.Setup(d => d.ExecuteReader())
                   .Throws(new Exception("ExecuteReader called after token was canceled"));
            command.SetupAllProperties();

            command.Object.Invoking(cmd => cmd.DoExecute(c => c.ExecuteReader(), cts.Token))
                          .ShouldThrow<OperationCanceledException>("because the token is already canceled.");
            
            command.Verify(d => d.ExecuteReader(), Times.Never);
        }

        [TestMethod]
        public void TestDoExecuteCancelsCommandWhenTokenCanceled()
        {
            var sema    = new SemaphoreSlim(0, 1);
            var command = new Mock<IDbCommand>();

            command.SetupAllProperties();
            command.Setup(d => d.ExecuteReader())
                   .Callback(() =>
                   {
                       sema.Release();
                       do
                       {
                           Thread.Sleep(100);
                       } while (sema.Wait(100));
                   })
                   .Returns(() => new Mock<IDataReader>().Object);
            command.Setup(d => d.Cancel())
                   .Verifiable();

            command.Object.CommandTimeout = 30;

            var cts = new CancellationTokenSource();

            var toTest = Task.Factory.StartNew(() => command.Object.DoExecute(c => c.ExecuteReader(), cts.Token), cts.Token);
            bool isCancelled = false;

            var continuation = 
                toTest.ContinueWith(t => isCancelled = true,
                                    TaskContinuationOptions.OnlyOnCanceled);

            sema.Wait();
            cts.Cancel();

            continuation.Wait();
            sema.Release();
            command.Verify(d => d.Cancel(), Times.Once);
            isCancelled.Should().BeTrue("because the operation was cancelled.");
        }

        [TestMethod]
        public void TestDoExecuteThrowsWhenExecuteReaderThrows()
        {
            var command = new Mock<IDbCommand>();
            command.SetupAllProperties();
            command.Setup(d => d.ExecuteReader())
                   .Throws(new Exception("Test Exception"));

            command.Object.Invoking(cmd => cmd.DoExecute(c => c.ExecuteReader(), CancellationToken.None))
                .ShouldThrow<Exception>("because ExecuteReader throws.").WithMessage("Test Exception");
        }

        [TestMethod]
        public void TestDoExecuteAbortsCommandAfterTimeoutPassed()
        {
            var cmd = new Mock<IDbCommand>();
            cmd.Setup(c => c.ExecuteReader())
               .Callback(() => Thread.Sleep(2000))
               .Returns(() => new Mock<IDataReader>().Object);
            cmd.SetupAllProperties();
            cmd.Object.CommandTimeout = 1;
            
            cmd.Object.Invoking(cd => cd.DoExecute(c => c.ExecuteReader(), CancellationToken.None))
                .ShouldThrow<TimeoutException>("because ExecuteReader does not finish in time.");

            cmd.Verify(c => c.Cancel(), Times.Once());
        }
    }
}
