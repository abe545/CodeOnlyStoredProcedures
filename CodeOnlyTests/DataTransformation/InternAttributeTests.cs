using CodeOnlyStoredProcedure.DataTransformation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Reflection;

#if NET40
namespace CodeOnlyTests.Net40.DataTransformation
#else
namespace CodeOnlyTests.DataTransformation
#endif
{
    [TestClass]
    public class InternAttributeTests
    {
        private InternAttribute toTest;

        [TestInitialize]
        public void Initialize()
        {
            toTest = new InternAttribute();
        }

        [TestMethod]
        public void TestOrderIndexIsMaxValue()
        {
            Assert.AreEqual(Int32.MaxValue, toTest.Order);
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void TestTransformThrowsWhenInputIsNotAString()
        {
            toTest.Transform(false, typeof(string), false);
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void TestTransformThrowsWhenTargetTypeIsNotString()
        {
            toTest.Transform("foo", typeof(bool), false);
        }

        [TestMethod]
        public void TestTransformReturnsNullForNull()
        {
            Assert.IsNull(toTest.Transform(null, typeof(string), false));
        }

        [TestMethod]
        public void TestTransformReturnsEmptyForEmpty()
        {
            Assert.ReferenceEquals(string.Empty, toTest.Transform(string.Empty, typeof(string), false));
        }

        [TestMethod]
        public void TestTransformReturnsStringAsInterned()
        {
            // can't use a string literal for the test, because the compiler interns
            // them automatically
            // we also have to use the executing assembly, so both the .net 4.0 & 4.5
            // assemblies can run the test. Otherwise, if one of them interns the
            // string, the other will fail.
            var str = Assembly.GetExecutingAssembly().FullName + GetType() + DateTime.Now;
            Assert.IsNull(string.IsInterned(str)); // just make sure

            var res = (string)toTest.Transform(str, typeof(string), false);
            Assert.ReferenceEquals(string.IsInterned(res), res);
        }
    }
}
