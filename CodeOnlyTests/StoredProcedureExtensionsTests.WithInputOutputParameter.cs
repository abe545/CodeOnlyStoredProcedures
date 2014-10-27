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
        public void TestWithInputOutputParameterHasInputAndSetsOutput()
        {
            var sp = new StoredProcedure("Test");

            string set = null;
            var toTest = sp.WithInputOutputParameter("Foo", "Bar", s => set = s);

            toTest.Should().NotBeSameAs(sp, "because StoredProcedures should be immutable");
            sp.Parameters.Should().BeEmpty("because StoredProcedures should be immutable");

            var param = toTest.Parameters.Should().ContainSingle(p => p.ParameterName == "Foo", "because we added one Parameter").Which;
            param.Should().BeOfType<InputOutputParameter>().Which.Value.Should().Be("Bar", "because it was passed to WithInputOutputParameter");
            param.Should().BeOfType<InputOutputParameter>().Which.Invoking(p => p.TransferOutputValue("Baz")).Invoke();

            set.Should().Be("Baz", "because we invoked TransferOutputValue with Baz.");
        }

        [TestMethod]
        public void TestWithInputOutputParameterAndDbTypeHasInputAndSetsOutput()
        {
            var sp = new StoredProcedure("Test");

            string set = null;
            var toTest = sp.WithInputOutputParameter("Foo", "Bar", s => set = s, DbType.StringFixedLength);

            toTest.Should().NotBeSameAs(sp, "because StoredProcedures should be immutable");
            sp.Parameters.Should().BeEmpty("because StoredProcedures should be immutable");

            var param = toTest.Parameters.Should().ContainSingle(p => p.ParameterName == "Foo", "because we added one Parameter")
                .Which.Should().BeOfType<InputOutputParameter>().Which;
            param.Value.Should().Be("Bar", "because it was passed to WithInputOutputParameter");
            param.DbType.Should().Be(DbType.StringFixedLength, "because it was passed to WithInputOutputParameter");
            param.Invoking(p => p.TransferOutputValue("Baz")).Invoke();

            set.Should().Be("Baz", "because we invoked TransferOutputValue with Baz.");
        }

        [TestMethod]
        public void TestWithInputOutputParameterHasInputAndSetsSize()
        {
            var sp = new StoredProcedure("Test");

            string set = null;
            var toTest = sp.WithInputOutputParameter("Foo", "Bar", s => set = s, size: 11);

            toTest.Should().NotBeSameAs(sp, "because StoredProcedures should be immutable");
            sp.Parameters.Should().BeEmpty("because StoredProcedures should be immutable");

            var param = toTest.Parameters.Should().ContainSingle(p => p.ParameterName == "Foo", "because we added one Parameter")
                .Which.Should().BeOfType<InputOutputParameter>().Which;
            param.Value.Should().Be("Bar", "because it was passed to WithInputOutputParameter");
            param.Size.Should().Be(11, "because it was passed to WithInputOutputParameter");
        }

        [TestMethod]
        public void TestWithInputOutputParameterAndDbTypeHasInputAndSetsSize()
        {
            var sp = new StoredProcedure("Test");

            string set = null;
            var toTest = sp.WithInputOutputParameter("Foo", "Bar", s => set = s, DbType.StringFixedLength, size: 11);

            toTest.Should().NotBeSameAs(sp, "because StoredProcedures should be immutable");
            sp.Parameters.Should().BeEmpty("because StoredProcedures should be immutable");

            var param = toTest.Parameters.Should().ContainSingle(p => p.ParameterName == "Foo", "because we added one Parameter")
                .Which.Should().BeOfType<InputOutputParameter>().Which;
            param.Value.Should().Be("Bar", "because it was passed to WithInputOutputParameter");
            param.DbType.Should().Be(DbType.StringFixedLength, "because it was passed to WithInputOutputParameter");
            param.Size.Should().Be(11, "because it was passed to WithInputOutputParameter");
        }

        [TestMethod]
        public void TestWithInputOutputParameterHasInputAndSetsScale()
        {
            var sp = new StoredProcedure("Test");

            string set = null;
            var toTest = sp.WithInputOutputParameter("Foo", "Bar", s => set = s, scale: 13);

            toTest.Should().NotBeSameAs(sp, "because StoredProcedures should be immutable");
            sp.Parameters.Should().BeEmpty("because StoredProcedures should be immutable");

            var param = toTest.Parameters.Should().ContainSingle(p => p.ParameterName == "Foo", "because we added one Parameter")
                .Which.Should().BeOfType<InputOutputParameter>().Which;
            param.Value.Should().Be("Bar", "because it was passed to WithInputOutputParameter");
            param.Scale.Should().Be(13, "because it was passed to WithInputOutputParameter");
        }

        [TestMethod]
        public void TestWithInputOutputParameterAndDbTypeHasInputAndSetsScale()
        {
            var sp = new StoredProcedure("Test");

            string set = null;
            var toTest = sp.WithInputOutputParameter("Foo", "Bar", s => set = s, DbType.StringFixedLength, scale: 13);

            toTest.Should().NotBeSameAs(sp, "because StoredProcedures should be immutable");
            sp.Parameters.Should().BeEmpty("because StoredProcedures should be immutable");

            var param = toTest.Parameters.Should().ContainSingle(p => p.ParameterName == "Foo", "because we added one Parameter")
                .Which.Should().BeOfType<InputOutputParameter>().Which;
            param.Value.Should().Be("Bar", "because it was passed to WithInputOutputParameter");
            param.DbType.Should().Be(DbType.StringFixedLength, "because it was passed to WithInputOutputParameter");
            param.Scale.Should().Be(13, "because it was passed to WithInputOutputParameter");
        }

        [TestMethod]
        public void TestWithInputOutputParameterHasInputAndSetsPrecision()
        {
            var sp = new StoredProcedure("Test");

            string set = null;
            var toTest = sp.WithInputOutputParameter("Foo", "Bar", s => set = s, precision: 3);

            toTest.Should().NotBeSameAs(sp, "because StoredProcedures should be immutable");
            sp.Parameters.Should().BeEmpty("because StoredProcedures should be immutable");

            var param = toTest.Parameters.Should().ContainSingle(p => p.ParameterName == "Foo", "because we added one Parameter")
                .Which.Should().BeOfType<InputOutputParameter>().Which;
            param.Value.Should().Be("Bar", "because it was passed to WithInputOutputParameter");
            param.Precision.Should().Be(3, "because it was passed to WithInputOutputParameter");
        }

        [TestMethod]
        public void TestWithInputOutputParameterAndDbTypeHasInputAndSetsPrecision()
        {
            var sp = new StoredProcedure("Test");

            string set = null;
            var toTest = sp.WithInputOutputParameter("Foo", "Bar", s => set = s, DbType.StringFixedLength, precision: 3);

            toTest.Should().NotBeSameAs(sp, "because StoredProcedures should be immutable");
            sp.Parameters.Should().BeEmpty("because StoredProcedures should be immutable");

            var param = toTest.Parameters.Should().ContainSingle(p => p.ParameterName == "Foo", "because we added one Parameter")
                .Which.Should().BeOfType<InputOutputParameter>().Which;
            param.Value.Should().Be("Bar", "because it was passed to WithInputOutputParameter");
            param.DbType.Should().Be(DbType.StringFixedLength, "because it was passed to WithInputOutputParameter");
            param.Precision.Should().Be(3, "because it was passed to WithInputOutputParameter");
        }
    }
}
