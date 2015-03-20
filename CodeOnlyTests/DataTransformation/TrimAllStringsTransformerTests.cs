using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CodeOnlyStoredProcedure.DataTransformation;
using System.Linq;
using FluentAssertions;

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
                                  .Should().BeTrue("because null will be 'trimmed' to the empty string"))
                  .ShouldNotThrow();
        }

        [TestMethod]
        public void CanTransformReturnsTrueForString()
        {
            toTest.Invoking(t => t.CanTransform("foo", typeof(string), false, Enumerable.Empty<Attribute>())
                                  .Should().BeTrue("because all strings can be trimmed"))
                  .ShouldNotThrow();
        }

        [TestMethod]
        public void TransformReturnsEmptyForEmpty()
        {
            toTest.Invoking(t => t.Transform(string.Empty, typeof(string), false, Enumerable.Empty<Attribute>())
                                  .Should().BeSameAs(string.Empty, "because the empty string is already trimmed"))
                  .ShouldNotThrow();
        }

        [TestMethod]
        public void TransformReturnsWhitespaceForNullInput()
        {
            toTest.Invoking(t => t.Transform(null, typeof(string), false, Enumerable.Empty<Attribute>())
                                  .Should().BeSameAs(string.Empty, "because null should return the empty string"))
                  .ShouldNotThrow();
        }

        [TestMethod]
        public void TransformReturnsEmptyForWhitespace()
        {
            toTest.Invoking(t => t.Transform("     ", typeof(string), false, Enumerable.Empty<Attribute>())
                                  .Should().Be(string.Empty, "because whitespace should return the empty string"))
                  .ShouldNotThrow();
        }

        [TestMethod]
        public void TransformReturnsValueWhenNoWhitespace()
        {
            toTest.Invoking(t => t.Transform("Foo", typeof(string), false, Enumerable.Empty<Attribute>())
                                  .Should().Be("Foo", "because it had no whitespace"))
                  .ShouldNotThrow();
        }

        [TestMethod]
        public void TransformReturnsValueWithoutTrailingWhitespace()
        {
            toTest.Invoking(t => t.Transform("Bar     ", typeof(string), false, Enumerable.Empty<Attribute>())
                                  .Should().Be("Bar", "because trailing whitespace should be trimmed"))
                  .ShouldNotThrow();
        }

        [TestMethod]
        public void TransformReturnsValueWithoutLeadingWhitespace()
        {
            toTest.Invoking(t => t.Transform("     Bar", Enumerable.Empty<Attribute>())
                                  .Should().Be("Bar", "because leading whitespace should be trimmed"))
                  .ShouldNotThrow();
        }

        [TestMethod]
        public void TypedTransformReturnsEmptyForEmpty()
        {
            toTest.Invoking(t => t.Transform(string.Empty, Enumerable.Empty<Attribute>())
                                  .Should().BeSameAs(string.Empty, "because the empty string is already trimmed"))
                  .ShouldNotThrow();
        }

        [TestMethod]
        public void TypedTransformReturnsWhitespaceForNullInput()
        {
            toTest.Invoking(t => t.Transform(null, Enumerable.Empty<Attribute>())
                                  .Should().BeSameAs(string.Empty, "because null should return the empty string"))
                  .ShouldNotThrow();
        }

        [TestMethod]
        public void TypedTransformReturnsEmptyForWhitespace()
        {
            toTest.Invoking(t => t.Transform("     ", Enumerable.Empty<Attribute>())
                                  .Should().Be(string.Empty, "because whitespace should return the empty string"))
                  .ShouldNotThrow();
        }

        [TestMethod]
        public void TypedTransformReturnsValueWhenNoWhitespace()
        {
            toTest.Invoking(t => t.Transform("Foo", Enumerable.Empty<Attribute>())
                                  .Should().Be("Foo", "because it had no whitespace"))
                  .ShouldNotThrow();
        }

        [TestMethod]
        public void TypedTransformReturnsValueWithoutTrailingWhitespace()
        {
            toTest.Invoking(t => t.Transform("Bar     ", Enumerable.Empty<Attribute>())
                                  .Should().Be("Bar", "because trailing whitespace should be trimmed"))
                  .ShouldNotThrow();
        }

        [TestMethod]
        public void TypedTransformReturnsValueWithoutLeadingWhitespace()
        {
            toTest.Invoking(t => t.Transform("     Bar", Enumerable.Empty<Attribute>())
                                  .Should().Be("Bar", "because leading whitespace should be trimmed"))
                  .ShouldNotThrow();
        }
    }
}
