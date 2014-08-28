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
        [TestMethod]
        public void CanCallWithoutArguments()
        {
            var reader = new Mock<IDataReader>();
            reader.SetupGet(r => r.FieldCount).Returns(1);
            reader.Setup(r => r.GetName(0)).Returns("FirstName");
            reader.SetupSequence(r => r.Read())
                  .Returns(true)
                  .Returns(false);
            reader.Setup(r => r.GetValues(It.IsAny<object[]>()))
                  .Callback<object[]>(o => o[0] = "Foo");

            var parms = new DataParameterCollection();
            var cmd = new Mock<IDbCommand>();
            cmd.Setup(c => c.ExecuteReader())
               .Returns(reader.Object);
            cmd.Setup(c => c.Parameters)
               .Returns(parms);

            var ctx = new Mock<IDbConnection>();
            ctx.Setup(c => c.CreateCommand())
               .Returns(cmd.Object);
            
            dynamic toTest = new DynamicStoredProcedure(ctx.Object, false, CancellationToken.None);

            IEnumerable<Person> people = toTest.usp_GetPeople<Person>();

            Assert.AreEqual("Foo", people.Single().FirstName);
        }

        [TestMethod]
        public void CanCallWithReturnValueFromNonQuery()
        {
            var parms = new DataParameterCollection();
            var cmd = new Mock<IDbCommand>();
            cmd.Setup(c => c.ExecuteNonQuery())
               .Callback(() =>
                   {
                       var parm = ((SqlParameter)parms[0]);
                       Assert.AreEqual(ParameterDirection.ReturnValue, parm.Direction, "Not passed as ReturnValue");
                       parm.Value = 42;
                   })
               .Returns(42);
            cmd.Setup(c => c.Parameters)
               .Returns(parms);

            var ctx = new Mock<IDbConnection>();
            ctx.Setup(c => c.CreateCommand())
               .Returns(cmd.Object);

            dynamic toTest = new DynamicStoredProcedure(ctx.Object, false, CancellationToken.None);

            int retValue;
            toTest.usp_StoredProc(returnValue: out retValue);

            Assert.AreEqual(42, retValue, "Return value not set.");
        }

        [TestMethod]
        public void CanCallWithRefParameterNoQuery()
        {
            var parms = new DataParameterCollection();
            var cmd = new Mock<IDbCommand>();
            cmd.Setup(c => c.ExecuteNonQuery())
               .Callback(() =>
                   {
                       var parm = ((SqlParameter)parms[0]);
                       Assert.AreEqual(ParameterDirection.InputOutput, parm.Direction, "Not passed as InputOutput");
                       Assert.AreEqual(16, (int)parm.Value, "Ref parameter not passed to SP");
                       parm.Value = 42;
                   })
               .Returns(0);
            cmd.Setup(c => c.Parameters)
               .Returns(parms);

            var ctx = new Mock<IDbConnection>();
            ctx.Setup(c => c.CreateCommand())
               .Returns(cmd.Object);

            dynamic toTest = new DynamicStoredProcedure(ctx.Object, false, CancellationToken.None);

            int id = 16;
            toTest.usp_StoredProc(id: ref id);

            Assert.AreEqual(42, id, "Ref parameter not set.");
        }

        [TestMethod]
        public void CanCallWithOutParameterNoQuery()
        {
            var parms = new DataParameterCollection();
            var cmd = new Mock<IDbCommand>();
            cmd.Setup(c => c.ExecuteNonQuery())
               .Callback(() =>
                   {
                       var parm = ((SqlParameter)parms[0]);
                       Assert.AreEqual(ParameterDirection.Output, parm.Direction, "Not passed as Output");
                       parm.Value = 42;
                   })
               .Returns(0);
            cmd.Setup(c => c.Parameters)
               .Returns(parms);

            var ctx = new Mock<IDbConnection>();
            ctx.Setup(c => c.CreateCommand())
               .Returns(cmd.Object);

            dynamic toTest = new DynamicStoredProcedure(ctx.Object, false, CancellationToken.None);

            int id;
            toTest.usp_StoredProc(id: out id);

            Assert.AreEqual(42, id, "Out parameter not set.");   
        }

        [TestMethod]
        public void CanCallAsyncWithNoArguments()
        {
            var reader = new Mock<IDataReader>();
            reader.SetupGet(r => r.FieldCount).Returns(1);
            reader.Setup(r => r.GetName(0)).Returns("FirstName");
            reader.SetupSequence(r => r.Read())
                  .Returns(true)
                  .Returns(false);
            reader.Setup(r => r.GetValues(It.IsAny<object[]>()))
                  .Callback<object[]>(o => o[0] = "Foo");

            var parms = new DataParameterCollection();
            var cmd = new Mock<IDbCommand>();
            cmd.Setup(c => c.ExecuteReader())
               .Returns(reader.Object);
            cmd.Setup(c => c.Parameters)
               .Returns(parms);

            var ctx = new Mock<IDbConnection>();
            ctx.Setup(c => c.CreateCommand())
               .Returns(cmd.Object);

            dynamic toTest = new DynamicStoredProcedure(ctx.Object, true, CancellationToken.None);

            Task<IEnumerable<Person>> task = toTest.usp_GetPeople<Person>();
            task.Wait();
            Assert.AreEqual("Foo", task.Result.Single().FirstName);
        }

        [TestMethod]
        public void CallAsyncWithSimpleReturnValueThrows()
        {
            dynamic toTest = new DynamicStoredProcedure(Mock.Of<IDbConnection>(), true, CancellationToken.None);

            int retValue;
            try
            {
                toTest.usp_StoredProc(returnValue: out retValue);
                Assert.Fail("Expected exception not thrown.");
            }
            catch(NotSupportedException ex)
            {
                Assert.AreEqual(DynamicStoredProcedure.asyncParameterDirectionError, ex.Message);
            }
        }

        [TestMethod]
        public void CallAsyncWithSimpleRefValueThrows()
        {
            dynamic toTest = new DynamicStoredProcedure(Mock.Of<IDbConnection>(), true, CancellationToken.None);

            string retValue = "Foo";
            try
            {
                toTest.usp_StoredProc(id: ref retValue);
                Assert.Fail("Expected exception not thrown.");
            }
            catch (NotSupportedException ex)
            {
                Assert.AreEqual(DynamicStoredProcedure.asyncParameterDirectionError, ex.Message);
            }
        }

        [TestMethod]
        public void CallAsyncWithSimpleOutValueThrows()
        {
            dynamic toTest = new DynamicStoredProcedure(Mock.Of<IDbConnection>(), true, CancellationToken.None);

            decimal val;
            try
            {
                toTest.usp_StoredProc(value: out val);
                Assert.Fail("Expected exception not thrown.");
            }
            catch (NotSupportedException ex)
            {
                Assert.AreEqual(DynamicStoredProcedure.asyncParameterDirectionError, ex.Message);
            }
        }

        [TestMethod]
        public void CanCallAsyncWithReturnValueFromNonQuery()
        {
            var parms = new DataParameterCollection();
            var cmd = new Mock<IDbCommand>();
            cmd.Setup(c => c.ExecuteNonQuery())
               .Callback(() =>
               {
                   var parm = ((SqlParameter)parms[0]);
                   Assert.AreEqual(ParameterDirection.ReturnValue, parm.Direction, "Not passed as ReturnValue");
                   parm.Value = 42;
               })
               .Returns(42);
            cmd.Setup(c => c.Parameters)
               .Returns(parms);

            var ctx = new Mock<IDbConnection>();
            ctx.Setup(c => c.CreateCommand())
               .Returns(cmd.Object);

            dynamic toTest = new DynamicStoredProcedure(ctx.Object, true, CancellationToken.None);

            var retValue = new Return();
            Task task = toTest.usp_StoredProc(retValue);
            task.Wait();

            Assert.AreEqual(42, retValue.Value, "Return value not set.");
        }

        [TestMethod]
        public void CanCallAsyncWithRefParameterNoQuery()
        {
            var parms = new DataParameterCollection();
            var cmd = new Mock<IDbCommand>();
            cmd.Setup(c => c.ExecuteNonQuery())
               .Callback(() =>
               {
                   var parm = ((SqlParameter)parms[0]);
                   Assert.AreEqual(ParameterDirection.InputOutput, parm.Direction, "Not passed as InputOutput");
                   Assert.AreEqual(16, (int)parm.Value, "Ref parameter not passed to SP");
                   parm.Value = 42;
               })
               .Returns(0);
            cmd.Setup(c => c.Parameters)
               .Returns(parms);

            var ctx = new Mock<IDbConnection>();
            ctx.Setup(c => c.CreateCommand())
               .Returns(cmd.Object);

            dynamic toTest = new DynamicStoredProcedure(ctx.Object, true, CancellationToken.None);

            var inputOutput = new InputOutput { Value = 16 };
            Task task = toTest.usp_StoredProc(inputOutput);
            task.Wait();

            Assert.AreEqual(42, inputOutput.Value, "Ref parameter not set.");
        }

        [TestMethod]
        public void CanCallAsyncWithOutParameterNoQuery()
        {
            var parms = new DataParameterCollection();
            var cmd = new Mock<IDbCommand>();
            cmd.Setup(c => c.ExecuteNonQuery())
               .Callback(() =>
               {
                   var parm = ((SqlParameter)parms[0]);
                   Assert.AreEqual(ParameterDirection.Output, parm.Direction, "Not passed as Output");
                   parm.Value = 42;
               })
               .Returns(0);
            cmd.Setup(c => c.Parameters)
               .Returns(parms);

            var ctx = new Mock<IDbConnection>();
            ctx.Setup(c => c.CreateCommand())
               .Returns(cmd.Object);

            dynamic toTest = new DynamicStoredProcedure(ctx.Object, true, CancellationToken.None);

            var output = new Output();
            Task task = toTest.usp_StoredProc(output);
            task.Wait();

            Assert.AreEqual(42, output.Value, "Out parameter not set.");
        }

        [TestMethod]
        public void CancelledTokenWillNotExecute()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();
            var parms = new DataParameterCollection();

            var reader = new Mock<IDataReader>();
            reader.SetupGet(r => r.FieldCount).Returns(1);
            reader.Setup(r => r.GetName(0)).Returns("FirstName");
            reader.Setup(r => r.Read())
                  .Throws(new Exception("Should have been cancelled."));

            var cmd = new Mock<IDbCommand>();
            cmd.Setup(c => c.ExecuteReader())
               .Returns(reader.Object);
            cmd.Setup(c => c.Parameters)
               .Returns(parms);

            var ctx = new Mock<IDbConnection>();
            ctx.Setup(c => c.CreateCommand())
               .Returns(cmd.Object);

            dynamic toTest = new DynamicStoredProcedure(ctx.Object, true, cts.Token);

            var value = 13;
            Task<IEnumerable<Person>> people = toTest.usp_StoredProc<Person>(value: value);
            Assert.AreEqual(TaskStatus.Canceled, people.Status);
        }

        [TestMethod]
        public void CanGetMultipleResultSets()
        {
            var reader = new Mock<IDataReader>();
            reader.SetupGet(r => r.FieldCount).Returns(1);
            reader.Setup(r => r.GetName(0)).Returns("FirstName");
            reader.SetupSequence(r => r.Read())
                  .Returns(true)
                  .Returns(false);
            reader.Setup(r => r.GetValues(It.IsAny<object[]>()))
                  .Callback<object[]>(o => o[0] = "Foo");
            reader.Setup(r => r.NextResult())
                  .Callback(() => 
                    {
                        reader.Setup(r => r.GetName(0)).Returns("LastName");
                        reader.SetupSequence(r => r.Read())
                              .Returns(true)
                              .Returns(false);
                        reader.Setup(r => r.GetValues(It.IsAny<object[]>()))
                              .Callback<object[]>(o => o[0] = "Bar");
                    })
                  .Returns(true);

            var parms = new DataParameterCollection();
            var cmd = new Mock<IDbCommand>();
            cmd.Setup(c => c.ExecuteReader())
               .Returns(reader.Object);
            cmd.Setup(c => c.Parameters)
               .Returns(parms);

            var ctx = new Mock<IDbConnection>();
            ctx.Setup(c => c.CreateCommand())
               .Returns(cmd.Object);

            dynamic toTest = new DynamicStoredProcedure(ctx.Object, false, CancellationToken.None);

            Tuple<IEnumerable<Person>, IEnumerable<Family>> results = toTest.usp_GetPeople<Person, Family>();

            Assert.AreEqual("Foo", results.Item1.Single().FirstName, "First result set not returned.");
            Assert.AreEqual("Bar", results.Item2.Single().LastName, "Second result set not returned.");
        }

        [TestMethod]
        public void CanGetMultipleResultSetsAsync()
        {
            var reader = new Mock<IDataReader>();
            reader.SetupGet(r => r.FieldCount).Returns(1);
            reader.Setup(r => r.GetName(0)).Returns("FirstName");
            reader.SetupSequence(r => r.Read())
                  .Returns(true)
                  .Returns(false);
            reader.Setup(r => r.GetValues(It.IsAny<object[]>()))
                  .Callback<object[]>(o => o[0] = "Foo");
            reader.Setup(r => r.NextResult())
                  .Callback(() =>
                  {
                      reader.Setup(r => r.GetName(0)).Returns("LastName");
                      reader.SetupSequence(r => r.Read())
                            .Returns(true)
                            .Returns(false);
                      reader.Setup(r => r.GetValues(It.IsAny<object[]>()))
                            .Callback<object[]>(o => o[0] = "Bar");
                  })
                  .Returns(true);

            var parms = new DataParameterCollection();
            var cmd = new Mock<IDbCommand>();
            cmd.Setup(c => c.ExecuteReader())
               .Returns(reader.Object);
            cmd.Setup(c => c.Parameters)
               .Returns(parms);

            var ctx = new Mock<IDbConnection>();
            ctx.Setup(c => c.CreateCommand())
               .Returns(cmd.Object);

            dynamic toTest = new DynamicStoredProcedure(ctx.Object, true, CancellationToken.None);

            Task<Tuple<IEnumerable<Person>, IEnumerable<Family>>> task = toTest.usp_GetPeople<Person, Family>();
            task.Wait();

            var results = task.Result;

            Assert.AreEqual("Foo", results.Item1.Single().FirstName, "First result set not returned.");
            Assert.AreEqual("Bar", results.Item2.Single().LastName, "Second result set not returned.");
        }

        private class Person
        {
            public string FirstName { get; set; }
        }

        private class Family
        {
            public string LastName { get; set; }
        }

        private class Return
        {
            [StoredProcedureParameter(Direction = ParameterDirection.ReturnValue)]
            public int Value { get; set; }
        }

        private class Output
        {
            [StoredProcedureParameter(Direction = ParameterDirection.Output)]
            public int Value { get; set; }
        }

        private class InputOutput
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
