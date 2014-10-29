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
        public void TestWithOutputParameterAddsParameterAndSetsOutput()
        {
            var sp = new StoredProcedure("Test");

            string set = null;
            var toTest = sp.WithOutputParameter<StoredProcedure, string>("Foo", s => set = s);

            toTest.Should().NotBeSameAs(sp, "because StoredProcedures should be immutable");
            sp.Parameters.Should().BeEmpty("because StoredProcedures should be immutable");

            var param = toTest.Parameters.Should().ContainSingle(p => p.ParameterName == "Foo", "because we added one Parameter").Which;
            param.Should().BeOfType<OutputParameter>().Which.Invoking(p => p.TransferOutputValue("Baz")).Invoke();

            set.Should().Be("Baz", "because we invoked TransferOutputValue with Baz.");
        }

        [TestMethod]
        public void TestWithOutputParameterAndDbTypeAddsParameterAndSetsOutput()
        {
            var sp = new StoredProcedure("Test");

            string set = null;
            var toTest = sp.WithOutputParameter<StoredProcedure, string>("Foo", s => set = s, DbType.StringFixedLength);

            toTest.Should().NotBeSameAs(sp, "because StoredProcedures should be immutable");
            sp.Parameters.Should().BeEmpty("because StoredProcedures should be immutable");

            var param = toTest.Parameters.Should().ContainSingle(p => p.ParameterName == "Foo", "because we added one Parameter")
                .Which.Should().BeOfType<OutputParameter>().Which;
            param.DbType.Should().Be(DbType.StringFixedLength, "because it was passed to WithOutputParameter");
            param.Invoking(p => p.TransferOutputValue("Baz")).Invoke();

            set.Should().Be("Baz", "because we invoked TransferOutputValue with Baz.");
        }

        [TestMethod]
        public void TestWithOutputParameterHasInputAndSetsSize()
        {
            var sp = new StoredProcedure("Test");

            string set = null;
            var toTest = sp.WithOutputParameter<StoredProcedure, string>("Foo", s => set = s, size: 11);

            toTest.Should().NotBeSameAs(sp, "because StoredProcedures should be immutable");
            sp.Parameters.Should().BeEmpty("because StoredProcedures should be immutable");

            var param = toTest.Parameters.Should().ContainSingle(p => p.ParameterName == "Foo", "because we added one Parameter")
                .Which.Should().BeOfType<OutputParameter>().Which;
            param.Size.Should().Be(11, "because it was passed to WithOutputParameter");
        }

        [TestMethod]
        public void TestWithOutputParameterAndDbTypeHasInputAndSetsSize()
        {
            var sp = new StoredProcedure("Test");

            string set = null;
            var toTest = sp.WithOutputParameter<StoredProcedure, string>("Foo", s => set = s, DbType.StringFixedLength, size: 11);

            toTest.Should().NotBeSameAs(sp, "because StoredProcedures should be immutable");
            sp.Parameters.Should().BeEmpty("because StoredProcedures should be immutable");

            var param = toTest.Parameters.Should().ContainSingle(p => p.ParameterName == "Foo", "because we added one Parameter")
                .Which.Should().BeOfType<OutputParameter>().Which;
            param.DbType.Should().Be(DbType.StringFixedLength, "because it was passed to WithOutputParameter");
            param.Size.Should().Be(11, "because it was passed to WithOutputParameter");
        }

        [TestMethod]
        public void TestWithOutputParameterHasInputAndSetsScale()
        {
            var sp = new StoredProcedure("Test");

            string set = null;
            var toTest = sp.WithOutputParameter<StoredProcedure, string>("Foo", s => set = s, scale: 13);

            toTest.Should().NotBeSameAs(sp, "because StoredProcedures should be immutable");
            sp.Parameters.Should().BeEmpty("because StoredProcedures should be immutable");

            var param = toTest.Parameters.Should().ContainSingle(p => p.ParameterName == "Foo", "because we added one Parameter")
                .Which.Should().BeOfType<OutputParameter>().Which;
            param.Scale.Should().Be(13, "because it was passed to WithOutputParameter");
        }

        [TestMethod]
        public void TestWithOutputParameterAndDbTypeHasInputAndSetsScale()
        {
            var sp = new StoredProcedure("Test");

            string set = null;
            var toTest = sp.WithOutputParameter<StoredProcedure, string>("Foo", s => set = s, DbType.StringFixedLength, scale: 13);

            toTest.Should().NotBeSameAs(sp, "because StoredProcedures should be immutable");
            sp.Parameters.Should().BeEmpty("because StoredProcedures should be immutable");

            var param = toTest.Parameters.Should().ContainSingle(p => p.ParameterName == "Foo", "because we added one Parameter")
                .Which.Should().BeOfType<OutputParameter>().Which;
            param.DbType.Should().Be(DbType.StringFixedLength, "because it was passed to WithOutputParameter");
            param.Scale.Should().Be(13, "because it was passed to WithOutputParameter");
        }

        [TestMethod]
        public void TestWithOutputParameterHasInputAndSetsPrecision()
        {
            var sp = new StoredProcedure("Test");

            string set = null;
            var toTest = sp.WithOutputParameter<StoredProcedure, string>("Foo", s => set = s, precision: 3);

            toTest.Should().NotBeSameAs(sp, "because StoredProcedures should be immutable");
            sp.Parameters.Should().BeEmpty("because StoredProcedures should be immutable");

            var param = toTest.Parameters.Should().ContainSingle(p => p.ParameterName == "Foo", "because we added one Parameter")
                .Which.Should().BeOfType<OutputParameter>().Which;
            param.Precision.Should().Be(3, "because it was passed to WithOutputParameter");
        }

        [TestMethod]
        public void TestWithOutputParameterAndDbTypeHasInputAndSetsPrecision()
        {
            var sp = new StoredProcedure("Test");

            string set = null;
            var toTest = sp.WithOutputParameter<StoredProcedure, string>("Foo", s => set = s, DbType.StringFixedLength, precision: 3);

            toTest.Should().NotBeSameAs(sp, "because StoredProcedures should be immutable");
            sp.Parameters.Should().BeEmpty("because StoredProcedures should be immutable");

            var param = toTest.Parameters.Should().ContainSingle(p => p.ParameterName == "Foo", "because we added one Parameter")
                .Which.Should().BeOfType<OutputParameter>().Which;
            param.DbType.Should().Be(DbType.StringFixedLength, "because it was passed to WithOutputParameter");
            param.Precision.Should().Be(3, "because it was passed to WithOutputParameter");
        }
    }
}
