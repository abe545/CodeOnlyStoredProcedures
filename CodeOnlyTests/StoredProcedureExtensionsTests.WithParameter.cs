using System.Data;
using System.Linq;
using CodeOnlyStoredProcedure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

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
            param.Should().BeOfType<IInputStoredProcedureParameter>().Which.Value.Should().Be("Bar", "because it was passed to WithInputParameter");
            (param as IOutputStoredProcedureParameter).Should().BeNull("because WithInputParameter should not create output parameters");
        }

        [TestMethod]
        public void TestWithParamaterAndSqlTypeAddsParameterToNewStoredProcedure()
        {
            var orig = new StoredProcedure("Test");

            var toTest = orig.WithParameter("Foo", "Bar", DbType.String);

            toTest.Should().NotBeSameAs(orig, "because StoredProcedures should be immutable");
            orig.Parameters.Should().BeEmpty("because StoredProcedures should be immutable");

            var param = toTest.Parameters.Should().ContainSingle(p => p.ParameterName == "Foo", "because we added one Parameter").Which;
            param.Should().BeOfType<IInputStoredProcedureParameter>().Which.Value.Should().Be("Bar", "because it was passed to WithInputParameter");
            (param as IOutputStoredProcedureParameter).Should().BeNull("because WithInputParameter should not create output parameters");
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
            param.Should().BeOfType<IInputStoredProcedureParameter>().Which.Value.Should().Be("Bar", "because it was passed to WithInputParameter");
            (param as IOutputStoredProcedureParameter).Should().BeNull("because WithInputParameter should not create output parameters");
        }
    }
}
