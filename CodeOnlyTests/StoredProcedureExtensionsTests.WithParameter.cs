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
        public void TestWithParameterAddsParameterToNewStoredProcedure()
        {
            var orig = new StoredProcedure("Test");

            var toTest = orig.WithParameter("Foo", "Bar");

            Assert.IsFalse(ReferenceEquals(orig, toTest));
            Assert.AreEqual(0, orig.Parameters.Count());
            Assert.AreEqual(1, toTest.Parameters.Count());

            var p = toTest.Parameters.Single();
            Assert.AreEqual("Foo", p.ParameterName);
            Assert.AreEqual("Bar", p.Value);
            Assert.AreEqual(ParameterDirection.Input, p.Direction);
        }

        [TestMethod]
        public void TestWithParamaterAndSqlTypeAddsParameterToNewStoredProcedure()
        {
            var orig = new StoredProcedure("Test");

            var toTest = orig.WithParameter("Foo", "Bar", SqlDbType.NChar);

            Assert.IsFalse(ReferenceEquals(orig, toTest));
            Assert.AreEqual(0, orig.Parameters.Count());
            Assert.AreEqual(1, toTest.Parameters.Count());

            var p = toTest.Parameters.Single();
            Assert.AreEqual("Foo", p.ParameterName);
            Assert.AreEqual("Bar", p.Value);
            Assert.AreEqual(ParameterDirection.Input, p.Direction);
            Assert.AreEqual(SqlDbType.NChar, p.SqlDbType);
        }

        [TestMethod]
        public void TestWithParameterClonesStoredProcedureWithResultType()
        {
            var orig = new StoredProcedure<int>("Test");

            var toTest = orig.WithParameter("Foo", "Bar");

            Assert.AreEqual(typeof(StoredProcedure<int>), toTest.GetType());
            Assert.IsFalse(ReferenceEquals(orig, toTest));
            Assert.AreEqual(0, orig.Parameters.Count());
            Assert.AreEqual(1, toTest.Parameters.Count());

            var p = toTest.Parameters.Single();
            Assert.AreEqual(ParameterDirection.Input, p.Direction);
            Assert.AreEqual("Foo", p.ParameterName);
            Assert.AreEqual("Bar", p.Value);
        }
    }
}
