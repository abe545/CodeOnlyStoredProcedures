using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CodeOnlyStoredProcedure.DataTransformation;

namespace CodeOnlyTests.DataTransformation
{
    [TestClass]
    public class TrimAttributeTests
    {
        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void TestTrimAttributeThrowsOnInputThatIsNotAString()
        {
            var toTest = new TrimAttribute();

            toTest.Transform(false, typeof(string));
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void TestTrimAttributeThrowsOnTargetTypeThatIsNotAString()
        {
            var toTest = new TrimAttribute();

            toTest.Transform("false", typeof(bool));
        }

        [TestMethod]
        public void TestTransformReturnsWhitespaceForNullInput()
        {
            var toTest = new TrimAttribute();

            var res = toTest.Transform(null, typeof(string));

            Assert.AreEqual(string.Empty, res);
        }

        [TestMethod]
        public void TestTransformReturnsEmptyForWhitespace()
        {
            var toTest = new TrimAttribute();

            var res = toTest.Transform("     ", typeof(string));

            Assert.AreEqual(string.Empty, res);
        }

        [TestMethod]
        public void TestTransformReturnsValueWhenNoWhitespace()
        {
            var toTest = new TrimAttribute();

            var res = toTest.Transform("Foo", typeof(string));

            Assert.AreEqual("Foo", res);
        }

        [TestMethod]
        public void TestTransformReturnsValueWithoutTrailingWhitespace()
        {
            var toTest = new TrimAttribute();

            var res = toTest.Transform("Bar     ", typeof(string));

            Assert.AreEqual("Bar", res);
        }

        [TestMethod]
        public void TestTransformReturnsValueWithoutLeadingWhitespace()
        {
            var toTest = new TrimAttribute();

            var res = toTest.Transform("     Bar", typeof(string));

            Assert.AreEqual("Bar", res);
        }
    }
}
