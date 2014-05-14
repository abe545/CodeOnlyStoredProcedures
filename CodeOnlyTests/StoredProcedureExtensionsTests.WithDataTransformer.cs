using System.Linq;
using CodeOnlyStoredProcedure;
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
            var xform = new Mock<IDataTransformer>().Object;

            var toTest = orig.WithDataTransformer(xform);

            Assert.IsFalse(ReferenceEquals(orig, toTest));
            Assert.AreEqual(xform, toTest.DataTransformers.Single());
        }

        [TestMethod]
        public void TestWithDataTransformerAddsTransformersInOrder()
        {
            var orig = new StoredProcedure("Test");
            var x1 = new Mock<IDataTransformer>().Object;
            var x2 = new Mock<IDataTransformer>().Object;

            var toTest = orig.WithDataTransformer(x1).WithDataTransformer(x2);

            Assert.AreEqual(2, toTest.DataTransformers.Count());
            Assert.AreEqual(x1, toTest.DataTransformers.First());
            Assert.AreEqual(x2, toTest.DataTransformers.Last());
        }
    }
}
