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
        public void TestWithReturnValueAddsParameterAndOutputSetter()
        {
            var orig = new StoredProcedure("Test");

            int rv = 0;
            var toTest = orig.WithReturnValue(i => rv = i);

            Assert.IsFalse(ReferenceEquals(orig, toTest));
            Assert.AreEqual(0, orig.Parameters.Count());
            Assert.AreEqual(0, orig.OutputParameterSetters.Count());

            var p = toTest.Parameters.Single();
            Assert.AreEqual(ParameterDirection.ReturnValue, p.Direction);
            Assert.AreEqual(SqlDbType.Int, p.SqlDbType);

            var act = toTest.OutputParameterSetters.Single();
            act.Value(100);

            Assert.AreEqual(100, rv);
        }
    }
}
