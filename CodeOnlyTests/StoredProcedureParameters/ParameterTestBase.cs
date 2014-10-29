using System.Data;
using Moq;

#if NET40
namespace CodeOnlyTests.Net40.StoredProcedureParameters
#else
namespace CodeOnlyTests.StoredProcedureParameters
#endif
{
    public class ParameterTestBase
    {
        protected IDbCommand CreateCommand()
        {
            var cmd = new Mock<IDbCommand>();
            cmd.Setup(c => c.CreateParameter())
               .Returns(() =>
               {
                   var p = new Mock<IDbDataParameter>();
                   p.SetupAllProperties();

                   return p.Object;
               });

            return cmd.Object;
        }
    }
}
