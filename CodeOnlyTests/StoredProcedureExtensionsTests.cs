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
    public partial class StoredProcedureExtensionsTests
    {
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
                { "Date",    new DateTime(1982, 1, 31) },
                { "FooBar",  (int)FooBar.Bar           }
            };

            var keys = values.Keys.OrderBy(s => s).ToArray();
            var vals = values.OrderBy(kv => kv.Key).Select(kv => kv.Value).ToArray();

            var reader  = new Mock<IDataReader>();
            var command = new Mock<IDbCommand>();

            command.Setup(d => d.ExecuteReader())
                   .Returns(reader.Object);

            reader.SetupGet(r => r.FieldCount)
                  .Returns(keys.Length);

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
                  .Callback((object[] arr) => vals.CopyTo(arr, 0))
                  .Returns(vals.Length);

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
            Assert.AreEqual(FooBar.Bar,                item.FooBar);
        }

        [TestMethod]
        public void TestExecuteReturnsSingleResultSetOneRowWithStringEnumValue()
        {
            var values = new Dictionary<string, object>
            {
                { "String",  "Hello, World!"           },
                { "Double",  42.0                      },
                { "Decimal", 100M                      },
                { "Int",     99                        },
                { "Long",    1028130L                  },
                { "Date",    new DateTime(1982, 1, 31) },
                { "FooBar",  "Bar"                     }
            };

            var keys = values.Keys.OrderBy(s => s).ToArray();
            var vals = values.OrderBy(kv => kv.Key).Select(kv => kv.Value).ToArray();

            var reader  = new Mock<IDataReader>();
            var command = new Mock<IDbCommand>();

            command.Setup(d => d.ExecuteReader())
                   .Returns(reader.Object);

            reader.SetupGet(r => r.FieldCount)
                  .Returns(keys.Length);

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
                  .Callback((object[] arr) => vals.CopyTo(arr, 0))
                  .Returns(vals.Length);

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
            Assert.AreEqual(FooBar.Bar,                item.FooBar);
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

            reader.SetupSequence(r => r.Read())
                  .Returns(true)
                  .Returns(false);

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

        [TestMethod]
        public void TestExecute_ReturnsSingleColumnSetForString()
        {
            var reader = new Mock<IDataReader>();
            var command = new Mock<IDbCommand>();

            var res = new[] { "Hello", "World", "Foo", "Bar" };
            int i = 0;
            reader.SetupGet(r => r.FieldCount)
                  .Returns(1);

            reader.Setup(r => r.Read())
                  .Returns(() => i < res.Length);

            reader.Setup(r => r.GetName(0))
                  .Returns("SHOULD_BE_IGNORED");
            reader.Setup(r => r.GetValues(It.IsAny<object[]>()))
                  .Callback((object[] arr) => arr[0] = res[i++])
                  .Returns(1);

            command.SetupAllProperties();
            command.Setup(d => d.ExecuteReader())
                   .Returns(reader.Object);

            var results = command.Object.Execute(CancellationToken.None,
                                                 new[] { typeof(string) },
                                                 Enumerable.Empty<IDataTransformer>());

            var totest = (IEnumerable<string>)results[typeof(string)];
            for (int j = 0; j < res.Length; j++)
            {
                Assert.AreEqual(res[j], totest.ElementAt(j));
            }
        }

        [TestMethod]
        public void TestExecute_ReturnsSingleColumnSetForEnum()
        {
            var reader = new Mock<IDataReader>();
            var command = new Mock<IDbCommand>();

            command.SetupAllProperties();
            command.Setup(d => d.ExecuteReader())
                   .Returns(reader.Object);

            var res = new[] { FooBar.Foo, FooBar.Bar };
            int i = 0;
            reader.SetupGet(r => r.FieldCount)
                  .Returns(1);

            reader.Setup(r => r.Read())
                  .Returns(() => i < res.Length);

            reader.Setup(r => r.GetName(0))
                  .Returns("SHOULD_BE_IGNORED");
            reader.Setup(r => r.GetValues(It.IsAny<object[]>()))
                  .Callback((object[] arr) => arr[0] = (int)res[i++])
                  .Returns(1);

            var results = command.Object.Execute(CancellationToken.None,
                                                 new[] { typeof(FooBar) },
                                                 Enumerable.Empty<IDataTransformer>());

            var totest = (IEnumerable<FooBar>)results[typeof(FooBar)];
            for (int j = 0; j < res.Length; j++)
            {
                Assert.AreEqual(res[j], totest.ElementAt(j));
            }
        }

        [TestMethod]
        public void TestExecute_ReturnsSingleColumnSetForEnumReturnedAsString()
        {
            var reader = new Mock<IDataReader>();
            var command = new Mock<IDbCommand>();

            command.SetupAllProperties();
            command.Setup(d => d.ExecuteReader())
                   .Returns(reader.Object);

            var res = new[] { FooBar.Foo, FooBar.Bar };
            int i = 0;
            reader.SetupGet(r => r.FieldCount)
                  .Returns(1);

            reader.Setup(r => r.Read())
                  .Returns(() => i < res.Length);

            reader.Setup(r => r.GetName(0))
                  .Returns("SHOULD_BE_IGNORED");
            reader.Setup(r => r.GetValues(It.IsAny<object[]>()))
                  .Callback((object[] arr) => arr[0] = res[i++].ToString())
                  .Returns(1);

            var results = command.Object.Execute(CancellationToken.None,
                                                 new[] { typeof(FooBar) },
                                                 Enumerable.Empty<IDataTransformer>());

            var totest = (IEnumerable<FooBar>)results[typeof(FooBar)];
            for (int j = 0; j < res.Length; j++)
                Assert.AreEqual(res[j], totest.ElementAt(j));
        }

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
            public FooBar   FooBar  { get; set; }
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

            public override object Transform(object value, Type targetType, bool isNullable)
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

            public override object Transform(object value, Type targetType, bool isNullable)
            {
                return ((string)value).ToUpper();
            }
        }

        private class StaticTransformer : IDataTransformer
        {
            public string Result { get; set; }

            public bool CanTransform(object value, Type targetType, bool isNullable, IEnumerable<Attribute> propertyAttributes)
            {
                return true;
            }

            public object Transform(object value, Type targetType, bool isNullable, IEnumerable<Attribute> propertyAttributes)
            {
                return Result;
            }
        }

        private class NeverTransformer : IDataTransformer
        {
            public bool CanTransform(object value, Type targetType, bool isNullable, IEnumerable<Attribute> propertyAttributes)
            {
                return false;
            }

            public object Transform(object value, Type targetType, bool isNullable, IEnumerable<Attribute> propertyAttributes)
            {
                Assert.Fail("Transform should not be called when an IDataTransformer returns false from CanTransform");
                return null;
            }
        }

        private enum FooBar
        {
            Foo = 4,
            Bar = 6
        }
        #endregion
    }
}
