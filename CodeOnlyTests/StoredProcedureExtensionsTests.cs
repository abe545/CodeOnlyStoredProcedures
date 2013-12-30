using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CodeOnlyStoredProcedure;
using System.Data;
using System.Collections.Generic;
using Microsoft.SqlServer.Server;

namespace CodeOnlyTests
{
    [TestClass]
    public class StoredProcedureExtensionsTests
    {
        #region WithParameter Tests
        [TestMethod]
        public void TestWithParameterAddsParameterToNewStoredProcedure()
        {
            var orig = new StoredProcedure("Test");

            var toTest = orig.WithParameter("Foo", "Bar");

            Assert.IsFalse(ReferenceEquals(orig, toTest));
            Assert.AreEqual(0, orig.Parameters.Count());
            Assert.AreEqual(1, toTest.Parameters.Count());

            var p = toTest.Parameters.Single();
            Assert.AreEqual("Foo", p.ParameterName);
            Assert.AreEqual("Bar", p.Value);
            Assert.AreEqual(ParameterDirection.Input, p.Direction);
        }

        [TestMethod]
        public void TestWithParamaterAndSqlTypeAddsParameterToNewStoredProcedure()
        {
            var orig = new StoredProcedure("Test");

            var toTest = orig.WithParameter("Foo", "Bar", SqlDbType.NChar);

            Assert.IsFalse(ReferenceEquals(orig, toTest));
            Assert.AreEqual(0, orig.Parameters.Count());
            Assert.AreEqual(1, toTest.Parameters.Count());

            var p = toTest.Parameters.Single();
            Assert.AreEqual("Foo", p.ParameterName);
            Assert.AreEqual("Bar", p.Value);
            Assert.AreEqual(ParameterDirection.Input, p.Direction);
            Assert.AreEqual(SqlDbType.NChar, p.SqlDbType);
        }

        [TestMethod]
        public void TestWithParameterClonesStoredProcedureWithResultType()
        {
            var orig = new StoredProcedure<int>("Test");

            var toTest = orig.WithParameter("Foo", "Bar");

            Assert.AreEqual(typeof(StoredProcedure<int>), toTest.GetType());
            Assert.IsFalse(ReferenceEquals(orig, toTest));
            Assert.AreEqual(0, orig.Parameters.Count());
            Assert.AreEqual(1, toTest.Parameters.Count());

            var p = toTest.Parameters.Single();
            Assert.AreEqual(ParameterDirection.Input, p.Direction);
            Assert.AreEqual("Foo", p.ParameterName);
            Assert.AreEqual("Bar", p.Value);
        }
        #endregion

        #region WithOutputParameter Tests
        [TestMethod]
        public void TestWithOutputParameterAddsParameterAndSetter()
        {
            var sp = new StoredProcedure("Test");

            string set = null;
            var toTest = sp.WithOutputParameter<StoredProcedure, string>("Foo", s => set = s);

            Assert.IsFalse(ReferenceEquals(sp, toTest));
            Assert.AreEqual(0, sp.Parameters.Count());
            Assert.AreEqual(0, sp.OutputParameterSetters.Count());

            var p = toTest.Parameters.Single();
            Assert.AreEqual(ParameterDirection.Output, p.Direction);
            Assert.AreEqual("Foo", p.ParameterName);

            var output = toTest.OutputParameterSetters.Single();
            output.Value("Bar");
            Assert.AreEqual("Bar", set);
        }

        [TestMethod]
        public void TestWithOutputParameterSetsSqlDbType()
        {
            var sp = new StoredProcedure("Test");

            int set = 0;
            var toTest = sp.WithOutputParameter<StoredProcedure, int>("Foo", s => set = s, SqlDbType.Int);

            Assert.IsFalse(ReferenceEquals(sp, toTest));
            Assert.AreEqual(0, sp.Parameters.Count());
            Assert.AreEqual(0, sp.OutputParameterSetters.Count());

            var p = toTest.Parameters.Single();
            Assert.AreEqual(ParameterDirection.Output, p.Direction);
            Assert.AreEqual("Foo", p.ParameterName);
            Assert.AreEqual(SqlDbType.Int, p.SqlDbType);

            var output = toTest.OutputParameterSetters.Single();
            output.Value(42);
            Assert.AreEqual(42, set);
        }

        [TestMethod]
        public void TestWithOutputParameterSetsSize()
        {
            var sp = new StoredProcedure("Test");

            string set = null;
            var toTest = sp.WithOutputParameter<StoredProcedure, string>("Foo", s => set = s, size: 10);

            Assert.IsFalse(ReferenceEquals(sp, toTest));
            Assert.AreEqual(0, sp.Parameters.Count());
            Assert.AreEqual(0, sp.OutputParameterSetters.Count());

            var p = toTest.Parameters.Single();
            Assert.AreEqual(ParameterDirection.Output, p.Direction);
            Assert.AreEqual("Foo", p.ParameterName);
            Assert.AreEqual(10, p.Size);

            var output = toTest.OutputParameterSetters.Single();
            output.Value("Bar");
            Assert.AreEqual("Bar", set);
        }

        [TestMethod]
        public void TestWithOutputParameterAndSqlDbTypeSetsSize()
        {
            var sp = new StoredProcedure("Test");

            string set = null;
            var toTest = sp.WithOutputParameter<StoredProcedure, string>("Foo", s => set = s, SqlDbType.NVarChar, size: 10);

            Assert.IsFalse(ReferenceEquals(sp, toTest));
            Assert.AreEqual(0, sp.Parameters.Count());
            Assert.AreEqual(0, sp.OutputParameterSetters.Count());

            var p = toTest.Parameters.Single();
            Assert.AreEqual(ParameterDirection.Output, p.Direction);
            Assert.AreEqual("Foo", p.ParameterName);
            Assert.AreEqual(SqlDbType.NVarChar, p.SqlDbType);
            Assert.AreEqual(10, p.Size);

            var output = toTest.OutputParameterSetters.Single();
            output.Value("Bar");
            Assert.AreEqual("Bar", set);
        }

        [TestMethod]
        public void TestWithOutputParameterSetsScale()
        {
            var sp = new StoredProcedure("Test");

            decimal set = 0;
            var toTest = sp.WithOutputParameter<StoredProcedure, decimal>("Foo", d => set = d, scale: 4);

            Assert.IsFalse(ReferenceEquals(sp, toTest));
            Assert.AreEqual(0, sp.Parameters.Count());
            Assert.AreEqual(0, sp.OutputParameterSetters.Count());

            var p = toTest.Parameters.Single();
            Assert.AreEqual(ParameterDirection.Output, p.Direction);
            Assert.AreEqual("Foo", p.ParameterName);
            Assert.AreEqual(4, p.Scale);

            var output = toTest.OutputParameterSetters.Single();
            output.Value(142.13M);
            Assert.AreEqual(142.13M, set);
        }

        [TestMethod]
        public void TestWithOutputParameterAndSqlDbTypeSetsScale()
        {
            var sp = new StoredProcedure("Test");
            
            decimal set = 0;
            var toTest = sp.WithOutputParameter<StoredProcedure, decimal>("Foo", d => set = d, SqlDbType.Decimal, scale: 4);

            Assert.IsFalse(ReferenceEquals(sp, toTest));
            Assert.AreEqual(0, sp.Parameters.Count());
            Assert.AreEqual(0, sp.OutputParameterSetters.Count());

            var p = toTest.Parameters.Single();
            Assert.AreEqual(ParameterDirection.Output, p.Direction);
            Assert.AreEqual("Foo", p.ParameterName);
            Assert.AreEqual(4, p.Scale);
            Assert.AreEqual(SqlDbType.Decimal, p.SqlDbType);

            var output = toTest.OutputParameterSetters.Single();
            output.Value(12.37M);
            Assert.AreEqual(12.37M, set);
        }

        [TestMethod]
        public void TestWithOutputParameterSetsPrecision()
        {
            var sp = new StoredProcedure("Test");

            decimal set = 0;
            var toTest = sp.WithOutputParameter<StoredProcedure, decimal>("Foo", d => set = d, precision: 11);

            Assert.IsFalse(ReferenceEquals(sp, toTest));
            Assert.AreEqual(0, sp.Parameters.Count());
            Assert.AreEqual(0, sp.OutputParameterSetters.Count());

            var p = toTest.Parameters.Single();
            Assert.AreEqual(ParameterDirection.Output, p.Direction);
            Assert.AreEqual("Foo", p.ParameterName);
            Assert.AreEqual(11, p.Precision);

            var output = toTest.OutputParameterSetters.Single();
            output.Value(142.13M);
            Assert.AreEqual(142.13M, set);
        }

        [TestMethod]
        public void TestWithOutputParameterAndSqlDbTypeSetsPrecision()
        {
            var sp = new StoredProcedure("Test");

            decimal set = 0;
            var toTest = sp.WithOutputParameter<StoredProcedure, decimal>("Foo", d => set = d, SqlDbType.Decimal, precision: 11);

            Assert.IsFalse(ReferenceEquals(sp, toTest));
            Assert.AreEqual(0, sp.Parameters.Count());
            Assert.AreEqual(0, sp.OutputParameterSetters.Count());

            var p = toTest.Parameters.Single();
            Assert.AreEqual("Foo", p.ParameterName);
            Assert.AreEqual(11, p.Precision);
            Assert.AreEqual(SqlDbType.Decimal, p.SqlDbType);

            var output = toTest.OutputParameterSetters.Single();
            output.Value(12.37M);
            Assert.AreEqual(12.37M, set);
        }
        #endregion

        #region WithInputOutputParameter Tests
        [TestMethod]
        public void TestWithInputOutputParameterHasInputAndSetsOutput()
        {
            var sp = new StoredProcedure("Test");

            string set = null;
            var toTest = sp.WithInputOutputParameter("Foo", "Bar", s => set = s);

            Assert.IsFalse(ReferenceEquals(sp, toTest));
            Assert.AreEqual(0, sp.Parameters.Count());
            Assert.AreEqual(0, sp.OutputParameterSetters.Count());

            var p = toTest.Parameters.Single();
            Assert.AreEqual(ParameterDirection.InputOutput, p.Direction);
            Assert.AreEqual("Bar", p.Value);
            Assert.AreEqual("Foo", p.ParameterName);

            var output = toTest.OutputParameterSetters.Single();
            output.Value("Bar");
            Assert.AreEqual("Bar", set);
        }

        [TestMethod]
        public void TestWithInputOutputParameterAndSqlDbTypeHasInputAndSetsOutput()
        {
            var sp = new StoredProcedure("Test");

            string set = null;
            var toTest = sp.WithInputOutputParameter("Foo", "Bar", s => set = s, SqlDbType.NVarChar);

            Assert.IsFalse(ReferenceEquals(sp, toTest));
            Assert.AreEqual(0, sp.Parameters.Count());
            Assert.AreEqual(0, sp.OutputParameterSetters.Count());

            var p = toTest.Parameters.Single();
            Assert.AreEqual(ParameterDirection.InputOutput, p.Direction);
            Assert.AreEqual("Bar", p.Value);
            Assert.AreEqual("Foo", p.ParameterName);
            Assert.AreEqual(SqlDbType.NVarChar, p.SqlDbType);

            var output = toTest.OutputParameterSetters.Single();
            output.Value("Bar");
            Assert.AreEqual("Bar", set);
        }


        [TestMethod]
        public void TestWithInputOutputParameterSetsSize()
        {
            var sp = new StoredProcedure("Test");

            string set = null;
            var toTest = sp.WithInputOutputParameter("Foo", "Baz", s => set = s, size: 10);

            Assert.IsFalse(ReferenceEquals(sp, toTest));
            Assert.AreEqual(0, sp.Parameters.Count());
            Assert.AreEqual(0, sp.OutputParameterSetters.Count());

            var p = toTest.Parameters.Single();
            Assert.AreEqual(ParameterDirection.InputOutput, p.Direction);
            Assert.AreEqual("Foo", p.ParameterName);
            Assert.AreEqual(10, p.Size);
            Assert.AreEqual("Baz", p.Value);

            var output = toTest.OutputParameterSetters.Single();
            output.Value("Bar");
            Assert.AreEqual("Bar", set);
        }

        [TestMethod]
        public void TestWithInputOutputParameterAndSqlDbTypeSetsSize()
        {
            var sp = new StoredProcedure("Test");

            string set = null;
            var toTest = sp.WithInputOutputParameter("Foo", "Fab", s => set = s, SqlDbType.NVarChar, size: 10);

            Assert.IsFalse(ReferenceEquals(sp, toTest));
            Assert.AreEqual(0, sp.Parameters.Count());
            Assert.AreEqual(0, sp.OutputParameterSetters.Count());

            var p = toTest.Parameters.Single();
            Assert.AreEqual(ParameterDirection.InputOutput, p.Direction);
            Assert.AreEqual("Foo", p.ParameterName);
            Assert.AreEqual(SqlDbType.NVarChar, p.SqlDbType);
            Assert.AreEqual(10, p.Size);
            Assert.AreEqual("Fab", p.Value);

            var output = toTest.OutputParameterSetters.Single();
            output.Value("Bar");
            Assert.AreEqual("Bar", set);
        }

        [TestMethod]
        public void TestWithInputOutputParameterSetsScale()
        {
            var sp = new StoredProcedure("Test");

            decimal set = 0;
            var toTest = sp.WithInputOutputParameter("Foo", 99M, d => set = d, scale: 4);

            Assert.IsFalse(ReferenceEquals(sp, toTest));
            Assert.AreEqual(0, sp.Parameters.Count());
            Assert.AreEqual(0, sp.OutputParameterSetters.Count());

            var p = toTest.Parameters.Single();
            Assert.AreEqual(ParameterDirection.InputOutput, p.Direction);
            Assert.AreEqual("Foo", p.ParameterName);
            Assert.AreEqual(4, p.Scale);
            Assert.AreEqual(99M, p.Value);

            var output = toTest.OutputParameterSetters.Single();
            output.Value(142.13M);
            Assert.AreEqual(142.13M, set);
        }

        [TestMethod]
        public void TestWithInputOutputParameterAndSqlDbTypeSetsScale()
        {
            var sp = new StoredProcedure("Test");

            decimal set = 0;
            var toTest = sp.WithInputOutputParameter("Foo", 100M, d => set = d, SqlDbType.Decimal, scale: 4);

            Assert.IsFalse(ReferenceEquals(sp, toTest));
            Assert.AreEqual(0, sp.Parameters.Count());
            Assert.AreEqual(0, sp.OutputParameterSetters.Count());

            var p = toTest.Parameters.Single();
            Assert.AreEqual(ParameterDirection.InputOutput, p.Direction);
            Assert.AreEqual("Foo", p.ParameterName);
            Assert.AreEqual(4, p.Scale);
            Assert.AreEqual(100M, p.Value);
            Assert.AreEqual(SqlDbType.Decimal, p.SqlDbType);

            var output = toTest.OutputParameterSetters.Single();
            output.Value(12.37M);
            Assert.AreEqual(12.37M, set);
        }

        [TestMethod]
        public void TestWithInputOutputParameterSetsPrecision()
        {
            var sp = new StoredProcedure("Test");

            decimal set = 0;
            var toTest = sp.WithInputOutputParameter("Foo", 10M, d => set = d, precision: 11);

            Assert.IsFalse(ReferenceEquals(sp, toTest));
            Assert.AreEqual(0, sp.Parameters.Count());
            Assert.AreEqual(0, sp.OutputParameterSetters.Count());

            var p = toTest.Parameters.Single();
            Assert.AreEqual(ParameterDirection.InputOutput, p.Direction);
            Assert.AreEqual("Foo", p.ParameterName);
            Assert.AreEqual(11, p.Precision);
            Assert.AreEqual(10M, p.Value);

            var output = toTest.OutputParameterSetters.Single();
            output.Value(142.13M);
            Assert.AreEqual(142.13M, set);
        }

        [TestMethod]
        public void TestWithInputOutputParameterAndSqlDbTypeSetsPrecision()
        {
            var sp = new StoredProcedure("Test");

            decimal set = 0;
            var toTest = sp.WithInputOutputParameter("Foo", 13M, d => set = d, SqlDbType.Decimal, precision: 11);

            Assert.IsFalse(ReferenceEquals(sp, toTest));
            Assert.AreEqual(0, sp.Parameters.Count());
            Assert.AreEqual(0, sp.OutputParameterSetters.Count());

            var p = toTest.Parameters.Single();
            Assert.AreEqual("Foo", p.ParameterName);
            Assert.AreEqual(ParameterDirection.InputOutput, p.Direction);
            Assert.AreEqual(11, p.Precision);
            Assert.AreEqual(13M, p.Value);
            Assert.AreEqual(SqlDbType.Decimal, p.SqlDbType);

            var output = toTest.OutputParameterSetters.Single();
            output.Value(12.37M);
            Assert.AreEqual(12.37M, set);
        }
        #endregion

        #region WithReturnValue Tests
        [TestMethod]
        public void TestWithReturnValueAddsParameterAndOutputSetter()
        {
            var orig = new StoredProcedure("Test");

            int rv = 0;
            var toTest = orig.WithReturnValue(i => rv = i);

            Assert.IsFalse(ReferenceEquals(orig, toTest));
            Assert.AreEqual(0, orig.Parameters.Count());
            Assert.AreEqual(0, orig.OutputParameterSetters.Count());

            var p = toTest.Parameters.Single();
            Assert.AreEqual(ParameterDirection.ReturnValue, p.Direction);

            var act = toTest.OutputParameterSetters.Single();
            act.Value(100);

            Assert.AreEqual(100, rv);
        }
        #endregion

        #region WithInput Tests
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
        #endregion

        #region WithTableValuedParameter Tests
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
        #endregion

        private class WithNamedParameter
        {
            [StoredProcedureParameter(Name = "InputName")]
            public string Foo { get; set; }
        }

        private class WithOutput
        {
            [StoredProcedureParameter(Direction = ParameterDirection.Output)]
            public string Value { get; set; }
        }

        private class WithInputOutput
        {
            [StoredProcedureParameter(Direction = ParameterDirection.InputOutput)]
            public decimal Value { get; set; }
        }

        private class WithReturnValue
        {
            [StoredProcedureParameter(Direction = ParameterDirection.ReturnValue)]
            public int ReturnValue { get; set; }
        }

        private class WithTableValuedParameter
        {
            [TableValuedParameter(Schema = "TEST", TableName = "TVP_TEST")]
            public IEnumerable<TVPHelper> Table { get; set; }
        }

        private class TVPHelper
        {
            public string Name { get; set; }
            public int Foo { get; set; }
            public decimal Bar { get; set; }
        }
    }
}
