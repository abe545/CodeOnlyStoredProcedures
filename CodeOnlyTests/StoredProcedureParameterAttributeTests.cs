using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeOnlyStoredProcedure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#if NET40
namespace CodeOnlyTests.Net40
#else
namespace CodeOnlyTests
#endif
{
    [TestClass]
    public class StoredProcedureParameterAttributeTests
    {
        [TestMethod]
        public void ParameterUsedInCreateSqlParameter()
        {
            var toTest = new StoredProcedureParameterAttribute();

            var res = toTest.CreateSqlParameter("foo");

            Assert.AreEqual("foo", res.ParameterName);
        }

        [TestMethod]
        public void DirectionUsedInCreateSqlParameter()
        {
            var toTest = new StoredProcedureParameterAttribute { Direction = ParameterDirection.ReturnValue };

            var res = toTest.CreateSqlParameter("foo");

            Assert.AreEqual(ParameterDirection.ReturnValue, res.Direction);
        }

        [TestMethod]
        public void ScaleUsedInCreateSqlParameter()
        {
            var toTest = new StoredProcedureParameterAttribute { Scale = 15 };

            var res = toTest.CreateSqlParameter("foo");

            Assert.AreEqual(15, res.Scale);
        }

        [TestMethod]
        public void PrecisionUsedInCreateSqlParameter()
        {
            var toTest = new StoredProcedureParameterAttribute { Precision = 42 };

            var res = toTest.CreateSqlParameter("foo");

            Assert.AreEqual(42, res.Precision);
        }

        [TestMethod]
        public void SizeUsedInCreateSqlParameter()
        {
            var toTest = new StoredProcedureParameterAttribute { Size = 33 };

            var res = toTest.CreateSqlParameter("foo");

            Assert.AreEqual(33, res.Size);
        }

        [TestMethod]
        public void SqlDbTypeUsedInCreateSqlParameter()
        {
            var toTest = new StoredProcedureParameterAttribute { SqlDbType = SqlDbType.Char };

            var res = toTest.CreateSqlParameter("foo");

            Assert.AreEqual(SqlDbType.Char, res.SqlDbType);
        }
    }
}
