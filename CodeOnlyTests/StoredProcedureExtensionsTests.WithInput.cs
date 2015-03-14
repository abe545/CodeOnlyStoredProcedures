using System.Collections.Generic;
using System.Data;
using CodeOnlyStoredProcedure;
using FluentAssertions;
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
            var orig = new StoredProcedure("Test");

            var toTest = orig.WithInput(new
            {
                Id = 1,
                Name = "Foo"
            });
            
            toTest.Should().NotBeSameAs(orig, "because StoredProcedures should be immutable");
            orig.Parameters.Should().BeEmpty("because StoredProcedures should be immutable");

            toTest.Parameters.Should().HaveCount(2, "because 2 properties were defined on the anonymous type passed to WithInput");

            TestSingleInputParameter(toTest, "Id", 1, DbType.Int32);
            TestSingleInputParameter(toTest, "Name", "Foo", DbType.String);
        }

        [TestMethod]
        public void TestWithInputParsesAnonymousTypesWithStringNullValue()
        {
            var orig = new StoredProcedure("Test");

            var toTest = orig.WithInput(new
            {
                Id = 1,
                Name = "Foo",
                Value = default(string)
            });
            
            toTest.Should().NotBeSameAs(orig, "because StoredProcedures should be immutable");
            orig.Parameters.Should().BeEmpty("because StoredProcedures should be immutable");

            toTest.Parameters.Should().HaveCount(3, "because 3 properties were defined on the anonymous type passed to WithInput");

            TestSingleInputParameter(toTest, "Id", 1, DbType.Int32);
            TestSingleInputParameter(toTest, "Name", "Foo", DbType.String);
            TestSingleInputParameter(toTest, "Value", null, DbType.String);
        }

        [TestMethod]
        public void TestWithInputUsesParameterName()
        {
            var orig = new StoredProcedure("Test");

            var input = new WithNamedParameter { Foo = "Bar" };
            var toTest = orig.WithInput(input);

            toTest.Should().NotBeSameAs(orig, "because StoredProcedures should be immutable");
            orig.Parameters.Should().BeEmpty("because StoredProcedures should be immutable");

            toTest.Parameters.Should().HaveCount(1, "because 1 property is defined on the WithNamedParameter, which was passed to WithInput");

            toTest.Parameters.Should().ContainSingle(p => p.ParameterName == "InputName", "because Foo is decorated with a StoredProcedureAttribute renaming it").Which
                .Should().BeOfType<InputParameter>().Which.Value.Should().Be("Bar");
        }

        [TestMethod]
        public void TestWithInputDoesNotAddNullValues()
        {
            var orig = new StoredProcedure("Test");

            var input = new NullableColumns { Name = "Foo", NullableDouble = null, NullableInt = null };
            var toTest = orig.WithInput(input);

            toTest.Should().NotBeSameAs(orig, "because StoredProcedures should be immutable");
            orig.Parameters.Should().BeEmpty("because StoredProcedures should be immutable");

            toTest.Parameters.Should().HaveCount(3, "because 3 properties are defined on the NullableColumns, which was passed to WithInput");

            TestSingleInputParameter(toTest, "Name", "Foo", DbType.String);
            TestSingleInputParameter(toTest, "NullableDouble", null, DbType.Double);
            TestSingleInputParameter(toTest, "NullableInt", null, DbType.Int32);
        }

        [TestMethod]
        public void TestWithInputAddsOutputTypes()
        {
            var orig = new StoredProcedure("Test");

            var output = new WithOutput();
            var toTest = orig.WithInput(output);

            toTest.Should().NotBeSameAs(orig, "because StoredProcedures should be immutable");
            orig.Parameters.Should().BeEmpty("because StoredProcedures should be immutable");

            toTest.Parameters.Should().HaveCount(1, "because 1 property is defined on the WithOutput, which was passed to WithInput");

            toTest.Parameters.Should().ContainSingle(p => p.ParameterName == "Value").Which
                .Should().BeOfType<OutputParameter>().Which.Invoking(p => p.TransferOutputValue("Frob")).ShouldNotThrow();

            output.Value.Should().Be("Frob", "because it should have been set by the parameter's TransferOutputValue");
        }

        [TestMethod]
        public void TestWithInputAddsInputOutputTypes()
        {
            var orig = new StoredProcedure("Test");

            var inputOutput = new WithInputOutput { Value = 123M };
            var toTest = orig.WithInput(inputOutput);

            toTest.Should().NotBeSameAs(orig, "because StoredProcedures should be immutable");
            orig.Parameters.Should().BeEmpty("because StoredProcedures should be immutable");

            toTest.Parameters.Should().HaveCount(1, "because 1 property is defined on the WithInputOutput, which was passed to WithInput");

            var param = toTest.Parameters.Should().ContainSingle(p => p.ParameterName == "Value").Which
                .Should().BeOfType<InputOutputParameter>().Which;

            param.Value.Should().Be(123M, "because it was set on the in/out parameter before execution");
            param.Invoking(p => p.TransferOutputValue(42M)).ShouldNotThrow();

            inputOutput.Value.Should().Be(42M, "because it should have been set by the parameter's TransferOutputValue");
        }

        [TestMethod]
        public void TestWithInputAddsReturnValue()
        {
            var orig = new StoredProcedure("Test");

            var retVal = new WithReturnValue();
            var toTest = orig.WithInput(retVal);

            toTest.Should().NotBeSameAs(orig, "because StoredProcedures should be immutable");
            orig.Parameters.Should().BeEmpty("because StoredProcedures should be immutable");

            toTest.Parameters.Should().HaveCount(1, "because 1 property is defined on the WithReturnValue, which was passed to WithInput");

            toTest.Parameters.Should().ContainSingle(p => true).Which
                .Should().BeOfType<ReturnValueParameter>().Which.Invoking(p => p.TransferOutputValue(127)).ShouldNotThrow();

            retVal.ReturnValue.Should().Be(127, "because it should have been set when calling TransferOutputValue on the parameter.");
        }

        [TestMethod]
        public void TestWithInputSendsTableValuedParameter()
        {
            var orig = new StoredProcedure("Test");

            var input = new WithTableValuedParameter
            {
                Table = new List<TVPHelper>
                {
                    new TVPHelper { Name = "Hello", Foo = 0, Bar = 100M },
                    new TVPHelper { Name = "World", Foo = 3, Bar = 331M }
                }
            };

            var toTest = orig.WithInput(input);

            toTest.Should().NotBeSameAs(orig, "because StoredProcedures should be immutable");
            orig.Parameters.Should().BeEmpty("because StoredProcedures should be immutable");

            toTest.Parameters.Should().HaveCount(1, "because 1 property is defined on the WithTableValuedParameter, which was passed to WithInput");

            var param = toTest.Parameters.Should().ContainSingle(p => p.ParameterName == "Table").Which
                .Should().BeOfType<TableValuedParameter>().Which;

            param.Value.Should().BeOfType<List<TVPHelper>>().Which.Should().BeEquivalentTo(input.Table);
            param.TypeName.Should().Be("[TEST].[TVP_TEST]", "because that is the schema and table type specified on WithTableValuedParameter");
        }

        private static void TestSingleInputParameter(StoredProcedure toTest, string parameterName, object expectedValue, DbType expectedType)
        {
            var param = toTest.Parameters.Should().ContainSingle(p => p.ParameterName == parameterName).Which
                .Should().BeOfType<InputParameter>().Which;

            param.Value.Should().Be(expectedValue);
            param.DbType.Should().Be(expectedType);
        }
    }
}
