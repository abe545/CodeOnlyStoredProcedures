using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CodeOnlyStoredProcedure.DataTransformation;
using System.Linq;

#if NET40
namespace CodeOnlyTests.Net40.DataTransformation
#else
namespace CodeOnlyTests.DataTransformation
#endif
{
    [TestClass]
    public class TrimAllStringsTransformerTests
    {
        TrimAllStringsTransformer toTest;

        [TestInitialize]
        public void Initialize()
        {
            toTest = new TrimAllStringsTransformer();
        }

        [TestMethod]
        public void TestCanTransformReturnsFalseWhenValueIsNotAString()
        {
            Assert.IsFalse(toTest.CanTransform(false, typeof(string), Enumerable.Empty<Attribute>()));
        }

        [TestMethod]
        public void TestCanTransformReturnsFalseWhenTargetTypeIsNotAString()
        {
            Assert.IsFalse(toTest.CanTransform("false", typeof(bool), Enumerable.Empty<Attribute>()));
        }

        [TestMethod]
        public void TestCanTransformReturnsTrueForNullValue()
        {
            Assert.IsTrue(toTest.CanTransform(null, typeof(string), Enumerable.Empty<Attribute>()));
        }

        [TestMethod]
        public void TestCanTransformReturnsTrueForStringValue()
        {
            Assert.IsTrue(toTest.CanTransform("foo", typeof(string), Enumerable.Empty<Attribute>()));
        }

        [TestMethod]
        public void TestTransformReturnsWhitespaceForNullInput()
        {
            var res = toTest.Transform(null, typeof(string), Enumerable.Empty<Attribute>());

            Assert.AreEqual(string.Empty, res);
        }

        [TestMethod]
        public void TestTransformReturnsEmptyForWhitespace()
        {
            var res = toTest.Transform("     ", typeof(string), Enumerable.Empty<Attribute>());

            Assert.AreEqual(string.Empty, res);
        }

        [TestMethod]
        public void TestTransformReturnsValueWhenNoWhitespace()
        {
            var res = toTest.Transform("Foo", typeof(string), Enumerable.Empty<Attribute>());

            Assert.AreEqual("Foo", res);
        }

        [TestMethod]
        public void TestTransformReturnsValueWithoutTrailingWhitespace()
        {
            var res = toTest.Transform("Bar     ", typeof(string), Enumerable.Empty<Attribute>());

            Assert.AreEqual("Bar", res);
        }

        [TestMethod]
        public void TestTransformReturnsValueWithoutLeadingWhitespace()
        {
            var res = toTest.Transform("     Bar", typeof(string), Enumerable.Empty<Attribute>());

            Assert.AreEqual("Bar", res);
        }
    }
}
