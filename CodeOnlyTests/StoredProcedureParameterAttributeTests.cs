using System.Data;
using CodeOnlyStoredProcedure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;


#if NET40
namespace CodeOnlyTests.Net40
#else
namespace CodeOnlyTests
#endif
{
    [TestClass]
    public class StoredProcedureParameterAttributeTests
    {
        private readonly IDbCommand command;

        public StoredProcedureParameterAttributeTests()
        {
            var mock = new Mock<IDbCommand>();

            mock.Setup(c => c.CreateParameter())
                .Returns(() =>
                {
                    var m = new Mock<IDbDataParameter>();
                    m.SetupAllProperties();

                    return m.Object;
                });

            command = mock.Object;
        }

        [TestMethod]
        public void ParameterUsedInCreateSqlParameter()
        {
            var toTest = new StoredProcedureParameterAttribute();

            var res = toTest.CreateDataParameter("foo", command, typeof(string));

            Assert.AreEqual("foo", res.ParameterName);
        }

        [TestMethod]
        public void DirectionUsedInCreateSqlParameter()
        {
            var toTest = new StoredProcedureParameterAttribute { Direction = ParameterDirection.ReturnValue };

            var res = toTest.CreateDataParameter("foo", command, typeof(int));

            Assert.AreEqual(ParameterDirection.ReturnValue, res.Direction);
        }

        [TestMethod]
        public void ScaleUsedInCreateSqlParameter()
        {
            var toTest = new StoredProcedureParameterAttribute { Scale = 15 };

            var res = toTest.CreateDataParameter("foo", command, typeof(decimal));

            Assert.AreEqual(15, res.Scale);
        }

        [TestMethod]
        public void PrecisionUsedInCreateSqlParameter()
        {
            var toTest = new StoredProcedureParameterAttribute { Precision = 42 };

            var res = toTest.CreateDataParameter("foo", command, typeof(decimal));

            Assert.AreEqual(42, res.Precision);
        }

        [TestMethod]
        public void SizeUsedInCreateSqlParameter()
        {
            var toTest = new StoredProcedureParameterAttribute { Size = 33 };

            var res = toTest.CreateDataParameter("foo", command, typeof(string));

            Assert.AreEqual(33, res.Size);
        }

        [TestMethod]
        public void DbTypeUsedInCreateSqlParameter()
        {
            var toTest = new StoredProcedureParameterAttribute { DbType = DbType.String };

            var res = toTest.CreateDataParameter("foo", command, typeof(string));

            Assert.AreEqual(DbType.String, res.DbType);
        }
    }
}
