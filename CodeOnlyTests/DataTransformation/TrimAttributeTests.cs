using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CodeOnlyStoredProcedure.DataTransformation;

#if NET40
namespace CodeOnlyTests.Net40.DataTransformation
#else
namespace CodeOnlyTests.DataTransformation
#endif
{
    [TestClass]
    public class TrimAttributeTests
    {
        private TrimAttribute toTest;

        [TestInitialize]
        public void Initialize()
        {
            toTest = new TrimAttribute();
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void TestTransformThrowsOnInputThatIsNotAString()
        {
            toTest.Transform(false, typeof(string), false);
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void TestTransformThrowsOnTargetTypeThatIsNotAString()
        {
            toTest.Transform("false", typeof(bool), false);
        }

        [TestMethod]
        public void TestTransformReturnsWhitespaceForNullInput()
        {
            var res = toTest.Transform(null, typeof(string), false);

            Assert.AreEqual(string.Empty, res);
        }

        [TestMethod]
        public void TestTransformReturnsEmptyForWhitespace()
        {
            var res = toTest.Transform("     ", typeof(string), false);

            Assert.AreEqual(string.Empty, res);
        }

        [TestMethod]
        public void TestTransformReturnsValueWhenNoWhitespace()
        {
            var res = toTest.Transform("Foo", typeof(string), false);

            Assert.AreEqual("Foo", res);
        }

        [TestMethod]
        public void TestTransformReturnsValueWithoutTrailingWhitespace()
        {
            var res = toTest.Transform("Bar     ", typeof(string), false);

            Assert.AreEqual("Bar", res);
        }

        [TestMethod]
        public void TestTransformReturnsValueWithoutLeadingWhitespace()
        {
            var res = toTest.Transform("     Bar", typeof(string), false);

            Assert.AreEqual("Bar", res);
        }
    }
}
