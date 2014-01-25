using CodeOnlyStoredProcedure.DataTransformation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Reflection;

#if NET40
namespace CodeOnlyTests.Net40.DataTransformation
#else
namespace CodeOnlyTests.DataTransformation
#endif
{
    [TestClass]
    public class InternAllStringsTransformerTests
    {
        private InternAllStringsTransformer toTest;

        [TestInitialize]
        public void Initialize()
        {
            toTest = new InternAllStringsTransformer();
        }

        [TestMethod]
        public void TestCanTransformReturnsFalseForNonStringTargetType()
        {
            Assert.IsFalse(toTest.CanTransform("foo", typeof(int), Enumerable.Empty<Attribute>()));
        }

        [TestMethod]
        public void TestCanTransformReturnsFalseForNonStringInput()
        {
            Assert.IsFalse(toTest.CanTransform(false, typeof(string), Enumerable.Empty<Attribute>()));
        }

        [TestMethod]
        public void TestCanTransformReturnsFalseForNullInput()
        {
            Assert.IsFalse(toTest.CanTransform(null, typeof(string), Enumerable.Empty<Attribute>()));
        }

        [TestMethod]
        public void TestCanTransformReturnsTrueForString()
        {
            Assert.IsTrue(toTest.CanTransform("foo", typeof(string), Enumerable.Empty<Attribute>()));
        }

        [TestMethod]
        public void TestTransformReturnsEmptyForEmpty()
        {
            var res = toTest.Transform(string.Empty, typeof(string), Enumerable.Empty<Attribute>());
            Assert.ReferenceEquals(string.Empty, res);
        }

        [TestMethod]
        public void TestTransformInternsString()
        {
            // can't use a string literal for the test, because the compiler interns
            // them automatically
            // we also have to use the executing assembly, so both the .net 4.0 & 4.5
            // assemblies can run the test. Otherwise, if one of them interns the
            // string, the other will fail.
            var str = Assembly.GetExecutingAssembly().FullName + GetType() + DateTime.Now;
            Assert.IsNull(string.IsInterned(str)); // just make sure

            var res = (string)toTest.Transform(str, typeof(string), Enumerable.Empty<Attribute>());
            Assert.ReferenceEquals(string.IsInterned(res), res);
        }
    }
}
