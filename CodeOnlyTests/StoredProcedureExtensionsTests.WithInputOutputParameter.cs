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
        public void TestWithInputOutputParameterHasInputAndSetsOutput()
        {
            var sp = new StoredProcedure("Test");

            string set = null;
            var toTest = sp.WithInputOutputParameter("Foo", "Bar", s => set = s);

            toTest.Should().NotBeSameAs(sp, "because StoredProcedures should be immutable");
            sp.Parameters.Should().BeEmpty("because StoredProcedures should be immutable");

            var param = toTest.Parameters.Should().ContainSingle(p => p.ParameterName == "Foo", "because we added one Parameter").Which;
            param.Should().BeOfType<IInputStoredProcedureParameter>().Which.Value.Should().Be("Bar", "because it was passed to WithInputOutputParameter");
            param.Should().BeOfType<IOutputStoredProcedureParameter>().Which.Invoking(p => p.TransferOutputValue("Baz")).Invoke();

            set.Should().Be("Baz", "because we invoked TransferOutputValue with Baz.");
        }

        //[TestMethod]
        //public void TestWithInputOutputParameterAndDbTypeHasInputAndSetsOutput()
        //{
        //    var sp = new StoredProcedure("Test");

        //    string set = null;
        //    var toTest = sp.WithInputOutputParameter("Foo", "Bar", s => set = s, DbType.NVarChar);

        //    Assert.IsFalse(ReferenceEquals(sp, toTest));
        //    Assert.AreEqual(0, sp.Parameters.Count());
        //    Assert.AreEqual(0, sp.OutputParameterSetters.Count());

        //    var p = toTest.Parameters.Single();
        //    Assert.AreEqual(ParameterDirection.InputOutput, p.Direction);
        //    Assert.AreEqual("Bar", p.Value);
        //    Assert.AreEqual("Foo", p.ParameterName);
        //    Assert.AreEqual(DbType.NVarChar, p.DbType);

        //    var output = toTest.OutputParameterSetters.Single();
        //    output.Value("Bar");
        //    Assert.AreEqual("Bar", set);
        //}


        //[TestMethod]
        //public void TestWithInputOutputParameterSetsSize()
        //{
        //    var sp = new StoredProcedure("Test");

        //    string set = null;
        //    var toTest = sp.WithInputOutputParameter("Foo", "Baz", s => set = s, size: 10);

        //    Assert.IsFalse(ReferenceEquals(sp, toTest));
        //    Assert.AreEqual(0, sp.Parameters.Count());
        //    Assert.AreEqual(0, sp.OutputParameterSetters.Count());

        //    var p = toTest.Parameters.Single();
        //    Assert.AreEqual(ParameterDirection.InputOutput, p.Direction);
        //    Assert.AreEqual("Foo", p.ParameterName);
        //    Assert.AreEqual(10, p.Size);
        //    Assert.AreEqual("Baz", p.Value);

        //    var output = toTest.OutputParameterSetters.Single();
        //    output.Value("Bar");
        //    Assert.AreEqual("Bar", set);
        //}

        //[TestMethod]
        //public void TestWithInputOutputParameterAndDbTypeSetsSize()
        //{
        //    var sp = new StoredProcedure("Test");

        //    string set = null;
        //    var toTest = sp.WithInputOutputParameter("Foo", "Fab", s => set = s, DbType.NVarChar, size: 10);

        //    Assert.IsFalse(ReferenceEquals(sp, toTest));
        //    Assert.AreEqual(0, sp.Parameters.Count());
        //    Assert.AreEqual(0, sp.OutputParameterSetters.Count());

        //    var p = toTest.Parameters.Single();
        //    Assert.AreEqual(ParameterDirection.InputOutput, p.Direction);
        //    Assert.AreEqual("Foo", p.ParameterName);
        //    Assert.AreEqual(DbType.NVarChar, p.DbType);
        //    Assert.AreEqual(10, p.Size);
        //    Assert.AreEqual("Fab", p.Value);

        //    var output = toTest.OutputParameterSetters.Single();
        //    output.Value("Bar");
        //    Assert.AreEqual("Bar", set);
        //}

        //[TestMethod]
        //public void TestWithInputOutputParameterSetsScale()
        //{
        //    var sp = new StoredProcedure("Test");

        //    decimal set = 0;
        //    var toTest = sp.WithInputOutputParameter("Foo", 99M, d => set = d, scale: 4);

        //    Assert.IsFalse(ReferenceEquals(sp, toTest));
        //    Assert.AreEqual(0, sp.Parameters.Count());
        //    Assert.AreEqual(0, sp.OutputParameterSetters.Count());

        //    var p = toTest.Parameters.Single();
        //    Assert.AreEqual(ParameterDirection.InputOutput, p.Direction);
        //    Assert.AreEqual("Foo", p.ParameterName);
        //    Assert.AreEqual(4, p.Scale);
        //    Assert.AreEqual(99M, p.Value);

        //    var output = toTest.OutputParameterSetters.Single();
        //    output.Value(142.13M);
        //    Assert.AreEqual(142.13M, set);
        //}

        //[TestMethod]
        //public void TestWithInputOutputParameterAndDbTypeSetsScale()
        //{
        //    var sp = new StoredProcedure("Test");

        //    decimal set = 0;
        //    var toTest = sp.WithInputOutputParameter("Foo", 100M, d => set = d, DbType.Decimal, scale: 4);

        //    Assert.IsFalse(ReferenceEquals(sp, toTest));
        //    Assert.AreEqual(0, sp.Parameters.Count());
        //    Assert.AreEqual(0, sp.OutputParameterSetters.Count());

        //    var p = toTest.Parameters.Single();
        //    Assert.AreEqual(ParameterDirection.InputOutput, p.Direction);
        //    Assert.AreEqual("Foo", p.ParameterName);
        //    Assert.AreEqual(4, p.Scale);
        //    Assert.AreEqual(100M, p.Value);
        //    Assert.AreEqual(DbType.Decimal, p.DbType);

        //    var output = toTest.OutputParameterSetters.Single();
        //    output.Value(12.37M);
        //    Assert.AreEqual(12.37M, set);
        //}

        //[TestMethod]
        //public void TestWithInputOutputParameterSetsPrecision()
        //{
        //    var sp = new StoredProcedure("Test");

        //    decimal set = 0;
        //    var toTest = sp.WithInputOutputParameter("Foo", 10M, d => set = d, precision: 11);

        //    Assert.IsFalse(ReferenceEquals(sp, toTest));
        //    Assert.AreEqual(0, sp.Parameters.Count());
        //    Assert.AreEqual(0, sp.OutputParameterSetters.Count());

        //    var p = toTest.Parameters.Single();
        //    Assert.AreEqual(ParameterDirection.InputOutput, p.Direction);
        //    Assert.AreEqual("Foo", p.ParameterName);
        //    Assert.AreEqual(11, p.Precision);
        //    Assert.AreEqual(10M, p.Value);

        //    var output = toTest.OutputParameterSetters.Single();
        //    output.Value(142.13M);
        //    Assert.AreEqual(142.13M, set);
        //}

        //[TestMethod]
        //public void TestWithInputOutputParameterAndDbTypeSetsPrecision()
        //{
        //    var sp = new StoredProcedure("Test");

        //    decimal set = 0;
        //    var toTest = sp.WithInputOutputParameter("Foo", 13M, d => set = d, DbType.Decimal, precision: 11);

        //    Assert.IsFalse(ReferenceEquals(sp, toTest));
        //    Assert.AreEqual(0, sp.Parameters.Count());

        //    var p = toTest.Parameters.OfType<IInputStoredProcedureParameter>().OfType<IOutputStoredProcedureParameter>().Single();
        //    Assert.AreEqual("Foo", p.ParameterName);
        //    Assert.AreEqual(DbType.Decimal, p.DbType);

        //    p.TransferOutputValue(12.37M);
        //    Assert.AreEqual(12.37M, set);
        //}
    }
}
