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
    public class InputOutputParameterTests : ParameterTestBase
    {
        [TestMethod]
        public void SetsConstructorValuesOnParameter()
        {
            var toTest = new InputOutputParameter("Foo", o => { }, 123, DbType.Int32, 42, 31, 11);

            var res = toTest.CreateDbDataParameter(CreateCommand());

            res.DbType       .Should().Be(DbType.Int32,                   "it was passed in the constructor");
            res.ParameterName.Should().Be("Foo",                          "it was passed in the constructor");
            res.Value        .Should().Be(123,                            "it was passed in the constructor");
            res.Size         .Should().Be(42,                             "it was passed in the constructor");
            res.Scale        .Should().Be(31,                             "it was passed in the constructor");
            res.Precision    .Should().Be(11,                             "it was passed in the constructor");
            res.Direction    .Should().Be(ParameterDirection.InputOutput, "it is an input/output parameter");
        }

        [TestMethod]
        public void DbTypeInferredWhenNotSet()
        {
            var toTest = new InputOutputParameter("Foo", o => { }, 123M);

            var res = toTest.CreateDbDataParameter(CreateCommand());

            res.DbType       .Should().Be(DbType.Decimal,                 "it should have been inferred");
            res.ParameterName.Should().Be("Foo",                          "it was passed in the constructor");
            res.Value        .Should().Be(123M,                           "it was passed in the constructor");
            res.Direction    .Should().Be(ParameterDirection.InputOutput, "it is an input/output parameter");
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
        public void TransferOutputValueCallsActionPassedInConstructor()
        {
            object outVal = null;
            var toTest = new InputOutputParameter("Foo", o => outVal = o, 42M);

            toTest.TransferOutputValue("Bar");

            outVal.Should().Be("Bar", "it was set by the action passed in the constructor");
        }

        [TestMethod]
        public void ToStringRepresentsTheParameter()
        {
            new InputOutputParameter("Foo", o => { }, "Bar").ToString().Should().Be("[InOut] @Foo = 'Bar'");
        }

        [TestMethod]
        public void NullValueToStringReturnsCorrectString()
        {
            new InputOutputParameter("Foo", o => { }, null).ToString().Should().Be("[InOut] @Foo = '{null}'");
        }

        [TestMethod]
        public void ToStringDoesNotDisplayExtraAts()
        {
            new InputOutputParameter("@Foo", o => { }, "Bar").ToString().Should().Be("[InOut] @Foo = 'Bar'");
        }
    }
}
