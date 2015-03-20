using CodeOnlyStoredProcedure.DataTransformation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Reflection;
using FluentAssertions;

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
        public void CanTransformReturnsFalseForNonStringTargetType()
        {
            toTest.Invoking(t => t.CanTransform("foo", typeof(int), false, Enumerable.Empty<Attribute>())
                                  .Should().BeFalse("because the target type is not string"))
                  .ShouldNotThrow();
        }

        [TestMethod]
        public void CanTransformReturnsFalseForNonStringInput()
        {
            toTest.Invoking(t => t.CanTransform(false, typeof(string), false, Enumerable.Empty<Attribute>())
                                  .Should().BeFalse("because the input is not a string"))
                  .ShouldNotThrow();
        }

        [TestMethod]
        public void CanTransformReturnsFalseForNullInput()
        {
            toTest.Invoking(t => t.CanTransform(null, typeof(string), false, Enumerable.Empty<Attribute>())
                                  .Should().BeFalse("because the input is null"))
                  .ShouldNotThrow();
        }

        [TestMethod]
        public void CanTransformReturnsTrueForString()
        {
            toTest.Invoking(t => t.CanTransform("foo", typeof(string), false, Enumerable.Empty<Attribute>())
                                  .Should().BeTrue("because all strings can be interned"))
                  .ShouldNotThrow();
        }

        [TestMethod]
        public void TransformReturnsEmptyForEmpty()
        {
            toTest.Invoking(t => t.Transform(string.Empty, typeof(string), false, Enumerable.Empty<Attribute>())
                                  .Should().BeSameAs(string.Empty, "because the empty string is already interned"))
                  .ShouldNotThrow();
        }

        [TestMethod]
        public void TransformInternsString()
        {
            // can't use a string literal for the test, because the compiler interns
            // them automatically
            // we also have to use the executing assembly, so both the .net 4.0 & 4.5
            // assemblies can run the test. Otherwise, if one of them interns the
            // string, the other will fail.
            var str = Assembly.GetExecutingAssembly().FullName + GetType() + DateTime.Now;
            string.IsInterned(str).Should().BeNull(); // just make sure

            toTest.Invoking(t => t.Transform(str, typeof(string), false, Enumerable.Empty<Attribute>())
                                  .Should().BeSameAs(string.IsInterned(str), "because the string should be interned"))
                  .ShouldNotThrow();
        }

        [TestMethod]
        public void TypedTransformNullReturnsNull()
        {
            var res = toTest.Transform(null, Enumerable.Empty<Attribute>());
            res.Should().BeNull("because null was passed to the transformer.");
        }

        [TestMethod]
        public void TypedTransformInternsString()
        {
            // can't use a string literal for the test, because the compiler interns
            // them automatically
            // we also have to use the executing assembly, so both the .net 4.0 & 4.5
            // assemblies can run the test. Otherwise, if one of them interns the
            // string, the other will fail.
            var str = Assembly.GetExecutingAssembly().FullName + GetType() + DateTime.UtcNow;
            string.IsInterned(str).Should().BeNull(); // just make sure

            toTest.Invoking(t => t.Transform(str, Enumerable.Empty<Attribute>())
                                  .Should().BeSameAs(string.IsInterned(str), "because the string should be interned"))
                  .ShouldNotThrow();
        }
    }
}
