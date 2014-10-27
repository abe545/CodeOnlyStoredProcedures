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
        public void TestWithDataTransformerStoresTransformer()
        {
            var orig = new StoredProcedure("Test");
            var xform = Mock.Of<IDataTransformer>();

            var toTest = orig.WithDataTransformer(xform);

            toTest.Should().NotBeSameAs(orig, "because StoredProcedures should be immutable");
            orig.DataTransformers.Should().BeEmpty("because StoredProcedures should be immutable");

            toTest.DataTransformers.Should().ContainSingle(dt => ReferenceEquals(dt, xform));
        }

        [TestMethod]
        public void TestWithDataTransformerAddsTransformersInOrder()
        {
            var orig = new StoredProcedure("Test");
            var x1 = Mock.Of<IDataTransformer>();
            var x2 = Mock.Of<IDataTransformer>();

            var toTest = orig.WithDataTransformer(x1).WithDataTransformer(x2);

            toTest.Should().NotBeSameAs(orig, "because StoredProcedures should be immutable");
            orig.DataTransformers.Should().BeEmpty("because StoredProcedures should be immutable");

            toTest.DataTransformers.Should().ContainInOrder(x1, x2).And.HaveCount(2);
        }
    }
}
