using System.Collections.Generic;
using System.Data;
using System.Linq;
using CodeOnlyStoredProcedure;
using Microsoft.SqlServer.Server;
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
            var sp = new StoredProcedure("Test");

            var tvp = new[]
            {
                new TVPHelper { Name = "Hello", Foo = 0, Bar = 100M },
                new TVPHelper { Name = "World", Foo = 3, Bar = 331M }
            };

            var toTest = sp.WithTableValuedParameter("Bar", tvp, "TVP");

            Assert.IsFalse(ReferenceEquals(sp, toTest));
            Assert.AreEqual(0, sp.Parameters.Count());
            Assert.AreEqual(0, sp.OutputParameterSetters.Count);

            Assert.AreEqual(0, toTest.OutputParameterSetters.Count);
            var p = toTest.Parameters.Single();
            Assert.AreEqual(SqlDbType.Structured, p.SqlDbType);
            Assert.AreEqual("Bar", p.ParameterName);
            Assert.AreEqual("[dbo].[TVP]", p.TypeName);

            int i = 0;
            foreach (var record in (IEnumerable<SqlDataRecord>)p.Value)
            {
                Assert.AreEqual("Name", record.GetName(0));
                Assert.AreEqual(tvp[i].Name, record.GetString(0));

                Assert.AreEqual("Foo", record.GetName(1));
                Assert.AreEqual(tvp[i].Foo, record.GetInt32(1));

                Assert.AreEqual("Bar", record.GetName(2));
                Assert.AreEqual(tvp[i].Bar, record.GetDecimal(2));

                ++i;
            }
        }

        [TestMethod]
        public void TestWithTableValuedParameterWithSchemaAddsParameter()
        {
            var sp = new StoredProcedure("Test");

            var tvp = new[]
            {
                new TVPHelper { Name = "Hello", Foo = 0, Bar = 100M },
                new TVPHelper { Name = "World", Foo = 3, Bar = 331M }
            };

            var toTest = sp.WithTableValuedParameter("Bar", tvp, "TVP", "Table Type");

            Assert.IsFalse(ReferenceEquals(sp, toTest));
            Assert.AreEqual(0, sp.Parameters.Count());
            Assert.AreEqual(0, sp.OutputParameterSetters.Count);

            Assert.AreEqual(0, toTest.OutputParameterSetters.Count);
            var p = toTest.Parameters.Single();
            Assert.AreEqual(SqlDbType.Structured, p.SqlDbType);
            Assert.AreEqual("Bar", p.ParameterName);
            Assert.AreEqual("[TVP].[Table Type]", p.TypeName);

            int i = 0;
            foreach (var record in (IEnumerable<SqlDataRecord>)p.Value)
            {
                Assert.AreEqual("Name", record.GetName(0));
                Assert.AreEqual(tvp[i].Name, record.GetString(0));

                Assert.AreEqual("Foo", record.GetName(1));
                Assert.AreEqual(tvp[i].Foo, record.GetInt32(1));

                Assert.AreEqual("Bar", record.GetName(2));
                Assert.AreEqual(tvp[i].Bar, record.GetDecimal(2));

                ++i;
            }
        }
    }
}
