using System.Data;
using System.Linq;
using CodeOnlyStoredProcedure;
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

            Assert.IsFalse(ReferenceEquals(sp, toTest));
            Assert.AreEqual(0, sp.Parameters.Count());
            Assert.AreEqual(0, sp.OutputParameterSetters.Count());

            var p = toTest.Parameters.Single();
            Assert.AreEqual(ParameterDirection.InputOutput, p.Direction);
            Assert.AreEqual("Bar", p.Value);
            Assert.AreEqual("Foo", p.ParameterName);

            var output = toTest.OutputParameterSetters.Single();
            output.Value("Bar");
            Assert.AreEqual("Bar", set);
        }

        [TestMethod]
        public void TestWithInputOutputParameterAndSqlDbTypeHasInputAndSetsOutput()
        {
            var sp = new StoredProcedure("Test");

            string set = null;
            var toTest = sp.WithInputOutputParameter("Foo", "Bar", s => set = s, SqlDbType.NVarChar);

            Assert.IsFalse(ReferenceEquals(sp, toTest));
            Assert.AreEqual(0, sp.Parameters.Count());
            Assert.AreEqual(0, sp.OutputParameterSetters.Count());

            var p = toTest.Parameters.Single();
            Assert.AreEqual(ParameterDirection.InputOutput, p.Direction);
            Assert.AreEqual("Bar", p.Value);
            Assert.AreEqual("Foo", p.ParameterName);
            Assert.AreEqual(SqlDbType.NVarChar, p.SqlDbType);

            var output = toTest.OutputParameterSetters.Single();
            output.Value("Bar");
            Assert.AreEqual("Bar", set);
        }


        [TestMethod]
        public void TestWithInputOutputParameterSetsSize()
        {
            var sp = new StoredProcedure("Test");

            string set = null;
            var toTest = sp.WithInputOutputParameter("Foo", "Baz", s => set = s, size: 10);

            Assert.IsFalse(ReferenceEquals(sp, toTest));
            Assert.AreEqual(0, sp.Parameters.Count());
            Assert.AreEqual(0, sp.OutputParameterSetters.Count());

            var p = toTest.Parameters.Single();
            Assert.AreEqual(ParameterDirection.InputOutput, p.Direction);
            Assert.AreEqual("Foo", p.ParameterName);
            Assert.AreEqual(10, p.Size);
            Assert.AreEqual("Baz", p.Value);

            var output = toTest.OutputParameterSetters.Single();
            output.Value("Bar");
            Assert.AreEqual("Bar", set);
        }

        [TestMethod]
        public void TestWithInputOutputParameterAndSqlDbTypeSetsSize()
        {
            var sp = new StoredProcedure("Test");

            string set = null;
            var toTest = sp.WithInputOutputParameter("Foo", "Fab", s => set = s, SqlDbType.NVarChar, size: 10);

            Assert.IsFalse(ReferenceEquals(sp, toTest));
            Assert.AreEqual(0, sp.Parameters.Count());
            Assert.AreEqual(0, sp.OutputParameterSetters.Count());

            var p = toTest.Parameters.Single();
            Assert.AreEqual(ParameterDirection.InputOutput, p.Direction);
            Assert.AreEqual("Foo", p.ParameterName);
            Assert.AreEqual(SqlDbType.NVarChar, p.SqlDbType);
            Assert.AreEqual(10, p.Size);
            Assert.AreEqual("Fab", p.Value);

            var output = toTest.OutputParameterSetters.Single();
            output.Value("Bar");
            Assert.AreEqual("Bar", set);
        }

        [TestMethod]
        public void TestWithInputOutputParameterSetsScale()
        {
            var sp = new StoredProcedure("Test");

            decimal set = 0;
            var toTest = sp.WithInputOutputParameter("Foo", 99M, d => set = d, scale: 4);

            Assert.IsFalse(ReferenceEquals(sp, toTest));
            Assert.AreEqual(0, sp.Parameters.Count());
            Assert.AreEqual(0, sp.OutputParameterSetters.Count());

            var p = toTest.Parameters.Single();
            Assert.AreEqual(ParameterDirection.InputOutput, p.Direction);
            Assert.AreEqual("Foo", p.ParameterName);
            Assert.AreEqual(4, p.Scale);
            Assert.AreEqual(99M, p.Value);

            var output = toTest.OutputParameterSetters.Single();
            output.Value(142.13M);
            Assert.AreEqual(142.13M, set);
        }

        [TestMethod]
        public void TestWithInputOutputParameterAndSqlDbTypeSetsScale()
        {
            var sp = new StoredProcedure("Test");

            decimal set = 0;
            var toTest = sp.WithInputOutputParameter("Foo", 100M, d => set = d, SqlDbType.Decimal, scale: 4);

            Assert.IsFalse(ReferenceEquals(sp, toTest));
            Assert.AreEqual(0, sp.Parameters.Count());
            Assert.AreEqual(0, sp.OutputParameterSetters.Count());

            var p = toTest.Parameters.Single();
            Assert.AreEqual(ParameterDirection.InputOutput, p.Direction);
            Assert.AreEqual("Foo", p.ParameterName);
            Assert.AreEqual(4, p.Scale);
            Assert.AreEqual(100M, p.Value);
            Assert.AreEqual(SqlDbType.Decimal, p.SqlDbType);

            var output = toTest.OutputParameterSetters.Single();
            output.Value(12.37M);
            Assert.AreEqual(12.37M, set);
        }

        [TestMethod]
        public void TestWithInputOutputParameterSetsPrecision()
        {
            var sp = new StoredProcedure("Test");

            decimal set = 0;
            var toTest = sp.WithInputOutputParameter("Foo", 10M, d => set = d, precision: 11);

            Assert.IsFalse(ReferenceEquals(sp, toTest));
            Assert.AreEqual(0, sp.Parameters.Count());
            Assert.AreEqual(0, sp.OutputParameterSetters.Count());

            var p = toTest.Parameters.Single();
            Assert.AreEqual(ParameterDirection.InputOutput, p.Direction);
            Assert.AreEqual("Foo", p.ParameterName);
            Assert.AreEqual(11, p.Precision);
            Assert.AreEqual(10M, p.Value);

            var output = toTest.OutputParameterSetters.Single();
            output.Value(142.13M);
            Assert.AreEqual(142.13M, set);
        }

        [TestMethod]
        public void TestWithInputOutputParameterAndSqlDbTypeSetsPrecision()
        {
            var sp = new StoredProcedure("Test");

            decimal set = 0;
            var toTest = sp.WithInputOutputParameter("Foo", 13M, d => set = d, SqlDbType.Decimal, precision: 11);

            Assert.IsFalse(ReferenceEquals(sp, toTest));
            Assert.AreEqual(0, sp.Parameters.Count());
            Assert.AreEqual(0, sp.OutputParameterSetters.Count());

            var p = toTest.Parameters.Single();
            Assert.AreEqual("Foo", p.ParameterName);
            Assert.AreEqual(ParameterDirection.InputOutput, p.Direction);
            Assert.AreEqual(11, p.Precision);
            Assert.AreEqual(13M, p.Value);
            Assert.AreEqual(SqlDbType.Decimal, p.SqlDbType);

            var output = toTest.OutputParameterSetters.Single();
            output.Value(12.37M);
            Assert.AreEqual(12.37M, set);
        }
    }
}
