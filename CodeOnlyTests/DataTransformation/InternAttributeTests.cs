using CodeOnlyStoredProcedure.DataTransformation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Reflection;

namespace CodeOnlyTests.DataTransformation
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
            toTest.Transform(false, typeof(string));
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void TestTransformThrowsWhenTargetTypeIsNotString()
        {
            toTest.Transform("foo", typeof(bool));
        }

        [TestMethod]
        public void TestTransformReturnsNullForNull()
        {
            Assert.IsNull(toTest.Transform(null, typeof(string)));
        }

        [TestMethod]
        public void TestTransformReturnsEmptyForEmpty()
        {
            Assert.ReferenceEquals(string.Empty, toTest.Transform(string.Empty, typeof(string)));
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

            var res = (string)toTest.Transform(str, typeof(string));
            Assert.ReferenceEquals(string.IsInterned(res), res);
        }
    }
}
