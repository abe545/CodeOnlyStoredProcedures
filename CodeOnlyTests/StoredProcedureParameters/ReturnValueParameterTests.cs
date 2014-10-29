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
    public class ReturnValueParameterTests : ParameterTestBase
    {        
        [TestMethod]
        public void SetsConstructorValuesOnParameter()
        {
            var toTest = new ReturnValueParameter(o => { });

            var res = toTest.CreateDbDataParameter(CreateCommand());

            res.DbType   .Should().Be(DbType.Int32,                   "return values are ints");
            res.Value    .Should().Be(null,                           "return value parameters can't have initial values");
            res.Direction.Should().Be(ParameterDirection.ReturnValue, "it is an return value parameter");
        }

        [TestMethod]
        public void TransferReturnValueCallsActionPassedInConstructor()
        {
            int outVal = 0;
            var toTest = new ReturnValueParameter(i => outVal = i);

            toTest.TransferOutputValue(42);

            outVal.Should().Be(42, "it was set by the action passed in the constructor");
        }

        [TestMethod]
        public void ToStringRepresentsTheParameter()
        {
            new ReturnValueParameter(o => { }).ToString().Should().Be("@returnValue");
        }
    }
}
