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
        public void TestWithOutputParameterAddsParameterAndSetter()
        {
            var sp = new StoredProcedure("Test");

            string set = null;
            var toTest = sp.WithOutputParameter<StoredProcedure, string>("Foo", s => set = s);

            Assert.IsFalse(ReferenceEquals(sp, toTest));
            Assert.AreEqual(0, sp.Parameters.Count());
            Assert.AreEqual(0, sp.OutputParameterSetters.Count());

            var p = toTest.Parameters.Single();
            Assert.AreEqual(ParameterDirection.Output, p.Direction);
            Assert.AreEqual("Foo", p.ParameterName);

            var output = toTest.OutputParameterSetters.Single();
            output.Value("Bar");
            Assert.AreEqual("Bar", set);
        }

        [TestMethod]
        public void TestWithOutputParameterSetsSqlDbType()
        {
            var sp = new StoredProcedure("Test");

            int set = 0;
            var toTest = sp.WithOutputParameter<StoredProcedure, int>("Foo", s => set = s, SqlDbType.Int);

            Assert.IsFalse(ReferenceEquals(sp, toTest));
            Assert.AreEqual(0, sp.Parameters.Count());
            Assert.AreEqual(0, sp.OutputParameterSetters.Count());

            var p = toTest.Parameters.Single();
            Assert.AreEqual(ParameterDirection.Output, p.Direction);
            Assert.AreEqual("Foo", p.ParameterName);
            Assert.AreEqual(SqlDbType.Int, p.SqlDbType);

            var output = toTest.OutputParameterSetters.Single();
            output.Value(42);
            Assert.AreEqual(42, set);
        }

        [TestMethod]
        public void TestWithOutputParameterSetsSize()
        {
            var sp = new StoredProcedure("Test");

            string set = null;
            var toTest = sp.WithOutputParameter<StoredProcedure, string>("Foo", s => set = s, size: 10);

            Assert.IsFalse(ReferenceEquals(sp, toTest));
            Assert.AreEqual(0, sp.Parameters.Count());
            Assert.AreEqual(0, sp.OutputParameterSetters.Count());

            var p = toTest.Parameters.Single();
            Assert.AreEqual(ParameterDirection.Output, p.Direction);
            Assert.AreEqual("Foo", p.ParameterName);
            Assert.AreEqual(10, p.Size);

            var output = toTest.OutputParameterSetters.Single();
            output.Value("Bar");
            Assert.AreEqual("Bar", set);
        }

        [TestMethod]
        public void TestWithOutputParameterAndSqlDbTypeSetsSize()
        {
            var sp = new StoredProcedure("Test");

            string set = null;
            var toTest = sp.WithOutputParameter<StoredProcedure, string>("Foo", s => set = s, SqlDbType.NVarChar, size: 10);

            Assert.IsFalse(ReferenceEquals(sp, toTest));
            Assert.AreEqual(0, sp.Parameters.Count());
            Assert.AreEqual(0, sp.OutputParameterSetters.Count());

            var p = toTest.Parameters.Single();
            Assert.AreEqual(ParameterDirection.Output, p.Direction);
            Assert.AreEqual("Foo", p.ParameterName);
            Assert.AreEqual(SqlDbType.NVarChar, p.SqlDbType);
            Assert.AreEqual(10, p.Size);

            var output = toTest.OutputParameterSetters.Single();
            output.Value("Bar");
            Assert.AreEqual("Bar", set);
        }

        [TestMethod]
        public void TestWithOutputParameterSetsScale()
        {
            var sp = new StoredProcedure("Test");

            decimal set = 0;
            var toTest = sp.WithOutputParameter<StoredProcedure, decimal>("Foo", d => set = d, scale: 4);

            Assert.IsFalse(ReferenceEquals(sp, toTest));
            Assert.AreEqual(0, sp.Parameters.Count());
            Assert.AreEqual(0, sp.OutputParameterSetters.Count());

            var p = toTest.Parameters.Single();
            Assert.AreEqual(ParameterDirection.Output, p.Direction);
            Assert.AreEqual("Foo", p.ParameterName);
            Assert.AreEqual(4, p.Scale);

            var output = toTest.OutputParameterSetters.Single();
            output.Value(142.13M);
            Assert.AreEqual(142.13M, set);
        }

        [TestMethod]
        public void TestWithOutputParameterAndSqlDbTypeSetsScale()
        {
            var sp = new StoredProcedure("Test");

            decimal set = 0;
            var toTest = sp.WithOutputParameter<StoredProcedure, decimal>("Foo", d => set = d, SqlDbType.Decimal, scale: 4);

            Assert.IsFalse(ReferenceEquals(sp, toTest));
            Assert.AreEqual(0, sp.Parameters.Count());
            Assert.AreEqual(0, sp.OutputParameterSetters.Count());

            var p = toTest.Parameters.Single();
            Assert.AreEqual(ParameterDirection.Output, p.Direction);
            Assert.AreEqual("Foo", p.ParameterName);
            Assert.AreEqual(4, p.Scale);
            Assert.AreEqual(SqlDbType.Decimal, p.SqlDbType);

            var output = toTest.OutputParameterSetters.Single();
            output.Value(12.37M);
            Assert.AreEqual(12.37M, set);
        }

        [TestMethod]
        public void TestWithOutputParameterSetsPrecision()
        {
            var sp = new StoredProcedure("Test");

            decimal set = 0;
            var toTest = sp.WithOutputParameter<StoredProcedure, decimal>("Foo", d => set = d, precision: 11);

            Assert.IsFalse(ReferenceEquals(sp, toTest));
            Assert.AreEqual(0, sp.Parameters.Count());
            Assert.AreEqual(0, sp.OutputParameterSetters.Count());

            var p = toTest.Parameters.Single();
            Assert.AreEqual(ParameterDirection.Output, p.Direction);
            Assert.AreEqual("Foo", p.ParameterName);
            Assert.AreEqual(11, p.Precision);

            var output = toTest.OutputParameterSetters.Single();
            output.Value(142.13M);
            Assert.AreEqual(142.13M, set);
        }

        [TestMethod]
        public void TestWithOutputParameterAndSqlDbTypeSetsPrecision()
        {
            var sp = new StoredProcedure("Test");

            decimal set = 0;
            var toTest = sp.WithOutputParameter<StoredProcedure, decimal>("Foo", d => set = d, SqlDbType.Decimal, precision: 11);

            Assert.IsFalse(ReferenceEquals(sp, toTest));
            Assert.AreEqual(0, sp.Parameters.Count());
            Assert.AreEqual(0, sp.OutputParameterSetters.Count());

            var p = toTest.Parameters.Single();
            Assert.AreEqual("Foo", p.ParameterName);
            Assert.AreEqual(11, p.Precision);
            Assert.AreEqual(SqlDbType.Decimal, p.SqlDbType);

            var output = toTest.OutputParameterSetters.Single();
            output.Value(12.37M);
            Assert.AreEqual(12.37M, set);
        }
    }
}
