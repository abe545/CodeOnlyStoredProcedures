using CodeOnlyStoredProcedure;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#if NET40
namespace CodeOnlyTests.Net40
#else
namespace CodeOnlyTests
#endif
{
    public partial class StoredProcedureExtensionsTests
    {
        [TestMethod]
        public void TestWithReturnValueAddsParameterAndSetsValueWhenTransferCalled()
        {
            var orig = new StoredProcedure("Test");

            int rv = 0;
            var toTest = orig.WithReturnValue(i => rv = i);

            toTest.Should().NotBeSameAs(orig, "because StoredProcedures should be immutable");
            orig.Parameters.Should().BeEmpty("because StoredProcedures should be immutable");

            toTest.Parameters.Should().ContainSingle(p => true, "because we added one Parameter")
                .Which.Should().BeOfType<ReturnValueParameter>()
                    .Which.Invoking(p => p.TransferOutputValue(42)).Invoke();

            rv.Should().Be(42, "because we invoked TransferOutputValue on the parameter.");
        }
    }
}
