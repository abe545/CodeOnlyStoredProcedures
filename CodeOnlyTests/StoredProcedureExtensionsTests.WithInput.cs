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
        public void TestWithInputParsesAnonymousType()
        {
            var sp = new StoredProcedure("Test");

            var toTest = sp.WithInput(new
            {
                Id = 1,
                Name = "Foo"
            });

            Assert.IsFalse(ReferenceEquals(sp, toTest));
            Assert.AreEqual(0, sp.Parameters.Count());
            Assert.AreEqual(0, sp.OutputParameterSetters.Count);
            Assert.AreEqual(2, toTest.Parameters.Count());
            Assert.AreEqual(0, toTest.OutputParameterSetters.Count);

            var p = toTest.Parameters.First();
            Assert.AreEqual("Id", p.ParameterName);
            Assert.AreEqual(1, p.Value);
            Assert.AreEqual(ParameterDirection.Input, p.Direction);

            p = toTest.Parameters.Last();
            Assert.AreEqual("Name", p.ParameterName);
            Assert.AreEqual("Foo", p.Value);
            Assert.AreEqual(ParameterDirection.Input, p.Direction);
        }

        [TestMethod]
        public void TestWithInputUsesParameterName()
        {
            var sp = new StoredProcedure("Test");

            var input = new WithNamedParameter { Foo = "Bar" };
            var toTest = sp.WithInput(input);

            Assert.IsFalse(ReferenceEquals(sp, toTest));
            Assert.AreEqual(0, sp.Parameters.Count());
            Assert.AreEqual(0, sp.OutputParameterSetters.Count);
            Assert.AreEqual(1, toTest.Parameters.Count());
            Assert.AreEqual(0, toTest.OutputParameterSetters.Count);

            var p = toTest.Parameters.First();
            Assert.AreEqual("InputName", p.ParameterName);
            Assert.AreEqual("Bar", p.Value);
            Assert.AreEqual(ParameterDirection.Input, p.Direction);
        }

        [TestMethod]
        public void TestWithInputAddsOutputTypes()
        {
            var sp = new StoredProcedure("Test");

            var output = new WithOutput();
            var toTest = sp.WithInput(output);

            Assert.IsFalse(ReferenceEquals(sp, toTest));
            Assert.AreEqual(0, sp.Parameters.Count());
            Assert.AreEqual(0, sp.OutputParameterSetters.Count);

            var p = toTest.Parameters.Single();
            Assert.AreEqual("Value", p.ParameterName);
            Assert.AreEqual(ParameterDirection.Output, p.Direction);

            var setter = toTest.OutputParameterSetters.Single();
            setter.Value("Foo");
            Assert.AreEqual("Foo", output.Value);
        }

        [TestMethod]
        public void TestWithInputAddsInputOutputTypes()
        {
            var sp = new StoredProcedure("Test");

            var inputOutput = new WithInputOutput { Value = 123M };
            var toTest = sp.WithInput(inputOutput);

            Assert.IsFalse(ReferenceEquals(sp, toTest));
            Assert.AreEqual(0, sp.Parameters.Count());
            Assert.AreEqual(0, sp.OutputParameterSetters.Count);

            var p = toTest.Parameters.Single();
            Assert.AreEqual("Value", p.ParameterName);
            Assert.AreEqual(123M, p.Value);
            Assert.AreEqual(ParameterDirection.InputOutput, p.Direction);

            var setter = toTest.OutputParameterSetters.Single();
            setter.Value(99M);
            Assert.AreEqual(99M, inputOutput.Value);
        }

        [TestMethod]
        public void TestWithInputAddsReturnValue()
        {
            var sp = new StoredProcedure("Test");

            var retVal = new WithReturnValue();
            var toTest = sp.WithInput(retVal);

            Assert.IsFalse(ReferenceEquals(sp, toTest));
            Assert.AreEqual(0, sp.Parameters.Count());
            Assert.AreEqual(0, sp.OutputParameterSetters.Count);

            var p = toTest.Parameters.Single();
            Assert.AreEqual("ReturnValue", p.ParameterName);
            Assert.AreEqual(ParameterDirection.ReturnValue, p.Direction);

            var setter = toTest.OutputParameterSetters.Single();
            setter.Value(10);
            Assert.AreEqual(10, retVal.ReturnValue);
        }

        [TestMethod]
        public void TestWithInputSendsTableValuedParameter()
        {
            var sp = new StoredProcedure("Test");

            var input = new WithTableValuedParameter
            {
                Table = new List<TVPHelper>
                {
                    new TVPHelper { Name = "Hello", Foo = 0, Bar = 100M },
                    new TVPHelper { Name = "World", Foo = 3, Bar = 331M }
                }
            };

            var toTest = sp.WithInput(input);

            Assert.IsFalse(ReferenceEquals(sp, toTest));
            Assert.AreEqual(0, sp.Parameters.Count());
            Assert.AreEqual(0, sp.OutputParameterSetters.Count);

            Assert.AreEqual(0, toTest.OutputParameterSetters.Count);
            var p = toTest.Parameters.Single();
            Assert.AreEqual(SqlDbType.Structured, p.SqlDbType);
            Assert.AreEqual("Table", p.ParameterName);
            Assert.AreEqual("[TEST].[TVP_TEST]", p.TypeName);

            int i = 0;
            foreach (var record in (IEnumerable<SqlDataRecord>)p.Value)
            {
                var item = input.Table.ElementAt(i);
                Assert.AreEqual("Name", record.GetName(0));
                Assert.AreEqual(item.Name, record.GetString(0));

                Assert.AreEqual("Foo", record.GetName(1));
                Assert.AreEqual(item.Foo, record.GetInt32(1));

                Assert.AreEqual("Bar", record.GetName(2));
                Assert.AreEqual(item.Bar, record.GetDecimal(2));

                ++i;
            }
        }
    }
}
