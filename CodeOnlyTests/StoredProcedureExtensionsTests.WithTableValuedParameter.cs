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
        public void TestWithTableValuedParameterAddsParameter()
        {
            var orig = new StoredProcedure("Test");

            var tvp = new[]
                {
                    new TVPHelper { Name = "Hello", Foo = 0, Bar = 100M },
                    new TVPHelper { Name = "World", Foo = 3, Bar = 331M }
                };

            var toTest = orig.WithTableValuedParameter("Foo", tvp, "TVP");

            toTest.Should().NotBeSameAs(orig, "because StoredProcedures should be immutable");
            orig.Parameters.Should().BeEmpty("because StoredProcedures should be immutable");

            var param = toTest.Parameters.Should().ContainSingle(p => p.ParameterName == "Foo", "because we added one Parameter")
                .Which.Should().BeOfType<TableValuedParameter>().Which;
            param.Value.Should().Be(tvp, "because it was passed to WithInputParameter");
            param.TypeName.Should().Be("[dbo].[TVP]", "because it was passed to WithInputParameter");
        }

        [TestMethod]
        public void TestWithTableValuedParameterWithSchemaAddsParameter()
        {
            var orig = new StoredProcedure("Test");

            var tvp = new[]
                {
                    new TVPHelper { Name = "Hello", Foo = 0, Bar = 100M },
                    new TVPHelper { Name = "World", Foo = 3, Bar = 331M }
                };

            var toTest = orig.WithTableValuedParameter("Foo", tvp, "TVP", "Table Type");

            toTest.Should().NotBeSameAs(orig, "because StoredProcedures should be immutable");
            orig.Parameters.Should().BeEmpty("because StoredProcedures should be immutable");

            var param = toTest.Parameters.Should().ContainSingle(p => p.ParameterName == "Foo", "because we added one Parameter")
                .Which.Should().BeOfType<TableValuedParameter>().Which;
            param.Value.Should().Be(tvp, "because it was passed to WithInputParameter");
            param.TypeName.Should().Be("[TVP].[Table Type]", "because it was passed to WithInputParameter");
        }
    }
}
