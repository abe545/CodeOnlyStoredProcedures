using CodeOnlyStoredProcedure;
using Microsoft.SqlServer.Server;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#if NET40
namespace CodeOnlyTests.Net40
#else
namespace CodeOnlyTests
#endif
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
            Assert.AreEqual(SqlDbType.Int, p.SqlDbType);

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

        [TestMethod]
        public void TestWithTableValuedParameterWithSchemaAddsParameter()
        {
            var sp = new StoredProcedure("Test");

            var tvp = new[]
            {
                new TVPHelper { Name = "Hello", Foo = 0, Bar = 100M },
                new TVPHelper { Name = "World", Foo = 3, Bar = 331M }
            };

            var toTest = sp.WithTableValuedParameter("Bar", tvp, "TVP", "Table Type");

            Assert.IsFalse(ReferenceEquals(sp, toTest));
            Assert.AreEqual(0, sp.Parameters.Count());
            Assert.AreEqual(0, sp.OutputParameterSetters.Count);

            Assert.AreEqual(0, toTest.OutputParameterSetters.Count);
            var p = toTest.Parameters.Single();
            Assert.AreEqual(SqlDbType.Structured, p.SqlDbType);
            Assert.AreEqual("Bar", p.ParameterName);
            Assert.AreEqual("[TVP].[Table Type]", p.TypeName);

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

        #region WithDataTransformerTests
        [TestMethod]
        public void TestWithDataTransformerStoresTransformer()
        {
            var orig = new StoredProcedure("Test");
            var xform = new Mock<IDataTransformer>().Object;

            var toTest = orig.WithDataTransformer(xform);

            Assert.IsFalse(ReferenceEquals(orig, toTest));
            Assert.AreEqual(xform, toTest.DataTransformers.Single());
        }

        [TestMethod]
        public void TestWithDataTransformerAddsTransformersInOrder()
        {
            var orig = new StoredProcedure("Test");
            var x1 = new Mock<IDataTransformer>().Object;
            var x2 = new Mock<IDataTransformer>().Object;

            var toTest = orig.WithDataTransformer(x1).WithDataTransformer(x2);

            Assert.AreEqual(2, toTest.DataTransformers.Count());
            Assert.AreEqual(x1, toTest.DataTransformers.First());
            Assert.AreEqual(x2, toTest.DataTransformers.Last());
        }
        #endregion

        #region DoExecute Tests
        [TestMethod]
        public void TestCreateataReaderCancelsWhenCanceledBeforeExecuting()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();

            var command = new Mock<IDbCommand>();
            command.Setup(d => d.ExecuteReader())
                   .Throws(new Exception("ExecuteReader called after token was canceled"));
            command.SetupAllProperties();

            bool exceptionThrown = false;
            try
            {
                command.Object.DoExecute(c => c.ExecuteReader(), cts.Token);
            }
            catch(OperationCanceledException)
            {
                exceptionThrown = true;
            }
            
            command.Verify(d => d.ExecuteReader(), Times.Never);
            Assert.IsTrue(exceptionThrown, "No TaskCanceledException thrown when token is cancelled");
        }

        [TestMethod]
        public void TestDoExecuteCancelsCommandWhenTokenCanceled()
        {
            var sema    = new SemaphoreSlim(0, 1);
            var command = new Mock<IDbCommand>();

            command.SetupAllProperties();
            command.Setup     (d => d.ExecuteReader())
                   .Callback  (() =>
                               {
                                   sema.Release();
                                   do
                                   {
                                       Thread.Sleep(100);
                                   } while (sema.Wait(100));
                               })
                   .Returns   (() => new Mock<IDataReader>().Object);
            command.Setup     (d => d.Cancel())
                   .Verifiable();

            command.Object.CommandTimeout = 30;

            var cts = new CancellationTokenSource();

            var toTest = Task.Factory.StartNew(() => command.Object.DoExecute(c => c.ExecuteReader(), cts.Token), cts.Token);
            bool isCancelled = false;

            var continuation = 
                toTest.ContinueWith(t => isCancelled = true,
                                    TaskContinuationOptions.OnlyOnCanceled);

            sema.Wait();
            cts.Cancel();

            continuation.Wait();
            sema.Release();
            command.Verify(d => d.Cancel(), Times.Once);
            Assert.IsTrue(isCancelled, "The cancellation was not processed properly");
        }

        [TestMethod]
        public void TestDoExecuteThrowsWhenExecuteReaderThrows()
        {
            var command = new Mock<IDbCommand>();
            command.SetupAllProperties();
            command.Setup (d => d.ExecuteReader())
                   .Throws(new Exception("Test Exception"));

            Exception ex = null;
            try
            {
                var toTest = command.Object.DoExecute(c => c.ExecuteReader(), CancellationToken.None);
            }
            catch (AggregateException a)
            {
                ex = a.InnerException;
            }
            catch (Exception e)
            {
                ex = e;
            }

            Assert.IsNotNull(ex);
            Assert.AreEqual("Test Exception", ex.Message);
        }

        [TestMethod]
        public void TestDoExecuteAbortsCommandAfterTimeoutPassed()
        {
            var cmd = new Mock<IDbCommand>();
            cmd.Setup(c => c.ExecuteReader())
               .Callback(() => Thread.Sleep(2000))
               .Returns(() => new Mock<IDataReader>().Object);
            cmd.SetupAllProperties();
            cmd.Object.CommandTimeout = 1;

            try
            {
                cmd.Object.DoExecute(c => c.ExecuteReader(), CancellationToken.None);
                Assert.Fail("The command was not aborted with a TimeoutException.");
            }
            catch(TimeoutException) { }

            cmd.Verify(c => c.Cancel(), Times.Once());
        }
        #endregion

        #region Execute Tests
        [TestMethod]
        public void TestExecuteCancelsWhenTokenCanceledBeforeExecuting()
        {
            var reader  = new Mock<IDataReader>();
            var command = new Mock<IDbCommand>();

            command.Setup(c => c.ExecuteReader())
                   .Returns(reader.Object);

            var cts = new CancellationTokenSource();
            cts.Cancel();

            bool exceptionThrown = false;
            try
            {
                command.Object.Execute(cts.Token, Enumerable.Empty<Type>(), Enumerable.Empty<IDataTransformer>());
            }
            catch (OperationCanceledException)
            {
                exceptionThrown = true;
            }

            reader.Verify(d => d.Read(), Times.Never);
            Assert.IsTrue(exceptionThrown, "No TaskCanceledException thrown when token is cancelled");
        }

        [TestMethod]
        public void TestExecuteCancelsWhenTokenCanceled()
        {
            var sema    = new SemaphoreSlim(0, 1);
            var reader  = new Mock<IDataReader>();
            var command = new Mock<IDbCommand>();

            reader.Setup(d => d.Read())
                  .Callback(() =>
                  {
                      sema.Release();
                      Thread.Sleep(100);
                  })
                  .Returns(true);

            command.Setup(d => d.ExecuteReader())
                   .Returns(reader.Object);

            var cts = new CancellationTokenSource();

            var toTest = Task.Factory.StartNew(() => command.Object.Execute(cts.Token, new[] { typeof(SingleResultSet) }, Enumerable.Empty<IDataTransformer>()));
            bool exceptionThrown = false;

            var continuation =
                toTest.ContinueWith(t => exceptionThrown = t.Exception.InnerException is OperationCanceledException,
                                    TaskContinuationOptions.OnlyOnFaulted);

            sema.Wait();
            cts.Cancel();

            continuation.Wait();
            Assert.IsTrue(exceptionThrown, "No TaskCanceledException thrown when token is cancelled");
        }

        [TestMethod]
        public void TestExecuteReturnsSingleResultSetOneRow()
        {
            var values = new Dictionary<string, object>
            {
                { "String",  "Hello, World!"           },
                { "Double",  42.0                      },
                { "Decimal", 100M                      },
                { "Int",     99                        },
                { "Long",    1028130L                  },
                { "Date",    new DateTime(1982, 1, 31) }
            };

            var keys = values.Keys.OrderBy(s => s).ToArray();
            var vals = values.OrderBy(kv => kv.Key).Select(kv => kv.Value).ToArray();

            var reader  = new Mock<IDataReader>();
            var command = new Mock<IDbCommand>();

            command.Setup(d => d.ExecuteReader())
                   .Returns(reader.Object);

            reader.SetupGet(r => r.FieldCount)
                  .Returns(6);

            var first = true;
            reader.Setup(r => r.Read())
                  .Returns(() =>
                  {
                      if(first)
                      {
                          first = false;
                          return true;
                      }

                      return false;
                  });

            reader.Setup(r => r.GetName(It.IsAny<int>()))
                  .Returns((int i) => keys[i]);
            reader.Setup(r => r.GetValues(It.IsAny<object[]>()))
                  .Callback((object[] arr) => vals.CopyTo(arr, 0))
                  .Returns(6);

            var results = command.Object.Execute(CancellationToken.None, new[] { typeof(SingleResultSet) }, Enumerable.Empty<IDataTransformer>());

            var toTest = (IList<SingleResultSet>)results[typeof(SingleResultSet)];

            Assert.AreEqual(1, toTest.Count);
            var item = toTest[0];

            Assert.AreEqual("Hello, World!",           item.String);
            Assert.AreEqual(42.0,                      item.Double);
            Assert.AreEqual(100M,                      item.Decimal);
            Assert.AreEqual(99,                        item.Int);
            Assert.AreEqual(1028130L,                  item.Long);
            Assert.AreEqual(new DateTime(1982, 1, 31), item.Date);
        }

        [TestMethod]
        public void TestExecuteHandlesRenamedColumns()
        {
            var reader = new Mock<IDataReader>();
            var command = new Mock<IDbCommand>();

            command.Setup(d => d.ExecuteReader())
                   .Returns(reader.Object);

            reader.SetupGet(r => r.FieldCount)
                  .Returns(1);

            var first = true;
            reader.Setup(r => r.Read())
                  .Returns(() =>
                  {
                      if (first)
                      {
                          first = false;
                          return true;
                      }

                      return false;
                  });

            reader.Setup(r => r.GetName(0))
                  .Returns("MyRenamedColumn");
            reader.Setup(r => r.GetValues(It.IsAny<object[]>()))
                  .Callback((object[] arr) => arr[0] = "Hello, World!")
                  .Returns(1);

            var results = command.Object.Execute(CancellationToken.None, new[] { typeof(RenamedColumn) }, Enumerable.Empty<IDataTransformer>());

            var toTest = (IList<RenamedColumn>)results[typeof(RenamedColumn)];

            Assert.AreEqual(1, toTest.Count);
            var item = toTest[0];

            Assert.AreEqual("Hello, World!", item.Column);
        }

        [TestMethod]
        public void TestExecuteReturnsMultipleRowsInOneResultSet()
        {
            var reader = new Mock<IDataReader>();
            var command = new Mock<IDbCommand>();

            command.Setup(d => d.ExecuteReader())
                   .Returns(reader.Object);

            reader.SetupGet(r => r.FieldCount)
                  .Returns(1);

            var results = new[] { "Hello", ", ", "World!" };

            int index = -1;
            reader.Setup(r => r.Read())
                  .Callback(() => ++index)
                  .Returns(() => index < results.Length);

            reader.Setup(r => r.GetName(0))
                  .Returns("Column");
            reader.Setup(r => r.GetValues(It.IsAny<object[]>()))
                  .Callback((object[] arr) => arr[0] = results[index])
                  .Returns(1);

            var res = command.Object.Execute(CancellationToken.None, new[] { typeof(SingleColumn) }, Enumerable.Empty<IDataTransformer>());

            var toTest = (IList<SingleColumn>)res[typeof(SingleColumn)];

            Assert.AreEqual(3, toTest.Count);

            for (int i = 0; i < results.Length; i++)
            {
                var item = toTest[i];

                Assert.AreEqual(results[i], item.Column);
            }
        }

        [TestMethod]
        public void TestExecuteConvertsDbNullToNullValues()
        {
            var keys = new[] { "Name", "NullableInt", "NullableDouble" };
            var reader = new Mock<IDataReader>();
            var command = new Mock<IDbCommand>();

            command.Setup(d => d.ExecuteReader())
                   .Returns(reader.Object);

            reader.SetupGet(r => r.FieldCount)
                  .Returns(3);

            var first = true;
            reader.Setup(r => r.Read())
                  .Returns(() =>
                  {
                      if (first)
                      {
                          first = false;
                          return true;
                      }

                      return false;
                  });

            reader.Setup(r => r.GetName(It.IsAny<int>()))
                  .Returns((int i) => keys[i]);
            reader.Setup(r => r.GetValues(It.IsAny<object[]>()))
                  .Callback((object[] arr) => arr[0] = arr[1] = arr[2] = DBNull.Value)
                  .Returns(3);

            var results = command.Object.Execute(CancellationToken.None, new[] { typeof(NullableColumns) }, Enumerable.Empty<IDataTransformer>());

            var toTest = (IList<NullableColumns>)results[typeof(NullableColumns)];

            Assert.AreEqual(1, toTest.Count);
            var item = toTest[0];

            Assert.IsNull(item.Name);
            Assert.IsNull(item.NullableDouble);
            Assert.IsNull(item.NullableInt);
        }

        [TestMethod]
        [ExpectedException(typeof(StoredProcedureResultsException))]
        public void TestExecuteThrowsIfMappedColumnDoesNotExistInResultSet()
        {
            var reader = new Mock<IDataReader>();
            var command = new Mock<IDbCommand>();

            command.Setup(d => d.ExecuteReader())
                   .Returns(reader.Object);

            reader.SetupGet(r => r.FieldCount)
                  .Returns(1);

            var first = true;
            reader.Setup(r => r.Read())
                  .Returns(() =>
                  {
                      if (first)
                      {
                          first = false;
                          return true;
                      }

                      return false;
                  });

            reader.Setup(r => r.GetName(It.IsAny<int>()))
                  .Returns("OtherColumnName");
            reader.Setup(r => r.GetValues(It.IsAny<object[]>()))
                  .Callback((object[] arr) => arr[0] = DBNull.Value)
                  .Returns(1);

            command.Object.Execute(CancellationToken.None, new[] { typeof(SingleColumn) }, Enumerable.Empty<IDataTransformer>());
        }

        [TestMethod]
        public void TestExecuteTransformsValueWhenPropertyDecoratedWithTransformer()
        {
            var reader = new Mock<IDataReader>();
            var command = new Mock<IDbCommand>();

            command.Setup(d => d.ExecuteReader())
                   .Returns(reader.Object);

            reader.SetupGet(r => r.FieldCount)
                  .Returns(1);

            var first = true;
            reader.Setup(r => r.Read())
                  .Returns(() =>
                  {
                      if (first)
                      {
                          first = false;
                          return true;
                      }

                      return false;
                  });

            reader.Setup(r => r.GetName(0))
                  .Returns("Name");
            reader.Setup(r => r.GetValues(It.IsAny<object[]>()))
                  .Callback((object[] arr) => arr[0] = "Hello, World!")
                  .Returns(1);

            var results = command.Object.Execute(CancellationToken.None, new[] { typeof(WithStaticValue) }, Enumerable.Empty<IDataTransformer>());

            var toTest = (IList<WithStaticValue>)results[typeof(WithStaticValue)];

            Assert.AreEqual(1, toTest.Count);
            var item = toTest[0];

            Assert.AreEqual("Foobar", item.Name);
        }

        [TestMethod]
        public void TestExecuteTransformsDBNullValueWhenPropertyDecoratedWithTransformer()
        {
            var reader = new Mock<IDataReader>();
            var command = new Mock<IDbCommand>();

            command.Setup(d => d.ExecuteReader())
                   .Returns(reader.Object);

            reader.SetupGet(r => r.FieldCount)
                  .Returns(1);

            var first = true;
            reader.Setup(r => r.Read())
                  .Returns(() =>
                  {
                      if (first)
                      {
                          first = false;
                          return true;
                      }

                      return false;
                  });

            reader.Setup(r => r.GetName(0))
                  .Returns("Name");
            reader.Setup(r => r.GetValues(It.IsAny<object[]>()))
                  .Callback((object[] arr) => arr[0] = DBNull.Value)
                  .Returns(1);

            var results = command.Object.Execute(CancellationToken.None, new[] { typeof(WithStaticValue) }, Enumerable.Empty<IDataTransformer>());

            var toTest = (IList<WithStaticValue>)results[typeof(WithStaticValue)];

            Assert.AreEqual(1, toTest.Count);
            var item = toTest[0];

            Assert.AreEqual("Foobar", item.Name);
        }

        [TestMethod]
        public void TestExecuteTransformsRenamedColumnWhenPropertyDecoratedWithTransformer()
        {
            var reader = new Mock<IDataReader>();
            var command = new Mock<IDbCommand>();

            command.Setup(d => d.ExecuteReader())
                   .Returns(reader.Object);

            reader.SetupGet(r => r.FieldCount)
                  .Returns(1);

            var first = true;
            reader.Setup(r => r.Read())
                  .Returns(() =>
                  {
                      if (first)
                      {
                          first = false;
                          return true;
                      }

                      return false;
                  });

            reader.Setup(r => r.GetName(0))
                  .Returns("MyRenamedColumn");
            reader.Setup(r => r.GetValues(It.IsAny<object[]>()))
                  .Callback((object[] arr) => arr[0] = "Hello, World!")
                  .Returns(1);

            var results = command.Object.Execute(CancellationToken.None, new[] { typeof(RenamedColumnWithStaticValue) }, Enumerable.Empty<IDataTransformer>());

            var toTest = (IList<RenamedColumnWithStaticValue>)results[typeof(RenamedColumnWithStaticValue)];

            Assert.AreEqual(1, toTest.Count);
            var item = toTest[0];

            Assert.AreEqual("Foobar", item.Name);
        }

        [TestMethod]
        public void TestExecuteChainsTransformPropertyDecoratedWithTransformerAttributesInOrder()
        {
            var reader = new Mock<IDataReader>();
            var command = new Mock<IDbCommand>();

            command.Setup(d => d.ExecuteReader())
                   .Returns(reader.Object);

            reader.SetupGet(r => r.FieldCount)
                  .Returns(1);

            var first = true;
            reader.Setup(r => r.Read())
                  .Returns(() =>
                  {
                      if (first)
                      {
                          first = false;
                          return true;
                      }

                      return false;
                  });

            reader.Setup(r => r.GetName(0))
                  .Returns("Name");
            reader.Setup(r => r.GetValues(It.IsAny<object[]>()))
                  .Callback((object[] arr) => arr[0] = "Hello, World!")
                  .Returns(1);

            var results = command.Object.Execute(CancellationToken.None, new[] { typeof(WithStaticValueToUpper) }, Enumerable.Empty<IDataTransformer>());

            var toTest = (IList<WithStaticValueToUpper>)results[typeof(WithStaticValueToUpper)];

            Assert.AreEqual(1, toTest.Count);
            var item = toTest[0];

            Assert.AreEqual("IS UPPER?", item.Name);
        }

        [TestMethod]
        public void TestGlobalDataTransformerTransformsData()
        {
            var reader = new Mock<IDataReader>();
            var command = new Mock<IDbCommand>();

            command.Setup(d => d.ExecuteReader())
                   .Returns(reader.Object);

            reader.SetupGet(r => r.FieldCount)
                  .Returns(1);

            var first = true;
            reader.Setup(r => r.Read())
                  .Returns(() =>
                  {
                      if (first)
                      {
                          first = false;
                          return true;
                      }

                      return false;
                  });

            reader.Setup(r => r.GetName(0))
                  .Returns("Column");
            reader.Setup(r => r.GetValues(It.IsAny<object[]>()))
                  .Callback((object[] arr) => arr[0] = "Hello, World!")
                  .Returns(1);

            var results = command.Object.Execute(CancellationToken.None, 
                                                 new[] { typeof(SingleColumn) },
                                                 new IDataTransformer[] { new StaticTransformer { Result = "Foobar" } });

            var toTest = (IList<SingleColumn>)results[typeof(SingleColumn)];

            Assert.AreEqual(1, toTest.Count);
            var item = toTest[0];

            Assert.AreEqual("Foobar", item.Column);
        }

        [TestMethod]
        public void TestGlobalDataTransformerTransformsDBNullValue()
        {
            var reader = new Mock<IDataReader>();
            var command = new Mock<IDbCommand>();

            command.Setup(d => d.ExecuteReader())
                   .Returns(reader.Object);

            reader.SetupGet(r => r.FieldCount)
                  .Returns(1);

            var first = true;
            reader.Setup(r => r.Read())
                  .Returns(() =>
                  {
                      if (first)
                      {
                          first = false;
                          return true;
                      }

                      return false;
                  });

            reader.Setup(r => r.GetName(0))
                  .Returns("Column");
            reader.Setup(r => r.GetValues(It.IsAny<object[]>()))
                  .Callback((object[] arr) => arr[0] = DBNull.Value)
                  .Returns(1);

            var results = command.Object.Execute(CancellationToken.None,
                                                 new[] { typeof(SingleColumn) },
                                                 new IDataTransformer[] { new StaticTransformer { Result = "Foobar" } });

            var toTest = (IList<SingleColumn>)results[typeof(SingleColumn)];

            Assert.AreEqual(1, toTest.Count);
            var item = toTest[0];

            Assert.AreEqual("Foobar", item.Column);
        }

        [TestMethod]
        public void TestGlobalDataTransformerTransformsDataWithRenamedColumn()
        {
            var reader = new Mock<IDataReader>();
            var command = new Mock<IDbCommand>();

            command.Setup(d => d.ExecuteReader())
                   .Returns(reader.Object);

            reader.SetupGet(r => r.FieldCount)
                  .Returns(1);

            var first = true;
            reader.Setup(r => r.Read())
                  .Returns(() =>
                  {
                      if (first)
                      {
                          first = false;
                          return true;
                      }

                      return false;
                  });

            reader.Setup(r => r.GetName(0))
                  .Returns("MyRenamedColumn");
            reader.Setup(r => r.GetValues(It.IsAny<object[]>()))
                  .Callback((object[] arr) => arr[0] = "Hello, World!")
                  .Returns(1);

            var results = command.Object.Execute(CancellationToken.None,
                                                 new[] { typeof(RenamedColumn) },
                                                 new IDataTransformer[] { new StaticTransformer { Result = "Foobar" } });

            var toTest = (IList<RenamedColumn>)results[typeof(RenamedColumn)];

            Assert.AreEqual(1, toTest.Count);
            var item = toTest[0];

            Assert.AreEqual("Foobar", item.Column);
        }

        [TestMethod]
        public void TestGlobalDataTransformerNotCalledWhenCanNotTransformValue()
        {
            var reader = new Mock<IDataReader>();
            var command = new Mock<IDbCommand>();

            command.Setup(d => d.ExecuteReader())
                   .Returns(reader.Object);

            reader.SetupGet(r => r.FieldCount)
                  .Returns(1);

            var first = true;
            reader.Setup(r => r.Read())
                  .Returns(() =>
                  {
                      if (first)
                      {
                          first = false;
                          return true;
                      }

                      return false;
                  });

            reader.Setup(r => r.GetName(0))
                  .Returns("Column");
            reader.Setup(r => r.GetValues(It.IsAny<object[]>()))
                  .Callback((object[] arr) => arr[0] = "Hello, World!")
                  .Returns(1);

            var results = command.Object.Execute(CancellationToken.None,
                                                 new[] { typeof(SingleColumn) },
                                                 new IDataTransformer[] { new NeverTransformer() });

            var toTest = (IList<SingleColumn>)results[typeof(SingleColumn)];

            Assert.AreEqual(1, toTest.Count);
            var item = toTest[0];

            Assert.AreEqual("Hello, World!", item.Column);
        }

        [TestMethod]
        public void TestGlobalDataTransformerNotCalledWhenCanNotTransformValueWithDBNullValue()
        {
            var reader = new Mock<IDataReader>();
            var command = new Mock<IDbCommand>();

            command.Setup(d => d.ExecuteReader())
                   .Returns(reader.Object);

            reader.SetupGet(r => r.FieldCount)
                  .Returns(1);

            var first = true;
            reader.Setup(r => r.Read())
                  .Returns(() =>
                  {
                      if (first)
                      {
                          first = false;
                          return true;
                      }

                      return false;
                  });

            reader.Setup(r => r.GetName(0))
                  .Returns("Column");
            reader.Setup(r => r.GetValues(It.IsAny<object[]>()))
                  .Callback((object[] arr) => arr[0] = DBNull.Value)
                  .Returns(1);

            var results = command.Object.Execute(CancellationToken.None,
                                                 new[] { typeof(SingleColumn) },
                                                 new IDataTransformer[] { new NeverTransformer() });

            var toTest = (IList<SingleColumn>)results[typeof(SingleColumn)];

            Assert.AreEqual(1, toTest.Count);
            var item = toTest[0];

            Assert.IsNull(item.Column);
        }

        [TestMethod]
        public void TestGlobalDataTransformerNotCalledWhenCanNotTransformValueWithRenamedColumn()
        {
            var reader = new Mock<IDataReader>();
            var command = new Mock<IDbCommand>();

            command.Setup(d => d.ExecuteReader())
                   .Returns(reader.Object);

            reader.SetupGet(r => r.FieldCount)
                  .Returns(1);

            var first = true;
            reader.Setup(r => r.Read())
                  .Returns(() =>
                  {
                      if (first)
                      {
                          first = false;
                          return true;
                      }

                      return false;
                  });

            reader.Setup(r => r.GetName(0))
                  .Returns("MyRenamedColumn");
            reader.Setup(r => r.GetValues(It.IsAny<object[]>()))
                  .Callback((object[] arr) => arr[0] = "Hello, World!")
                  .Returns(1);

            var results = command.Object.Execute(CancellationToken.None,
                                                 new[] { typeof(RenamedColumn) },
                                                 new IDataTransformer[] { new NeverTransformer() });

            var toTest = (IList<RenamedColumn>)results[typeof(RenamedColumn)];

            Assert.AreEqual(1, toTest.Count);
            var item = toTest[0];

            Assert.AreEqual("Hello, World!", item.Column);
        }
        #endregion

        #region Test Helper Classes
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
            public string  Name { get; set; }
            public int     Foo  { get; set; }
            public decimal Bar  { get; set; }
        }

        private class SingleResultSet
        {
            public String   String  { get; set; }
            public Double   Double  { get; set; }
            public Decimal  Decimal { get; set; }
            public Int32    Int     { get; set; }
            public Int64    Long    { get; set; }
            public DateTime Date    { get; set; }
        }

        private class SingleColumn
        {
            public string Column { get; set; }
        }

        private class RenamedColumn
        {
            [Column("MyRenamedColumn")]
            public string Column { get; set; }
        }

        private class NullableColumns
        {
            public string  Name           { get; set; }
            public int?    NullableInt    { get; set; }
            public double? NullableDouble { get; set; }
        }

        private class WithStaticValue
        {
            [StaticValue(Result = "Foobar")]
            public string Name { get; set; }
        }

        private class RenamedColumnWithStaticValue
        {
            [Column("MyRenamedColumn")]
            [StaticValue(Result = "Foobar")]
            public string Name { get; set; }
        }

        private class WithStaticValueToUpper
        {
            [StaticValue(Result = "is upper?")]
            [ToUpper(1)]
            public string Name { get; set; }
        }

        private class WithInvalidTransformer
        {
            [ToUpper]
            public double Value { get; set; }
        }

        private class StaticValueAttribute : DataTransformerAttributeBase
        {
            public object Result { get; set; }

            public override object Transform(object value, Type targetType)
            {
                return Result;
            }
        }

        private class ToUpperAttribute : DataTransformerAttributeBase
        {
            public ToUpperAttribute(int order = 0)
                : base(order)
            {

            }

            public override object Transform(object value, Type targetType)
            {
                return ((string)value).ToUpper();
            }
        }

        private class StaticTransformer : IDataTransformer
        {
            public string Result { get; set; }

            public bool CanTransform(object value, Type targetType, IEnumerable<Attribute> propertyAttributes)
            {
                return true;
            }

            public object Transform(object value, Type targetType, IEnumerable<Attribute> propertyAttributes)
            {
                return Result;
            }
        }

        private class NeverTransformer : IDataTransformer
        {
            public bool CanTransform(object value, Type targetType, IEnumerable<Attribute> propertyAttributes)
            {
                return false;
            }

            public object Transform(object value, Type targetType, IEnumerable<Attribute> propertyAttributes)
            {
                Assert.Fail("Transform should not be called when an IDataTransformer returns false from CanTransform");
                return null;
            }
        }
        #endregion
    }
}
