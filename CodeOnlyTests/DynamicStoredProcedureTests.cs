using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeOnlyStoredProcedure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

#if NET40
namespace CodeOnlyTests.Net40
#else
namespace CodeOnlyTests
#endif
{
    [TestClass]
    public class DynamicStoredProcedureTests
    {
        [TestClass]
        public class Synchronous
        {

            [TestMethod]
            public void CanCallWithoutArguments()
            {
                var ctx = CreatePeople("Foo");

                dynamic toTest = new DynamicStoredProcedure(ctx, CancellationToken.None);

                IEnumerable<Person> people = toTest.usp_GetPeople();

                Assert.AreEqual("Foo", people.Single().FirstName);
            }

            [TestMethod]
            public void CanCallWithReturnValueFromNonQuery()
            {
                var ctx = CreatePeople(parms =>
                {
                    var parm = ((SqlParameter)parms[0]);
                    Assert.AreEqual(ParameterDirection.ReturnValue, parm.Direction, "Not passed as ReturnValue");
                    parm.Value = 42;
                });

                dynamic toTest = new DynamicStoredProcedure(ctx, CancellationToken.None);

                int retValue;
                toTest.usp_StoredProc(returnValue: out retValue);

                Assert.AreEqual(42, retValue, "Return value not set.");
            }

            [TestMethod]
            public void CanCallWithRefParameterNoQuery()
            {
                var ctx = CreatePeople(parms =>
                {
                    var parm = ((SqlParameter)parms[0]);
                    Assert.AreEqual(ParameterDirection.InputOutput, parm.Direction, "Not passed as InputOutput");
                    Assert.AreEqual(16, (int)parm.Value, "Ref parameter not passed to SP");
                    parm.Value = 42;
                });

                dynamic toTest = new DynamicStoredProcedure(ctx, CancellationToken.None);

                int id = 16;
                toTest.usp_StoredProc(id: ref id);

                Assert.AreEqual(42, id, "Ref parameter not set.");
            }

            [TestMethod]
            public void CanCallWithOutParameterNoQuery()
            {
                var ctx = CreatePeople(parms =>
                {
                    var parm = ((SqlParameter)parms[0]);
                    Assert.AreEqual(ParameterDirection.Output, parm.Direction, "Not passed as Output");
                    parm.Value = 42;
                });

                dynamic toTest = new DynamicStoredProcedure(ctx, CancellationToken.None);

                int id;
                toTest.usp_StoredProc(id: out id);

                Assert.AreEqual(42, id, "Out parameter not set.");
            }

            [TestMethod]
            public void CancelledTokenWillNotExecute()
            {
                var ctx = CreatePeople(_ =>
                {
                    throw new Exception("Should have been cancelled.");
                });
                var cts = new CancellationTokenSource();
                cts.Cancel();

                dynamic toTest = new DynamicStoredProcedure(ctx, cts.Token);

                var value = 13;
                Task<IEnumerable<Person>> people = toTest.usp_StoredProc(value: value);
                Assert.AreEqual(TaskStatus.Canceled, people.Status);
            }

            [TestMethod]
            public void CanGetMultipleResultSets()
            {
                int resultSet = 0;

                var reader = new Mock<IDataReader>();
                reader.SetupGet(r => r.FieldCount).Returns(1);
                reader.Setup(r => r.GetName(0))
                      .Returns(() => resultSet == 0 ? "FirstName" : "LastName");
                reader.SetupSequence(r => r.Read())
                      .Returns(true)
                      .Returns(false)
                      .Returns(true)
                      .Returns(false);
                reader.Setup(r => r.GetValues(It.IsAny<object[]>()))
                      .Callback<object[]>(o => o[0] = resultSet == 0 ? "Foo" : "Bar");
                reader.Setup(r => r.NextResult())
                      .Callback(() => ++resultSet)
                      .Returns(() => resultSet < 2);

                var parms = new DataParameterCollection();
                var cmd = new Mock<IDbCommand>();
                cmd.Setup(c => c.ExecuteReader())
                   .Returns(reader.Object);
                cmd.Setup(c => c.Parameters)
                   .Returns(parms);

                var ctx = new Mock<IDbConnection>();
                ctx.Setup(c => c.CreateCommand())
                   .Returns(cmd.Object);

                dynamic toTest = new DynamicStoredProcedure(ctx.Object, CancellationToken.None);

                Tuple<IEnumerable<Person>, IEnumerable<Family>> results = toTest.usp_GetPeople();

                Assert.AreEqual("Foo", results.Item1.Single().FirstName, "First result set not returned.");
                Assert.AreEqual("Bar", results.Item2.Single().LastName, "Second result set not returned.");
            }
        }

        [TestClass]
        public abstract class Asynchronous
        {
            protected abstract Task<IEnumerable<Person>> GetPeople(dynamic toTest);
            protected abstract Task<IEnumerable<Person>> GetPeopleShouldThrow(dynamic toTest, ParameterDirection direction);
            protected abstract Task<IEnumerable<Person>> GetPeople<T>(dynamic toTest, T args);
            protected abstract Task<Tuple<IEnumerable<Person>, IEnumerable<Family>>> GetFamilies(dynamic toTest);
            protected abstract Task Call<T>(dynamic toTest, T args);

            [TestMethod]
            public void CanCallAsyncWithNoArguments()
            {
                var ctx = CreatePeople("Foo");

                var toTest = new DynamicStoredProcedure(ctx, CancellationToken.None);

                var result = GetPeople(toTest).Result;

                Assert.AreEqual("Foo", result.Single().FirstName);
            }

            [TestMethod]
            public void CallAsyncWithSimpleReturnValueThrows()
            {
                var ctx = CreatePeople(parms =>
                {
                    var parm = ((SqlParameter)parms[0]);
                    Assert.AreEqual(ParameterDirection.ReturnValue, parm.Direction, "Not passed as ReturnValue");
                    parm.Value = 42;
                }, "Foo");

                var toTest = new DynamicStoredProcedure(ctx, CancellationToken.None);

                try
                {
                    GetPeopleShouldThrow(toTest, ParameterDirection.ReturnValue).Wait();
                    Assert.Fail("Expected exception not thrown.");
                }
                catch (AggregateException ax)
                {
                    Assert.AreEqual(DynamicStoredProcedure.asyncParameterDirectionError, ax.InnerException.Message);
                }
                catch (NotSupportedException ex)
                {
                    Assert.AreEqual(DynamicStoredProcedure.asyncParameterDirectionError, ex.Message);
                }
            }

            [TestMethod]
            public void CallAsyncWithSimpleRefValueThrows()
            {
                var ctx = CreatePeople(parms =>
                {
                    var parm = ((SqlParameter)parms[0]);
                    Assert.AreEqual(ParameterDirection.InputOutput, parm.Direction, "Not passed as InputOutput");
                    Assert.AreEqual("Foo", parm.Value, "Ref value not passed to the stored procedure.");
                    parm.Value = "Bar";
                }, "Foo");

                var toTest = new DynamicStoredProcedure(ctx, CancellationToken.None);

                try
                {
                    GetPeopleShouldThrow(toTest, ParameterDirection.InputOutput).Wait();
                    Assert.Fail("Expected exception not thrown.");
                }
                catch (AggregateException ax)
                {
                    Assert.AreEqual(DynamicStoredProcedure.asyncParameterDirectionError, ax.InnerException.Message);
                }
                catch (NotSupportedException ex)
                {
                    Assert.AreEqual(DynamicStoredProcedure.asyncParameterDirectionError, ex.Message);
                }
            }

            [TestMethod]
            public void CallAsyncWithSimpleOutValueThrows()
            {
                var ctx = CreatePeople(parms =>
                {
                    var parm = ((SqlParameter)parms[0]);
                    Assert.AreEqual(ParameterDirection.Output, parm.Direction, "Not passed as Output");
                    parm.Value = 42M;
                }, "Foo");

                var toTest = new DynamicStoredProcedure(ctx, CancellationToken.None);

                try
                {
                    GetPeopleShouldThrow(toTest, ParameterDirection.Output).Wait();
                    Assert.Fail("Expected exception not thrown.");
                }
                catch (AggregateException ax)
                {
                    Assert.AreEqual(DynamicStoredProcedure.asyncParameterDirectionError, ax.InnerException.Message);
                }
                catch (NotSupportedException ex)
                {
                    Assert.AreEqual(DynamicStoredProcedure.asyncParameterDirectionError, ex.Message);
                }
            }

            [TestMethod]
            public void CanCallAsyncWithReturnValueFromNonQuery()
            {
                var ctx = CreatePeople(parms =>
                {
                    var parm = ((SqlParameter)parms[0]);
                    Assert.AreEqual(ParameterDirection.ReturnValue, parm.Direction, "Not passed as ReturnValue");
                    parm.Value = 42;
                });

                var toTest = new DynamicStoredProcedure(ctx, CancellationToken.None);

                var retValue = new Return();
                Call(toTest, retValue).Wait();

                Assert.AreEqual(42, retValue.Value, "Return value not set.");
            }

            [TestMethod]
            public void CanCallAsyncWithRefParameterNonQuery()
            {
                var ctx = CreatePeople(parms =>
                {
                    var parm = ((SqlParameter)parms[0]);
                    Assert.AreEqual(ParameterDirection.InputOutput, parm.Direction, "Not passed as InputOutput");
                    Assert.AreEqual(16, (int)parm.Value, "Ref parameter not passed to SP");
                    parm.Value = 42;
                });

                var toTest = new DynamicStoredProcedure(ctx, CancellationToken.None);

                var inputOutput = new InputOutput { Value = 16 };

                Call(toTest, inputOutput).Wait();

                Assert.AreEqual(42, inputOutput.Value, "Ref parameter not set.");
            }

            [TestMethod]
            public void CanCallAsyncWithOutParameterNonQuery()
            {
                var ctx = CreatePeople(parms =>
                {
                    var parm = ((SqlParameter)parms[0]);
                    Assert.AreEqual(ParameterDirection.Output, parm.Direction, "Not passed as Output");
                    parm.Value = 42;
                });

                var toTest = new DynamicStoredProcedure(ctx, CancellationToken.None);

                var output = new Output();

                Call(toTest, output).Wait();

                Assert.AreEqual(42, output.Value, "Out parameter not set.");
            }

            [TestMethod]
            public void CanAllAsyncWithReturnValue()
            {
                var ctx = CreatePeople(parms =>
                {
                    var parm = ((SqlParameter)parms[0]);
                    Assert.AreEqual(ParameterDirection.ReturnValue, parm.Direction, "Not passed as ReturnValue");
                    parm.Value = 42;
                }, "Foo", "Bar");

                var toTest = new DynamicStoredProcedure(ctx, CancellationToken.None);

                var retValue = new Return();
                var people = GetPeople(toTest, retValue).Result;

                Assert.AreEqual(42, retValue.Value, "Return value not set.");
                Assert.IsTrue(people.Select(p => p.FirstName).SequenceEqual(new[] { "Foo", "Bar" }));
            }

            [TestMethod]
            public void CanAllAsyncWithRefParameterValue()
            {
                var ctx = CreatePeople(parms =>
                {
                    var parm = ((SqlParameter)parms[0]);
                    Assert.AreEqual(ParameterDirection.InputOutput, parm.Direction, "Not passed as InputOutput");
                    Assert.AreEqual(22, (int)parm.Value, "Ref parameter not passed to SP");
                    parm.Value = 42;
                }, "Bar", "Baz");

                var toTest = new DynamicStoredProcedure(ctx, CancellationToken.None);

                var inout = new InputOutput { Value = 22 };
                var people = GetPeople(toTest, inout).Result;

                Assert.AreEqual(42, inout.Value, "Ref parameter not set.");
                Assert.IsTrue(people.Select(p => p.FirstName).SequenceEqual(new[] { "Bar", "Baz" }));
            }

            [TestMethod]
            public void CanAllAsyncWithOutParameterValue()
            {
                var ctx = CreatePeople(parms =>
                {
                    var parm = ((SqlParameter)parms[0]);
                    Assert.AreEqual(ParameterDirection.Output, parm.Direction, "Not passed as Output");
                    parm.Value = 42;
                }, "Bar", "Baz");

                var toTest = new DynamicStoredProcedure(ctx, CancellationToken.None);

                var output = new Output();
                var people = GetPeople(toTest, output).Result;

                Assert.AreEqual(42, output.Value, "Out parameter not set.");
                Assert.IsTrue(people.Select(p => p.FirstName).SequenceEqual(new[] { "Bar", "Baz" }));
            }

            [TestMethod]
            public void CanGetMultipleResultSetsAsync()
            {
                int resultSet = 0;

                var reader = new Mock<IDataReader>();
                reader.SetupGet(r => r.FieldCount).Returns(1);
                reader.Setup(r => r.GetName(0))
                      .Returns(() => resultSet == 0 ? "FirstName" : "LastName");
                reader.SetupSequence(r => r.Read())
                      .Returns(true)
                      .Returns(false)
                      .Returns(true)
                      .Returns(false);
                reader.Setup(r => r.GetValues(It.IsAny<object[]>()))
                      .Callback<object[]>(o => o[0] = resultSet == 0 ? "Foo" : "Bar");
                reader.Setup(r => r.NextResult())
                      .Callback(() => ++resultSet)
                      .Returns(() => resultSet < 2);

                var parms = new DataParameterCollection();
                var cmd = new Mock<IDbCommand>();
                cmd.Setup(c => c.ExecuteReader())
                   .Returns(reader.Object);
                cmd.Setup(c => c.Parameters)
                   .Returns(parms);

                var ctx = new Mock<IDbConnection>();
                ctx.Setup(c => c.CreateCommand())
                   .Returns(cmd.Object);

                var toTest = new DynamicStoredProcedure(ctx.Object, CancellationToken.None);
                
                var results = GetFamilies(toTest).Result;

                Assert.AreEqual("Foo", results.Item1.Single().FirstName, "First result set not returned.");
                Assert.AreEqual("Bar", results.Item2.Single().LastName, "Second result set not returned.");
            }
        }

        [TestClass]
        public class AsyncSyntax : Asynchronous
        {
            protected override async Task<IEnumerable<Person>> GetPeople(dynamic toTest)
            {
                return await toTest.usp_GetPeople();
            }

            protected async override Task<IEnumerable<Person>> GetPeopleShouldThrow(dynamic toTest, ParameterDirection direction)
            {
                switch (direction)
                {
                    case ParameterDirection.InputOutput:
                        string inOutValue = "Foo";
                        return await toTest.usp_GetPeople(id: ref inOutValue);

                    case ParameterDirection.Output:
                        decimal outValue;
                        return await toTest.usp_GetPeople(value: out outValue);

                    case ParameterDirection.ReturnValue:
                        int returnValue = -1;
                        return await toTest.usp_GetPeople(returnValue: out returnValue);
                }

                return null;
            }

            protected override async Task<IEnumerable<Person>> GetPeople<T>(dynamic toTest, T args)
            {
                return await toTest.usp_GetPeople(args);
            }

            protected override async Task<Tuple<IEnumerable<Person>, IEnumerable<Family>>> GetFamilies(dynamic toTest)
            {
                return await toTest.usp_GetFamilies();
            }

            protected override async Task Call<T>(dynamic toTest, T args)
            {
                await toTest.usp_StoredProc(args);
            }
        }

        [TestClass]
        public class TaskSyntax : Asynchronous
        {
            protected override Task<IEnumerable<Person>> GetPeople(dynamic toTest)
            {
                return toTest.usp_GetPeople();
            }

            protected override Task<IEnumerable<Person>> GetPeopleShouldThrow(dynamic toTest, ParameterDirection direction)
            {
                switch (direction)
                {
                    case ParameterDirection.InputOutput:
                        string inOutValue = "Foo";
                        return toTest.usp_GetPeople(id: ref inOutValue);

                    case ParameterDirection.Output:
                        decimal outValue;
                        return toTest.usp_GetPeople(value: out outValue);

                    case ParameterDirection.ReturnValue:
                        int returnValue = -1;
                        return toTest.usp_GetPeople(returnValue: out returnValue);
                }

                return null;
            }

            protected override Task<IEnumerable<Person>> GetPeople<T>(dynamic toTest, T args)
            {
                return toTest.usp_GetPeople(args);
            }

            protected override Task<Tuple<IEnumerable<Person>, IEnumerable<Family>>> GetFamilies(dynamic toTest)
            {
                return toTest.usp_GetFamilies();
            }

            protected override Task Call<T>(dynamic toTest, T args)
            {
                return toTest.usp_StoredProc(args);
            }
        }

        private static IDbConnection CreatePeople(params string[] names)
        {
            return CreatePeople(_ => { }, names);
        }

        private static IDbConnection CreatePeople(Action<DataParameterCollection> readerCallback, params string[] names)
        {
            var reader = new Mock<IDataReader>();
            reader.SetupGet(r => r.FieldCount).Returns(1);
            reader.Setup(r => r.GetName(0)).Returns("FirstName");

            var setup = reader.SetupSequence(r => r.Read());

            var idx = 0;
            reader.Setup(r => r.GetValues(It.IsAny<object[]>()))
                  .Callback<object[]>(o => o[0] = names[idx++]);

            for (int i = 0; i < names.Length; ++i)
                setup = setup.Returns(true);

            setup.Returns(false);

            var parms = new DataParameterCollection();
            var cmd = new Mock<IDbCommand>();
            cmd.Setup(c => c.ExecuteReader())
               .Callback(() => readerCallback(parms))
               .Returns(reader.Object);
            cmd.SetupGet(c => c.Parameters)
               .Returns(parms);

            var ctx = new Mock<IDbConnection>();
            ctx.Setup(c => c.CreateCommand())
               .Returns(cmd.Object);

            return ctx.Object;
        }

        public class Person
        {
            public string FirstName { get; set; }
        }

        public class Family
        {
            public string LastName { get; set; }
        }

        public class Return
        {
            [StoredProcedureParameter(Direction = ParameterDirection.ReturnValue)]
            public int Value { get; set; }
        }

        public class Output
        {
            [StoredProcedureParameter(Direction = ParameterDirection.Output)]
            public int Value { get; set; }
        }

        public class InputOutput
        {
            [StoredProcedureParameter(Direction = ParameterDirection.InputOutput)]
            public int Value { get; set; }
        }

        private class DataParameterCollection : List<object>, IDataParameterCollection
        {
            public bool Contains(string parameterName)
            {
                return false;
            }

            public int IndexOf(string parameterName)
            {
                return -1;
            }

            public void RemoveAt(string parameterName)
            {
            }

            public object this[string parameterName]
            {
                get
                {
                    return null;
                }
                set
                {
                }
            }
        }

    }
}
