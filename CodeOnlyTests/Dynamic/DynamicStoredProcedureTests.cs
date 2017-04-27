using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeOnlyStoredProcedure;
using CodeOnlyStoredProcedure.Dynamic;
using FluentAssertions;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

#if NET40
namespace CodeOnlyTests.Net40.Dynamic
#else
namespace CodeOnlyTests.Dynamic
#endif
{
    [TestClass]
    public class DynamicStoredProcedureTests
    {
        private const int TEST_TIMEOUT = 200;
        private static IEnumerable<IDataTransformer> transformers = Enumerable.Empty<IDataTransformer>();

        [TestClass]
        public class Schema
        {
            [TestMethod]
            public void CanSpecifyCustomSchema()
            {
                var ctx = CreatePeople("Foo");

                dynamic toTest = new DynamicStoredProcedure(ctx, transformers, CancellationToken.None, TEST_TIMEOUT, DynamicExecutionMode.Synchronous, true);

                IEnumerable<string> people = toTest.foo.usp_GetPeople();
                people.Should().ContainSingle("Foo", "because only one person should have been returned.");

                // our setup will only ever return this one object
                var cmd = ctx.CreateCommand();
                cmd.CommandText.Should().Be("[foo].[usp_GetPeople]", "because we called the stored procedure with another schema.");
            }

            [TestMethod]
            public void MultipleSchemasThrowsException()
            {
                var ctx = CreatePeople("Foo");

                dynamic toTest = new DynamicStoredProcedure(ctx, transformers, CancellationToken.None, TEST_TIMEOUT, DynamicExecutionMode.Synchronous, true);
                IEnumerable<string> res = null;
                Action shouldThrow = () => res = toTest.foo.bar.usp_GetPeople();

                shouldThrow.ShouldThrow<StoredProcedureException>("because you can only specify one schema")
                           .WithMessage("Schema already specified once. \n\tExisting schema: foo\n\tAdditional schema: bar");
            }
        }

        [TestClass]
        public class Synchronous
        {
            [TestMethod]
            public void CanCallWithoutArguments()
            {
                var ctx = CreatePeople("Foo");

                dynamic toTest = new DynamicStoredProcedure(ctx, transformers, CancellationToken.None, TEST_TIMEOUT, DynamicExecutionMode.Synchronous, true);

                IEnumerable<Person> people = toTest.usp_GetPeople();

                people.Should().ContainSingle(p => p.FirstName == "Foo", "because that was the only person created");
            }

            [TestMethod]
            public void ClosesClonedConnectionWhenDone_WithResults()
            {
                var db = new Mock<IDbConnection>();
                var db2 = CreatePeople("Foo");
                db.As<ICloneable>().Setup(d => d.Clone()).Returns(db2);

                dynamic toTest = new DynamicStoredProcedure(db.Object, transformers, CancellationToken.None, TEST_TIMEOUT, DynamicExecutionMode.Synchronous, true);

                IEnumerable<Person> people = toTest.usp_GetPeople();

                Mock.Get(db2).Verify(d => d.Close(), Times.Once());
                db.Verify(d => d.Close(), Times.Never());
            }

            [TestMethod]
            public void DisposesCommandWhenDone_WithResults()
            {
                Mock<IDbCommand> cmd;
                var db = CreatePeople(out cmd, "Foo");

                dynamic toTest = new DynamicStoredProcedure(db, transformers, CancellationToken.None, TEST_TIMEOUT, DynamicExecutionMode.Synchronous, true);

                IEnumerable<Person> people = toTest.usp_GetPeople();

                cmd.Verify(c => c.Dispose(), Times.Once());
            }

            [TestMethod]
            public void ClosesClonedConnectionWhenDone_WithSingleResult()
            {
                var db = new Mock<IDbConnection>();
                var db2 = CreatePeople("Foo");
                db.As<ICloneable>().Setup(d => d.Clone()).Returns(db2);

                dynamic toTest = new DynamicStoredProcedure(db.Object, transformers, CancellationToken.None, TEST_TIMEOUT, DynamicExecutionMode.Synchronous, true);

                Person person = toTest.usp_GetPeople();

                Mock.Get(db2).Verify(d => d.Close(), Times.Once());
                db.Verify(d => d.Close(), Times.Never());
            }

            [TestMethod]
            public void DisposesCommandWhenDone_WithSingleResult()
            {
                Mock<IDbCommand> cmd;
                var db = CreatePeople(out cmd, "Foo");

                dynamic toTest = new DynamicStoredProcedure(db, transformers, CancellationToken.None, TEST_TIMEOUT, DynamicExecutionMode.Synchronous, true);

                Person person = toTest.usp_GetPeople();

                cmd.Verify(c => c.Dispose(), Times.Once());
            }

            [TestMethod]
            public void ClosesClonedConnectionWhenDone_WithoutResults()
            {
                var db = new Mock<IDbConnection>();
                var db2 = CreatePeople("Foo");
                db.As<ICloneable>().Setup(d => d.Clone()).Returns(db2);

                dynamic toTest = new DynamicStoredProcedure(db.Object, transformers, CancellationToken.None, TEST_TIMEOUT, DynamicExecutionMode.Synchronous, true);

                toTest.usp_NoQuery().Dispose();

                Mock.Get(db2).Verify(d => d.Close(), Times.Once());
                db.Verify(d => d.Close(), Times.Never());
            }

            [TestMethod]
            public void DisposesCommandWhenDone_WithoutResults()
            {
                Mock<IDbCommand> cmd;
                var db = CreatePeople(out cmd, "Foo");

                dynamic toTest = new DynamicStoredProcedure(db, transformers, CancellationToken.None, TEST_TIMEOUT, DynamicExecutionMode.Synchronous, true);

                toTest.usp_GetPeople().Dispose();

                cmd.Verify(c => c.Dispose(), Times.Once());
            }

            [TestMethod]
            public void CanCastExplicitly()
            {
                var ctx = CreatePeople("Foo");

                dynamic toTest = new DynamicStoredProcedure(ctx, transformers, CancellationToken.None, TEST_TIMEOUT, DynamicExecutionMode.Synchronous, true);

                var people = (IEnumerable<Person>)toTest.usp_GetPeople();

                Assert.AreEqual("Foo", people.Single().FirstName);
            }

            [TestMethod]
            public void CastingToWrongItemTypeThrows()
            {
                var ctx = CreatePeople("Foo");

                IEnumerable<int> results = null;
                dynamic toTest = new DynamicStoredProcedure(ctx, transformers, CancellationToken.None, TEST_TIMEOUT, DynamicExecutionMode.Synchronous, true);
                this.Invoking(_ => results = (IEnumerable<int>)toTest.usp_GetPeople())
                    .ShouldThrow<StoredProcedureColumnException>("casting a result set to the wrong item type should fail");
                results.Should().BeNull("casting a result set to the wrong item type should fail");
            }

            [TestMethod]
            public void CanGetSingleResultWithoutExpectingIEnumerable()
            {
                var ctx = CreatePeople("Foo");

                dynamic toTest = new DynamicStoredProcedure(ctx, transformers, CancellationToken.None, TEST_TIMEOUT, DynamicExecutionMode.Synchronous, true);

                var foo = (Person)toTest.usp_GetPeople();
                foo.FirstName.Should().Be("Foo", "because that is the name of the only person returned by the Stored Procedure");
            }

            [TestMethod]
            public void CanCallWithReturnValueFromNonQuery()
            {
                var ctx = CreateNonQuery(parms =>
                {
                    var parm = ((IDbDataParameter)parms[0]);
                    Assert.AreEqual(ParameterDirection.ReturnValue, parm.Direction, "Not passed as ReturnValue");
                    parm.Value = 42;
                });

                dynamic toTest = new DynamicStoredProcedure(ctx, transformers, CancellationToken.None, TEST_TIMEOUT, DynamicExecutionMode.Synchronous, false);

                int retValue;
                toTest.usp_StoredProc(returnValue: out retValue);

                Assert.AreEqual(42, retValue, "Return value not set.");
            }

            [TestMethod]
            public void CanCallWithRefParameterNoQuery()
            {
                var ctx = CreatePeople(parms =>
                {
                    var parm = ((IDbDataParameter)parms[0]);
                    Assert.AreEqual(ParameterDirection.InputOutput, parm.Direction, "Not passed as InputOutput");
                    Assert.AreEqual(16, (int)parm.Value, "Ref parameter not passed to SP");
                    parm.Value = 42;
                });

                dynamic toTest = new DynamicStoredProcedure(ctx, transformers, CancellationToken.None, TEST_TIMEOUT, DynamicExecutionMode.Synchronous, true);

                int id = 16;
                toTest.usp_StoredProc(id: ref id);

                Assert.AreEqual(42, id, "Ref parameter not set.");
            }

            [TestMethod]
            public void CanCallWithOutParameterNoQuery()
            {
                var ctx = CreatePeople(parms =>
                {
                    var parm = ((IDbDataParameter)parms[0]);
                    Assert.AreEqual(ParameterDirection.Output, parm.Direction, "Not passed as Output");
                    parm.Value = 42;
                });

                dynamic toTest = new DynamicStoredProcedure(ctx, transformers, CancellationToken.None, TEST_TIMEOUT, DynamicExecutionMode.Synchronous, true);

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

                this.Invoking(_ =>
                {
                    dynamic toTest = new DynamicStoredProcedure(ctx, transformers, cts.Token, TEST_TIMEOUT, DynamicExecutionMode.Synchronous, true);

                    var value = 13;
                    IEnumerable<Person> people = toTest.usp_StoredProc(value: value);
                    people.Should().BeEmpty("The execution was cancelled.");
                }).ShouldThrow<OperationCanceledException>("because the execution has already been cancelled.");
            }

            [TestMethod]
            public void CanGetMultipleResultSets()
            {
                var ctx = CreateFamily();

                dynamic toTest = new DynamicStoredProcedure(ctx.Object, transformers, CancellationToken.None, TEST_TIMEOUT, DynamicExecutionMode.Synchronous, true);

                Tuple<IEnumerable<Person>, IEnumerable<Family>> results = toTest.usp_GetPeople();

                results.Item1.Single().FirstName.Should().Be("Foo", "First result set not returned.");
                results.Item2.Single().LastName.Should().Be("Bar", "Second result set not returned.");
            }

            [TestMethod]
            public void CanPassUnattributedTableValueParameterClass()
            {
                var reader = new Mock<IDataReader>();
                reader.SetupGet(r => r.FieldCount).Returns(0);
                reader.Setup(r => r.Read()).Returns(false);

                var parms = new DataParameterCollection();
                var cmd = new Mock<IDbCommand>();
                cmd.SetupAllProperties();
                cmd.Setup(c => c.ExecuteReader()).Returns(reader.Object);
                cmd.SetupGet(c => c.Parameters).Returns(parms);
                cmd.Setup(c => c.CreateParameter()).Returns(new SqlParameter());

                var ctx = new Mock<IDbConnection>();
                ctx.Setup(c => c.CreateCommand()).Returns(cmd.Object);
                
                dynamic toTest = new DynamicStoredProcedure(ctx.Object, transformers, CancellationToken.None, TEST_TIMEOUT, DynamicExecutionMode.Synchronous, true);

                toTest.usp_AddPeople(people: new[] { "Foo", "Bar" }.Select(s => new Person { FirstName = s }));

                var p = parms.OfType<SqlParameter>().Single();
                p.ParameterName.Should().Be("people", "because that was the argument name");
                p.SqlDbType.Should().Be(SqlDbType.Structured, "because it is a table-valued parameter");
                p.TypeName.Should().Be("[dbo].[Person]", "because that is the name of the Class being passed as a TVP");
            }

            [TestMethod]
            public void CanNotPassAnonymousClassForTableValueParameter()
            {
                var reader = new Mock<IDataReader>();
                reader.SetupGet(r => r.FieldCount).Returns(0);
                reader.Setup(r => r.Read()).Returns(false);

                var parms = new DataParameterCollection();
                var cmd = new Mock<IDbCommand>();
                cmd.SetupAllProperties();
                cmd.Setup(c => c.ExecuteReader()).Returns(reader.Object);
                cmd.SetupGet(c => c.Parameters).Returns(parms);

                var ctx = new Mock<IDbConnection>();
                ctx.Setup(c => c.CreateCommand()).Returns(cmd.Object);
                
                dynamic toTest = new DynamicStoredProcedure(ctx.Object, transformers, CancellationToken.None, TEST_TIMEOUT, DynamicExecutionMode.Synchronous, true);
                
                this.Invoking(_ =>
                {
                    toTest.usp_AddPeople(people: new[] { "Foo", "Bar" }.Select(s => new { FirstName = s }));
                }).ShouldThrow<NotSupportedException>("because anonymous types should not be allowed to be used as TVPs")
                  .WithMessage("You can not use an anonymous type as a Table-Valued Parameter, since you really need to match the type name with something in the database.", 
                               "because the message should be helpful");
            }

            [TestMethod]
            public void CanNotPassStringsForTableValueParameter()
            {
                var reader = new Mock<IDataReader>();
                reader.SetupGet(r => r.FieldCount).Returns(0);
                reader.Setup(r => r.Read()).Returns(false);

                var parms = new DataParameterCollection();
                var cmd = new Mock<IDbCommand>();
                cmd.SetupAllProperties();
                cmd.Setup(c => c.ExecuteReader()).Returns(reader.Object);
                cmd.SetupGet(c => c.Parameters).Returns(parms);

                var ctx = new Mock<IDbConnection>();
                ctx.Setup(c => c.CreateCommand()).Returns(cmd.Object);

                dynamic toTest = new DynamicStoredProcedure(ctx.Object, transformers, CancellationToken.None, TEST_TIMEOUT, DynamicExecutionMode.Synchronous, true);

                this.Invoking(_ =>
                {
                    toTest.usp_AddPeople(people: new[] { "Foo", "Bar" });
                }).ShouldThrow<NotSupportedException>("because the string type should not be allowed to be used as TVPs")
                  .WithMessage("You can not use a string as a Table-Valued Parameter, since you really need to use a class with properties.",
                               "because the message should be helpful");
            }

            [TestMethod]
            public void CanPassDataTable_AsTableValuedParameter()
            {
                var reader = new Mock<IDataReader>();
                reader.SetupGet(r => r.FieldCount).Returns(0);
                reader.Setup(r => r.Read()).Returns(false);

                var parms = new DataParameterCollection();
                var cmd = new Mock<IDbCommand>();
                cmd.SetupAllProperties();
                cmd.Setup(c => c.ExecuteReader()).Returns(reader.Object);
                cmd.SetupGet(c => c.Parameters).Returns(parms);
                cmd.Setup(c => c.CreateParameter()).Returns(new SqlParameter());

                var ctx = new Mock<IDbConnection>();
                ctx.Setup(c => c.CreateCommand()).Returns(cmd.Object);

                var dt = new DataTable();
                dt.TableName = "[dbo].[Person]";
                dt.Columns.Add("FirstName");
                dt.Rows.Add("Foo");
                dt.Rows.Add("Bar");

                dynamic toTest = new DynamicStoredProcedure(ctx.Object, transformers, CancellationToken.None, TEST_TIMEOUT, DynamicExecutionMode.Synchronous, true);

                toTest.usp_AddPeople(people: dt);

                var p = parms.OfType<SqlParameter>().Single();
                p.ParameterName.Should().Be("people", "because that was the argument name");
                p.SqlDbType.Should().Be(SqlDbType.Structured, "because it is a table-valued parameter");
                p.TypeName.Should().Be("[dbo].[Person]", "because that is the name of the TableName set on the DataTable being passed as a TVP");
            }

            [TestMethod]
            public void CanPassNullParameter()
            {
                var ctx = CreatePeople(parms =>
                {
                    var parm = ((IDbDataParameter)parms[0]);
                    parm.ParameterName.Should().Be("id", "it is the name passed in the stored procedure");
                    parm.Direction.Should().Be(ParameterDirection.Input, "because the parameter is an input parameter");
                    parm.Value.Should().Be(DBNull.Value, "because null was passed");
                });

                dynamic toTest = new DynamicStoredProcedure(ctx, transformers, CancellationToken.None, TEST_TIMEOUT, DynamicExecutionMode.Synchronous, true);
                toTest.usp_GetPeople(id: default(int?));
            }

            [TestMethod]
            public void CanPassDBNullParameter()
            {
                var ctx = CreatePeople(parms =>
                {
                    var parm = ((IDbDataParameter)parms[0]);
                    parm.ParameterName.Should().Be("id", "it is the name passed in the stored procedure");
                    parm.Direction.Should().Be(ParameterDirection.Input, "because the parameter is an input parameter");
                    parm.Value.Should().Be(DBNull.Value, "because DBNull was passed");
                });

                dynamic toTest = new DynamicStoredProcedure(ctx, transformers, CancellationToken.None, TEST_TIMEOUT, DynamicExecutionMode.Synchronous, true);
                toTest.usp_GetPeople(id: DBNull.Value);
            }

            [TestMethod]
            public void UnnamedParameters_Throw_Useful_Exception()
            {
                var ctx = CreatePeople("Foo");
                dynamic toTest = new DynamicStoredProcedure(ctx, transformers, CancellationToken.None, TEST_TIMEOUT, DynamicExecutionMode.Synchronous, true);
                try
                {
                    toTest.usp_GetPeople("foo");
                }
                catch (StoredProcedureException ex)
                {
                    ex.Message.Should().Be(DynamicStoredProcedure.namedParameterException);
                }
            }
        }

        [TestClass]
        public abstract class Asynchronous
        {
            protected abstract Task<IEnumerable<Person>> GetPeople(dynamic toTest);
            protected abstract Task<Person> GetPerson(dynamic toTest);
            protected abstract Task<IEnumerable<Person>> GetPeopleShouldThrow(dynamic toTest, ParameterDirection direction);
            protected abstract Task<IEnumerable<Person>> GetPeople<T>(dynamic toTest, T args);
            protected abstract Task<Tuple<IEnumerable<Person>, IEnumerable<Family>>> GetFamilies(dynamic toTest);
            protected abstract Task Call<T>(dynamic toTest, T args);

            [TestMethod]
            public void CanCallAsyncWithNoArguments()
            {
                var ctx = CreatePeople("Foo");

                var toTest = new DynamicStoredProcedure(ctx, transformers, CancellationToken.None, TEST_TIMEOUT, DynamicExecutionMode.Asynchronous, true);

                var result = GetPeople(toTest).Result;
                result.Single().FirstName.Should().Be("Foo");
            }

            [TestMethod]
            public void ClosesClonedConnectionWhenDone_WithSingleResult()
            {
                var db = new Mock<IDbConnection>();
                var db2 = CreatePeople(_ => { }, "Foo");
                db.As<ICloneable>().Setup(d => d.Clone()).Returns(db2);

                var toTest = new DynamicStoredProcedure(db.Object, transformers, CancellationToken.None, TEST_TIMEOUT, DynamicExecutionMode.Asynchronous, true);

                var result = GetPerson(toTest).Result;

                db.Verify(d => d.Open(), Times.Never());
                db.Verify(d => d.CreateCommand(), Times.Never());
                db.As<ICloneable>().Verify(d => d.Clone(), Times.Once());
                Mock.Get(db2).Verify(d => d.Open(), Times.Once());
                Mock.Get(db2).Verify(d => d.CreateCommand(), Times.Once());
                Mock.Get(db2).Verify(d => d.Close(), Times.Once());
                db.Verify(d => d.Close(), Times.Never());
            }

            [TestMethod]
            public void DisposesCommandWhenDone_WithSingleResult()
            {
                Mock<IDbCommand> cmd;
                var db = CreatePeople(out cmd, "Foo");

                var toTest = new DynamicStoredProcedure(db, transformers, CancellationToken.None, TEST_TIMEOUT, DynamicExecutionMode.Asynchronous, true);

                var result = GetPerson(toTest).Result;
                cmd.Verify(c => c.Dispose(), Times.Once());
            }

            [TestMethod]
            public void ClosesClonedConnectionWhenDone_WithMultipleResults()
            {
                var db = new Mock<IDbConnection>();
                var db2 = CreateFamily();
                db.As<ICloneable>().Setup(d => d.Clone()).Returns(db2.Object);

                var toTest = new DynamicStoredProcedure(db.Object, transformers, CancellationToken.None, TEST_TIMEOUT, DynamicExecutionMode.Asynchronous, true);

                var result = GetFamilies(toTest).Result;

                db2.Verify(d => d.Close(), Times.Once());
                db.Verify(d => d.Close(), Times.Never());
            }

            [TestMethod]
            public void DisposesCommandWhen_Done_WithMultipleResults()
            {
                Mock<IDbCommand> cmd;
                var db = CreateFamily(out cmd);

                var toTest = new DynamicStoredProcedure(db.Object, transformers, CancellationToken.None, TEST_TIMEOUT, DynamicExecutionMode.Asynchronous, true);

                var result = GetPerson(toTest).Result;
                cmd.Verify(c => c.Dispose(), Times.Once());
            }

            [TestMethod]
            public void ClosesClonedConnectionWhenDone_WithResults()
            {
                var db = new Mock<IDbConnection>();
                var db2 = CreatePeople(_ => { }, "Foo");
                db.As<ICloneable>().Setup(d => d.Clone()).Returns(() => db2);

                var toTest = new DynamicStoredProcedure(db.Object, transformers, CancellationToken.None, TEST_TIMEOUT, DynamicExecutionMode.Asynchronous, true);

                var result = GetPeople(toTest).Result;

                db.Verify(d => d.Open(), Times.Never());
                db.Verify(d => d.CreateCommand(), Times.Never());
                db.As<ICloneable>().Verify(d => d.Clone(), Times.Once());
                Mock.Get(db2).Verify(d => d.Open(), Times.Once());
                Mock.Get(db2).Verify(d => d.CreateCommand(), Times.Once());
                Mock.Get(db2).Verify(d => d.Close(), Times.Once());
                db.Verify(d => d.Close(), Times.Never());
            }

            [TestMethod]
            public void DisposesCommandWhen_Done_WithResults()
            {
                Mock<IDbCommand> cmd;
                var db = CreatePeople(out cmd, "Foo");

                var toTest = new DynamicStoredProcedure(db, transformers, CancellationToken.None, TEST_TIMEOUT, DynamicExecutionMode.Asynchronous, true);

                var result = GetPerson(toTest).Result;
                cmd.Verify(c => c.Dispose(), Times.Once());
            }

            [TestMethod]
            public void ClosesClonedConnectionWhenDone_WithoutResults()
            {
                var db = new Mock<IDbConnection>();
                var db2 = CreatePeople("Foo");
                db.As<ICloneable>().Setup(d => d.Clone()).Returns(db2);

                var toTest = new DynamicStoredProcedure(db.Object, transformers, CancellationToken.None, TEST_TIMEOUT, DynamicExecutionMode.Asynchronous, true);

                Call(toTest, new { test = "bar" }).Wait();

                Mock.Get(db2).Verify(d => d.Close(), Times.Once());
                db.Verify(d => d.Close(), Times.Never());
            }

            [TestMethod]
            public void DisposesCommandWhen_Done_WithoutResults()
            {
                Mock<IDbCommand> cmd;
                var db = CreatePeople(out cmd, "Foo");

                var toTest = new DynamicStoredProcedure(db, transformers, CancellationToken.None, TEST_TIMEOUT, DynamicExecutionMode.Asynchronous, true);

                Call(toTest, new { test = "bar" }).Wait();
                cmd.Verify(c => c.Dispose(), Times.Once());
            }

            [TestMethod]
            public void CanGetSingleResultWithoutExpectingIEnumerable()
            {
                var ctx = CreatePeople("Foo");

                var toTest = new DynamicStoredProcedure(ctx, transformers, CancellationToken.None, TEST_TIMEOUT, DynamicExecutionMode.Asynchronous, true);

                var foo = GetPerson(toTest).Result;
                foo.FirstName.Should().Be("Foo", "because that is the name of the only person returned by the Stored Procedure");
            }

            [TestMethod]
            public void DisposesCommandWhen_Done_WithSingleResult()
            {
                Mock<IDbCommand> cmd;
                var db = CreatePeople(out cmd, "Foo");

                var toTest = new DynamicStoredProcedure(db, transformers, CancellationToken.None, TEST_TIMEOUT, DynamicExecutionMode.Asynchronous, true);

                var foo = GetPerson(toTest).Result;
                cmd.Verify(c => c.Dispose(), Times.Once());
            }

            [TestMethod]
            public void CallAsyncWithSimpleReturnValueThrows()
            {
                var ctx = CreatePeople(parms =>
                {
                    var parm = ((IDbDataParameter)parms[0]);
                    Assert.AreEqual(ParameterDirection.ReturnValue, parm.Direction, "Not passed as ReturnValue");
                    parm.Value = 42;
                }, "Foo");

                var toTest = new DynamicStoredProcedure(ctx, transformers, CancellationToken.None, TEST_TIMEOUT, DynamicExecutionMode.Asynchronous, true);

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
                    var parm = ((IDbDataParameter)parms[0]);
                    Assert.AreEqual(ParameterDirection.InputOutput, parm.Direction, "Not passed as InputOutput");
                    Assert.AreEqual("Foo", parm.Value, "Ref value not passed to the stored procedure.");
                    parm.Value = "Bar";
                }, "Foo");

                var toTest = new DynamicStoredProcedure(ctx, transformers, CancellationToken.None, TEST_TIMEOUT, DynamicExecutionMode.Asynchronous, true);

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
                    var parm = ((IDbDataParameter)parms[0]);
                    Assert.AreEqual(ParameterDirection.Output, parm.Direction, "Not passed as Output");
                    parm.Value = 42M;
                }, "Foo");

                var toTest = new DynamicStoredProcedure(ctx, transformers, CancellationToken.None, TEST_TIMEOUT, DynamicExecutionMode.Asynchronous, true);

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
                var ctx = CreateNonQuery(parms =>
                {
                    var parm = ((IDbDataParameter)parms[0]);
                    Assert.AreEqual(ParameterDirection.ReturnValue, parm.Direction, "Not passed as ReturnValue");
                    parm.Value = 42;
                });

                var toTest = new DynamicStoredProcedure(ctx, transformers, CancellationToken.None, TEST_TIMEOUT, DynamicExecutionMode.Asynchronous, false);

                var retValue = new Return();
                Call(toTest, retValue).Wait();

                Assert.AreEqual(42, retValue.Value, "Return value not set.");
            }

            [TestMethod]
            public void CanCallAsyncWithRefParameterNonQuery()
            {
                var ctx = CreateNonQuery(parms =>
                {
                    var parm = ((IDbDataParameter)parms[0]);
                    Assert.AreEqual(ParameterDirection.InputOutput, parm.Direction, "Not passed as InputOutput");
                    Assert.AreEqual(16, (int)parm.Value, "Ref parameter not passed to SP");
                    parm.Value = 42;
                });

                var toTest = new DynamicStoredProcedure(ctx, transformers, CancellationToken.None, TEST_TIMEOUT, DynamicExecutionMode.Asynchronous, false);

                var inputOutput = new InputOutput { Value = 16 };

                Call(toTest, inputOutput).Wait();

                Assert.AreEqual(42, inputOutput.Value, "Ref parameter not set.");
            }

            [TestMethod]
            public void CanCallAsyncWithOutParameterNonQuery()
            {
                var ctx = CreateNonQuery(parms =>
                {
                    var parm = ((IDbDataParameter)parms[0]);
                    Assert.AreEqual(ParameterDirection.Output, parm.Direction, "Not passed as Output");
                    parm.Value = 42;
                });

                var toTest = new DynamicStoredProcedure(ctx, transformers, CancellationToken.None, TEST_TIMEOUT, DynamicExecutionMode.Asynchronous, false);

                var output = new Output();

                Call(toTest, output).Wait();

                Assert.AreEqual(42, output.Value, "Out parameter not set.");
            }

            [TestMethod]
            public void CanExecuteAsyncWithReturnValue()
            {
                var ctx = CreatePeople(parms =>
                {
                    var parm = ((IDbDataParameter)parms[0]);
                    Assert.AreEqual(ParameterDirection.ReturnValue, parm.Direction, "Not passed as ReturnValue");
                    parm.Value = 42;
                }, "Foo", "Bar");

                var toTest = new DynamicStoredProcedure(ctx, transformers, CancellationToken.None, TEST_TIMEOUT, DynamicExecutionMode.Asynchronous, true);

                var retValue = new Return();
                var people = GetPeople(toTest, retValue).Result;

                Assert.AreEqual(42, retValue.Value, "Return value not set.");
                Assert.IsTrue(people.Select(p => p.FirstName).SequenceEqual(new[] { "Foo", "Bar" }));
            }

            [TestMethod]
            public void CanExecuteAsyncWithRefParameterValue()
            {
                var ctx = CreatePeople(parms =>
                {
                    var parm = ((IDbDataParameter)parms[0]);
                    Assert.AreEqual(ParameterDirection.InputOutput, parm.Direction, "Not passed as InputOutput");
                    Assert.AreEqual(22, (int)parm.Value, "Ref parameter not passed to SP");
                    parm.Value = 42;
                }, "Bar", "Baz");

                var toTest = new DynamicStoredProcedure(ctx, transformers, CancellationToken.None, TEST_TIMEOUT, DynamicExecutionMode.Asynchronous, true);

                var inout = new InputOutput { Value = 22 };
                var people = GetPeople(toTest, inout).Result;

                Assert.AreEqual(42, inout.Value, "Ref parameter not set.");
                Assert.IsTrue(people.Select(p => p.FirstName).SequenceEqual(new[] { "Bar", "Baz" }));
            }

            [TestMethod]
            public void CanExecuteAsyncWithOutParameterValue()
            {
                var ctx = CreatePeople(parms =>
                {
                    var parm = ((IDbDataParameter)parms[0]);
                    Assert.AreEqual(ParameterDirection.Output, parm.Direction, "Not passed as Output");
                    parm.Value = 42;
                }, "Bar", "Baz");

                var toTest = new DynamicStoredProcedure(ctx, transformers, CancellationToken.None, TEST_TIMEOUT, DynamicExecutionMode.Asynchronous, true);

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
                reader.Setup(r => r.GetFieldType(0)).Returns(typeof(string));
                reader.SetupSequence(r => r.Read())
                      .Returns(true)
                      .Returns(false)
                      .Returns(true)
                      .Returns(false);
                reader.Setup(r => r.GetString(0))
                      .Returns(() => resultSet == 0 ? "Foo" : "Bar");
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

                var toTest = new DynamicStoredProcedure(ctx.Object, transformers, CancellationToken.None, TEST_TIMEOUT, DynamicExecutionMode.Asynchronous, true);
                
                var results = GetFamilies(toTest).Result;

                Assert.AreEqual("Foo", results.Item1.Single().FirstName, "First result set not returned.");
                Assert.AreEqual("Bar", results.Item2.Single().LastName, "Second result set not returned.");
            }
        }

        [TestClass]
        public class AsyncSyntax : Asynchronous
        {
            [TestMethod]
            public void ConfigureAwaitControlsThreadContinuationHappensOn()
            {
                // sleep so the task won't get inlined
                var ctx = CreatePeople(_ => Thread.Sleep(250), "Foo");

                var toTest = new DynamicStoredProcedure(ctx, transformers, CancellationToken.None, 400, DynamicExecutionMode.Asynchronous, true);

                var res = GetPeopleInBackground(toTest).Result;

                res.Should()
                   .ContainSingle("only one row should be returned")
                   .Which.FirstName.Should().Be("Foo", "that is the FirstName of the item returned");
            }

            [TestMethod]
#if NET40
            public void CanCastExplicitly() { CanCastExplicitlyWrapper().Wait(); }
            public async Task CanCastExplicitlyWrapper()
#else
            public async Task CanCastExplicitly()
#endif
            {
                var ctx = CreatePeople("Foo");

                dynamic toTest = new DynamicStoredProcedure(ctx, transformers, CancellationToken.None, TEST_TIMEOUT, DynamicExecutionMode.Asynchronous, true);

                var people = await (Task<IEnumerable<Person>>)toTest.usp_GetPeople();

                people.Should()
                      .ContainSingle("only one row should be returned")
                      .Which.FirstName.Should().Be("Foo", "that is the FirstName of the item returned");
            }

            [TestMethod]
#if NET40
            public void CanConfigureAwait() { CanConfigureAwaitWrapper().Wait(); }
            public async Task CanConfigureAwaitWrapper()
#else
            public async Task CanConfigureAwait()
#endif
            {
                var lockr = new SemaphoreSlim(0);
                int count = 0;
                var sync  = new Mock<SynchronizationContext>();

                sync.Setup(sc => sc.Post(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()))
                    .Callback<SendOrPostCallback, object>((c, o) =>
                    {
                        ++count;
                        c(o);
                    });

                var oldContext = SynchronizationContext.Current;
                SynchronizationContext.SetSynchronizationContext(sync.Object);

                var ctx = CreatePeople(_ => lockr.Wait(TimeSpan.FromMilliseconds(100)), "Foo");
                dynamic toTest = new DynamicStoredProcedure(ctx, transformers, CancellationToken.None, TEST_TIMEOUT, DynamicExecutionMode.Asynchronous, true);
                IEnumerable<Person> res = await toTest.usp_GetPeople().ConfigureAwait(true);

                SynchronizationContext.SetSynchronizationContext(oldContext);

                res.Should()
                   .ContainSingle("only one row should be returned")
                   .Which.FirstName.Should().Be("Foo", "that is the FirstName of the item returned");
                count.Should().Be(1, "the callback should be posted to the SynchronizationContext");
            }

            private async Task<IEnumerable<Person>> GetPeopleInBackground(dynamic toTest)
            {
                var entryThread = Thread.CurrentThread;

                var results = await toTest.usp_GetPeople().ConfigureAwait(false);

                Thread.CurrentThread.Should().NotBe(entryThread, "because the await should have configured the continuation to run on a different thread");

                return results;
            }

            protected override async Task<IEnumerable<Person>> GetPeople(dynamic toTest)
            {
                return await toTest.usp_GetPeople();
            }

            protected override async Task<Person> GetPerson(dynamic toTest)
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
                (await toTest.usp_StoredProc(args)).Dispose();
            }
        }

        [TestClass]
        public class TaskSyntax : Asynchronous
        {
            [TestMethod]
            public void CanCastExplicitly()
            {
                var ctx = CreatePeople("Foo");

                dynamic toTest = new DynamicStoredProcedure(ctx, transformers, CancellationToken.None, TEST_TIMEOUT, DynamicExecutionMode.Asynchronous, true);

                var people = (Task<IEnumerable<Person>>)toTest.usp_GetPeople();

                Assert.AreEqual("Foo", people.Result.Single().FirstName);
            }

            protected override Task<IEnumerable<Person>> GetPeople(dynamic toTest)
            {
                return toTest.usp_GetPeople();
            }

            protected override Task<Person> GetPerson(dynamic toTest)
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
            return CreatePeople(_ => { }, new Mock<IDbCommand>(), names);
        }

        private static IDbConnection CreatePeople(out Mock<IDbCommand> command, params string[] names)
        {
            command = new Mock<IDbCommand>();
            return CreatePeople(_ => { }, command, names);
        }

        private static IDbConnection CreatePeople(Action<DataParameterCollection> readerCallback, params string[] names)
        {
            return CreatePeople(readerCallback, new Mock<IDbCommand>(), names);
        }

        private static IDbConnection CreateNonQuery(Action<DataParameterCollection> readerCallback, int returnValue = 0)
        {
            var db = new Mock<IDbConnection>();
            var cmd = new Mock<IDbCommand>();
            db.Setup(d => d.CreateCommand()).Returns(cmd.Object);
            cmd.SetupAllProperties();
            var parms = SetupParameters(cmd);
            cmd.Setup(c => c.ExecuteNonQuery()).Callback(() => readerCallback(parms)).Returns(returnValue);

            return db.Object;
        }

        private static IDbConnection CreatePeople(Action<DataParameterCollection> readerCallback, Mock<IDbCommand> cmd, params string[] names)
        {
            var reader = new Mock<IDataReader>();
            reader.SetupGet(r => r.FieldCount).Returns(1);
            reader.Setup(r => r.GetName(0)).Returns("FirstName");
            reader.Setup(r => r.GetFieldType(0)).Returns(typeof(string));

            var setup = reader.SetupSequence(r => r.Read());

            var idx = 0;
            reader.Setup(r => r.GetValue(0))
                  .Returns(() => names[idx++]);
            reader.Setup(r => r.GetString(0))
                  .Returns(() => names[idx++]);
            reader.Setup(r => r.IsDBNull(It.IsAny<int>()))
                  .Returns((int i) => names[i] == null);

            for (int i = 0; i < names.Length; ++i)
                setup = setup.Returns(true);

            setup.Returns(false);
            cmd.SetupAllProperties();
            var parms = SetupParameters(cmd);
            cmd.Setup(c => c.ExecuteReader())
               .Callback(() => readerCallback(parms))
               .Returns(reader.Object);

            var ctx = new Mock<IDbConnection>();
            ctx.Setup(c => c.CreateCommand())
               .Returns(cmd.Object);

            return ctx.Object;
        }

        private static DataParameterCollection SetupParameters(Mock<IDbCommand> cmd)
        {
            var parms = new DataParameterCollection();
            cmd.SetupGet(c => c.Parameters)
               .Returns(parms);
            cmd.Setup(c => c.CreateParameter())
               .Returns(() =>
               {
                   var m = new Mock<IDbDataParameter>();
                   m.SetupAllProperties();

                   return m.Object;
               });
            return parms;
        }

        private static Mock<IDbConnection> CreateFamily(out Mock<IDbCommand> cmd)
        {
            cmd = new Mock<IDbCommand>();
            return CreateFamily(cmd);
        }

        private static Mock<IDbConnection> CreateFamily(Mock<IDbCommand> cmd = null)
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
            reader.Setup(r => r.GetFieldType(It.IsAny<int>())).Returns(typeof(string));
            reader.Setup(r => r.GetString(0))
                  .Returns(() => resultSet == 0 ? "Foo" : "Bar");
            reader.Setup(r => r.NextResult())
                  .Callback(() => ++resultSet)
                  .Returns(() => resultSet < 2);

            var parms = new DataParameterCollection();
            cmd = cmd ?? new Mock<IDbCommand>();
            cmd.Setup(c => c.ExecuteReader())
               .Returns(reader.Object);
            cmd.Setup(c => c.Parameters)
               .Returns(parms);

            var ctx = new Mock<IDbConnection>();
            ctx.Setup(c => c.CreateCommand())
               .Returns(cmd.Object);
            return ctx;
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
