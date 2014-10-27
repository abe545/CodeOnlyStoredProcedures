using System.Data;
using CodeOnlyStoredProcedure;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;


#if NET40
namespace CodeOnlyTests.Net40
#else
namespace CodeOnlyTests
#endif
{
    public partial class StoredProcedureExtensionsTests
    {
        [TestMethod]
        public void TestWithParameterAddsParameterToNewStoredProcedure()
        {
            var orig = new StoredProcedure("Test");

            var toTest = orig.WithParameter("Foo", "Bar");

            toTest.Should().NotBeSameAs(orig, "because StoredProcedures should be immutable");
            orig.Parameters.Should().BeEmpty("because StoredProcedures should be immutable");

            var param = toTest.Parameters.Should().ContainSingle(p => p.ParameterName == "Foo", "because we added one Parameter").Which;
            param.Should().BeOfType<InputParameter>().Which.Value.Should().Be("Bar", "because it was passed to WithInputParameter");
        }

        [TestMethod]
        public void TestWithParamaterAndDbTypeAddsParameterToNewStoredProcedure()
        {
            var orig = new StoredProcedure("Test");

            var toTest = orig.WithParameter("Foo", "Bar", DbType.StringFixedLength);

            toTest.Should().NotBeSameAs(orig, "because StoredProcedures should be immutable");
            orig.Parameters.Should().BeEmpty("because StoredProcedures should be immutable");

            var param = toTest.Parameters.Should().ContainSingle(p => p.ParameterName == "Foo", "because we added one Parameter")
                                                  .Which.Should().BeOfType<InputParameter>().Which;
            param.Value.Should().Be("Bar", "because it was passed to WithInputParameter");
            param.DbType.Should().Be(DbType.StringFixedLength, "because it was passed to WithInputParameter");
        }

        [TestMethod]
        public void TestWithParameterClonesStoredProcedureWithResultType()
        {
            var orig = new StoredProcedure<int>("Test");

            var toTest = orig.WithParameter("Foo", "Bar");

            toTest.Should().NotBeSameAs(orig, "because StoredProcedures should be immutable");
            orig.Parameters.Should().BeEmpty("because StoredProcedures should be immutable");
            toTest.Should().BeOfType<StoredProcedure<int>>("because the original StoredProcedure returned an int also.");

            var param = toTest.Parameters.Should().ContainSingle(p => p.ParameterName == "Foo", "because we added one Parameter").Which;
            param.Should().BeOfType<InputParameter>().Which.Value.Should().Be("Bar", "because it was passed to WithInputParameter");
        }
    }
}
