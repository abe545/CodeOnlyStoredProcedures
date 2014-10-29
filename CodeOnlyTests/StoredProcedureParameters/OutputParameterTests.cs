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
    public class OutputParameterTests : ParameterTestBase
    {        
        [TestMethod]
        public void SetsConstructorValuesOnParameter()
        {
            var toTest = new OutputParameter("Foo", o => { }, DbType.Int16, 42, 31, 11);

            var res = toTest.CreateDbDataParameter(CreateCommand());

            res.DbType       .Should().Be(DbType.Int16,              "it was passed in the constructor");
            res.ParameterName.Should().Be("Foo",                     "it was passed in the constructor");
            res.Value        .Should().Be(null,                      "output parameters can't have initial values");
            res.Size         .Should().Be(42,                        "it was passed in the constructor");
            res.Scale        .Should().Be(31,                        "it was passed in the constructor");
            res.Precision    .Should().Be(11,                        "it was passed in the constructor");
            res.Direction    .Should().Be(ParameterDirection.Output, "it is an output parameter");
        }

        [TestMethod]
        public void TransferOutputValueCallsActionPassedInConstructor()
        {
            object outVal = null;
            var toTest = new OutputParameter("Foo", o => outVal = o);

            toTest.TransferOutputValue("Bar");

            outVal.Should().Be("Bar", "it was set by the action passed in the constructor");
        }

        [TestMethod]
        public void ToStringRepresentsTheParameter()
        {
            new OutputParameter("Foo", o => { }).ToString().Should().Be("[Out] @Foo");
        }
    }
}
