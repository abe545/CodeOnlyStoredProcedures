using System;
using System.Data;
using CodeOnlyStoredProcedure;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#if NET40
namespace CodeOnlyTests.Net40.StoredProcedureParameters
#else
namespace CodeOnlyTests.StoredProcedureParameters
#endif
{
    [TestClass]
    public class InputParameterTests : ParameterTestBase
    {
        [TestMethod]
        public void SetsConstructorValuesOnParameter()
        {
            var toTest = new InputParameter("Foo", 123, DbType.Int32);

            var res = toTest.CreateDbDataParameter(CreateCommand());

            res.DbType       .Should().Be(DbType.Int32,             "it was passed in the constructor");
            res.ParameterName.Should().Be("Foo",                    "it was passed in the constructor");
            res.Value        .Should().Be(123,                      "it was passed in the constructor");
            res.Direction    .Should().Be(ParameterDirection.Input, "it is an input parameter");
        }

        [TestMethod]
        public void DbTypeInferredWhenNotSet()
        {
            var toTest = new InputParameter("Foo", 123M);

            var res = toTest.CreateDbDataParameter(CreateCommand());

            res.DbType       .Should().Be(DbType.Decimal,           "it should have been inferred");
            res.ParameterName.Should().Be("Foo",                    "it was passed in the constructor");
            res.Value        .Should().Be(123M,                     "it was passed in the constructor");
            res.Direction    .Should().Be(ParameterDirection.Input, "it is an input parameter");
        }

        [TestMethod]
        public void SetsDbNullWhenNullableValueIsNull()
        {
            var toTest = new InputParameter("Foo", default(int?), DbType.Int32);

            var res = toTest.CreateDbDataParameter(CreateCommand());

            res.DbType       .Should().Be(DbType.Int32,             "it was passed in the constructor");
            res.ParameterName.Should().Be("Foo",                    "it was passed in the constructor");
            res.Value        .Should().Be(DBNull.Value,             "DBNull.Value should be used for null values");
            res.Direction    .Should().Be(ParameterDirection.Input, "it is an input parameter");
        }

        [TestMethod]
        public void SetsDbNullWhenStringValueIsNull()
        {
            var toTest = new InputParameter("Foo", default(string), DbType.String);

            var res = toTest.CreateDbDataParameter(CreateCommand());

            res.DbType       .Should().Be(DbType.String,            "it was passed in the constructor");
            res.ParameterName.Should().Be("Foo",                    "it was passed in the constructor");
            res.Value        .Should().Be(DBNull.Value,             "DBNull.Value should be used for null values");
            res.Direction    .Should().Be(ParameterDirection.Input, "it is an input parameter");
        }

        [TestMethod]
        public void ToStringRepresentsTheParameter()
        {
            new InputParameter("Foo", "Bar").ToString().Should().Be("@Foo = 'Bar'");
        }

        [TestMethod]
        public void NullValueToStringReturnsCorrectString()
        {
            new InputParameter("Foo", null).ToString().Should().Be("@Foo = '{null}'");
        }
    }
}
