using CodeOnlyStoredProcedure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;

#if NET40
namespace CodeOnlyTests.Net40
#else
namespace CodeOnlyTests
#endif
{
    [TestClass]
    public class StoredProcedureTests
    {
        private static readonly Type _basic = typeof(StoredProcedure);
        private static readonly Type _one   = typeof(StoredProcedure<>);
        private static readonly Type _two   = typeof(StoredProcedure<,>);
        private static readonly Type _three = typeof(StoredProcedure<,,>);
        private static readonly Type _four  = typeof(StoredProcedure<,,,>);
        private static readonly Type _five  = typeof(StoredProcedure<,,,,>);
        private static readonly Type _six   = typeof(StoredProcedure<,,,,,>);
        private static readonly Type _seven = typeof(StoredProcedure<,,,,,,>);

        private static readonly Type[] _generics = { _one, _two, _three, _four, _five, _six, _seven };

        #region Constructor Tests
        [TestMethod]
        public void TestConstructorSetsNameAndDefaultSchema()
        {
            TestConstructorWithDefaultSchema(_basic, "TestProc");

            for (int i = 0; i < 7; i++)
            {
                var type = _generics[i].MakeGenericType(Enumerable.Range(0, i + 1)
                                                                  .Select(_ => typeof(int))
                                                                  .ToArray());

                TestConstructorWithDefaultSchema(type, "TestProc" + i);
            }
        }

        [TestMethod]
        public void TestConstructorSetsNameAndSchema()
        {
            TestConstructorWithSchema(_basic, "tEst", "Proc");

            for (int i = 0; i < 7; i++)
            {
                var type = _generics[i].MakeGenericType(Enumerable.Range(0, i + 1)
                                                                  .Select(_ => typeof(int))
                                                                  .ToArray());

                TestConstructorWithSchema(type, "tEst", "TestProc" + i);
            }
        }

        [TestMethod]
        public void CanConstructAllGenericStoredProceduresForAllIntegralTypes()
        {
            StoredProcedure sp;
            Type type;
            int parameterCount = 1;

            foreach (var t in _generics)
            {
                foreach (var tt in CodeOnlyStoredProcedure.TypeExtensions.integralTypes)
                {
                    type = t.MakeGenericType(Enumerable.Range(0, parameterCount)
                                                        .Select(_ => tt)
                                                        .ToArray());
                    sp = (StoredProcedure)Activator.CreateInstance(type, "usp_Test");

                    // this would have thrown if the type wasn't allowed
                    Assert.IsNotNull(sp);
                }

                // try it with an enum value, too
                type = t.MakeGenericType(Enumerable.Range(0, parameterCount)
                                                   .Select(_ => typeof(ParameterDirection))
                                                   .ToArray());
                sp = (StoredProcedure)Activator.CreateInstance(type, "usp_Test");

                // this would have thrown if the type wasn't allowed
                Assert.IsNotNull(sp);

                ++parameterCount;
            }
        }

        [TestMethod]
        public void CanNotConstructAnyGenericStoredProcedureForResultWithoutDefaultConstructor()
        {
            int parameterCount = 1;

            foreach (var t in _generics)
            {
                foreach (var tt in CodeOnlyStoredProcedure.TypeExtensions.integralTypes)
                {
                    try
                    {
                        var type = t.MakeGenericType(Enumerable.Range(0, parameterCount)
                                                               .Select(_ => typeof(NoDefaultCtor))
                                                               .ToArray());
                        var sp = (StoredProcedure)Activator.CreateInstance(type, "usp_Test");
                        Assert.Fail("Expected exception not thrown.");
                    }
                    catch (TargetInvocationException ex)
                    {
                        // This exception is a ContractException but, since it isn't a public 
                        // class, we can't catch it directly.
                        Assert.AreEqual("Precondition failed: typeof(T1).IsValidResultType()",
                                        ex.InnerException.Message);
                    }
                }

                ++parameterCount;
            }
        }

        private static void TestConstructorWithDefaultSchema(Type type, string name)
        {
            // this will actually call the SP's ctor
            var sp = (StoredProcedure)Activator.CreateInstance(type, name);
            AssertProcValues(sp, type, "dbo", name, 0, 0);
        }

        private static void TestConstructorWithSchema(Type type, string schema, string name)
        {
            // this will actually call the SP's ctor
            var sp = (StoredProcedure)Activator.CreateInstance(type, schema, name);
            AssertProcValues(sp, type, schema, name, 0, 0);
        }
        #endregion

        #region Create Tests
        [TestMethod]
        public void TestCreateSetsNameAndDefaultSchema()
        {
            var sp = StoredProcedure.Create("procMe");

            Assert.AreEqual("procMe", sp.Name);
            Assert.AreEqual("dbo", sp.Schema);
            Assert.AreEqual("[dbo].[procMe]", sp.FullName);
            Assert.AreEqual(0, sp.Parameters.Count());
        }

        [TestMethod]
        public void TestCreateSetsNameAndSchema()
        {
            var sp = StoredProcedure.Create("dummy", "usp_test");

            Assert.AreEqual("dummy", sp.Schema);
            Assert.AreEqual("usp_test", sp.Name);
            Assert.AreEqual("[dummy].[usp_test]", sp.FullName);
            Assert.AreEqual(0, sp.Parameters.Count());
        } 
        #endregion

        #region CloneCore Tests
        [TestMethod]
        public void TestCloneCore_CreatesNewStoredProcedureWithParameters()
        {
            var p1 = Mock.Of<IStoredProcedureParameter>(p => p.ParameterName == "Foo");
            var p2 = Mock.Of<IStoredProcedureParameter>(p => p.ParameterName == "Bar");

            var parms = new[] { p1, p2 };
            var transformers = new IDataTransformer[0];

            var sp = new StoredProcedure("schema", "Test");
            var toTest = sp.CloneCore(parms, transformers);

            Assert.AreEqual("schema", toTest.Schema, "Schema not cloned");
            Assert.AreEqual("Test", toTest.Name, "Name not cloned");
            CollectionAssert.AreEquivalent(
                new[] { p1, p2 }, 
                toTest.Parameters.ToArray(), 
                "Parameters are not equal");
            CollectionAssert.AreEquivalent(
                new IDataParameter[0],
                toTest.DataTransformers.ToArray(),
                "DataTransformers are not equal");
        }
        #endregion

        #region CloneWith Tests
        [TestMethod]
        public void TestCloneWithDoesNotAlterOriginalProcedure()
        {
            var sp = new StoredProcedure("test", "proc");

            var toTest = sp.CloneWith(Mock.Of<IStoredProcedureParameter>());

            Assert.AreEqual("test", sp.Schema);
            Assert.AreEqual("proc", sp.Name);
            Assert.AreEqual("[test].[proc]", sp.FullName);
            Assert.AreEqual(0, sp.Parameters.Count());
            Assert.AreEqual(0, sp.DataTransformers.Count());
        }

        [TestMethod]
        public void TestCloneWithTransformerDoesNotAlterOriginalProcedure()
        {
            var sp = new StoredProcedure("test", "proc");

            var toTest = sp.CloneWith(Mock.Of<IDataTransformer>());

            Assert.AreEqual("test", sp.Schema);
            Assert.AreEqual("proc", sp.Name);
            Assert.AreEqual("[test].[proc]", sp.FullName);
            Assert.AreEqual(0, sp.Parameters.Count());
            Assert.AreEqual(0, sp.DataTransformers.Count());
        }

        [TestMethod]
        public void TestCloneWithRetainsNameAndDefaultSchema()
        {
            var sp = new StoredProcedure("test_proc");

            var toTest = sp.CloneWith(Mock.Of<IStoredProcedureParameter>());

            Assert.AreEqual("dbo", toTest.Schema);
            Assert.AreEqual("test_proc", toTest.Name);
            Assert.AreEqual("[dbo].[test_proc]", toTest.FullName);
            Assert.AreEqual(1, toTest.Parameters.Count());
            Assert.AreEqual(0, toTest.DataTransformers.Count());
        }

        [TestMethod]
        public void TestCloneWithRetainsNameAndSchema()
        {
            var sp = new StoredProcedure("test", "proc");

            var toTest = sp.CloneWith(Mock.Of<IStoredProcedureParameter>());

            Assert.AreEqual("test", toTest.Schema);
            Assert.AreEqual("proc", toTest.Name);
            Assert.AreEqual("[test].[proc]", toTest.FullName);
            Assert.AreEqual(1, toTest.Parameters.Count());
            Assert.AreEqual(0, toTest.DataTransformers.Count());
        }

        [TestMethod]
        public void TestCloneWithTransformerRetainsNameAndDefaultSchema()
        {
            var sp = new StoredProcedure("test_proc");

            var toTest = sp.CloneWith(new Mock<IDataTransformer>().Object);

            Assert.AreEqual("dbo", toTest.Schema);
            Assert.AreEqual("test_proc", toTest.Name);
            Assert.AreEqual("[dbo].[test_proc]", toTest.FullName);
            Assert.AreEqual(0, toTest.Parameters.Count());
            Assert.AreEqual(1, toTest.DataTransformers.Count());
        }

        [TestMethod]
        public void TestCloneWithTransformerRetainsNameAndSchema()
        {
            var sp = new StoredProcedure("test", "proc");

            var toTest = sp.CloneWith(new Mock<IDataTransformer>().Object);

            Assert.AreEqual("test", toTest.Schema);
            Assert.AreEqual("proc", toTest.Name);
            Assert.AreEqual("[test].[proc]", toTest.FullName);
            Assert.AreEqual(0, toTest.Parameters.Count());
            Assert.AreEqual(1, toTest.DataTransformers.Count());
        }

        [TestMethod]
        public void TestCloneWithStoresParameter()
        {
            var p1 = Mock.Of<IStoredProcedureParameter>(p => p.ParameterName == "Parm");

            var toTest = new StoredProcedure("test").CloneWith(p1);

            Assert.AreEqual(p1, toTest.Parameters.Single());
        }
        
        [TestMethod]
        public void TestCloneWithTransformerStoresTransformer()
        {
            var x = new Mock<IDataTransformer>().Object;

            var toTest = new StoredProcedure("Test").CloneWith(x);

            Assert.AreEqual(x, toTest.DataTransformers.Single());
        }
        #endregion

        #region Execute Tests
        [TestMethod]
        public void TestExecuteProperlyCreatesAndExecutesCommand()
        {
            var cmd = new Mock<IDbCommand>();
            cmd.SetupAllProperties();

            var conn = new Mock<IDbConnection>();
            conn.Setup(c => c.CreateCommand())
                .Returns(cmd.Object);

            var toTest = new StoredProcedure("foo", "bar");

            toTest.Execute(conn.Object, 100);

            Assert.AreEqual("[foo].[bar]", cmd.Object.CommandText);
            Assert.AreEqual(CommandType.StoredProcedure, cmd.Object.CommandType);
            Assert.AreEqual(100, cmd.Object.CommandTimeout);

            cmd.Verify(c => c.ExecuteNonQuery(), Times.Once());
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
                  .Returns("Id");
            reader.Setup(r => r.GetString(0)).Returns((int _) => results[index]);
            reader.Setup(r => r.GetFieldType(0)).Returns(typeof(string));

            var connection = new Mock<IDbConnection>();
            connection.Setup(c => c.CreateCommand()).Returns(command.Object);

            var sp = new StoredProcedure<InterfaceImpl>("foo");
            var res = sp.Execute(connection.Object);

            var idx = 0;
            foreach (var toTest in res)
                toTest.ShouldBeEquivalentTo(new InterfaceImpl { Id = results[idx++] });
        }
        #endregion

        #region ExecuteAsync Tests
#if NET40
        [TestMethod]
        public void TestExecuteAsync_DoesNotExecuteMethodFromSameThreadWhenRunningInSynchronizationContext()
        {            
            var reader = new Mock<IDataReader>();
            var command = new Mock<IDbCommand>();
            var ctx = new TestSynchronizationContext();

            SynchronizationContext.SetSynchronizationContext(ctx);

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
                  .Returns("Id");
            reader.Setup(r => r.GetString(0)).Returns((int _) => results[index]);
            reader.Setup(r => r.GetFieldType(0)).Returns(typeof(string));

            var connection = new Mock<IDbConnection>();
            connection.Setup(c => c.CreateCommand())
                      .Callback(() => ctx.Worker.Should().NotBeSameAs(Thread.CurrentThread, "Command called from the same thread as the reentrant Task."))
                      .Returns(command.Object);

            var sp = new StoredProcedure<InterfaceImpl>("foo");
            var res = sp.ExecuteAsync(connection.Object).Result;

            var idx = 0;
            foreach (var toTest in res)
                toTest.ShouldBeEquivalentTo(new InterfaceImpl { Id = results[idx++] });

            SynchronizationContext.SetSynchronizationContext(null);
        }
#else
        [TestMethod]
        public async Task TestExecuteAsync_DoesNotExecuteMethodFromSameThreadWhenRunningInSynchronizationContext()
        {
            var reader = new Mock<IDataReader>();
            var command = new Mock<IDbCommand>();
            var ctx = new TestSynchronizationContext();

            SynchronizationContext.SetSynchronizationContext(ctx);

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
                  .Returns("Id");
            reader.Setup(r => r.GetString(0)).Returns((int _) => results[index]);
            reader.Setup(r => r.GetFieldType(0)).Returns(typeof(string));

            var connection = new Mock<IDbConnection>();
            connection.Setup(c => c.CreateCommand())
                      .Callback(() => ctx.Worker.Should().NotBeSameAs(Thread.CurrentThread, "Command called from the same thread as the reentrant Task."))
                      .Returns(command.Object);

            var sp = new StoredProcedure<InterfaceImpl>("foo");
            var res = await sp.ExecuteAsync(connection.Object);

            var idx = 0;
            foreach (var toTest in res)
                toTest.ShouldBeEquivalentTo(new InterfaceImpl { Id = results[idx++] });

            SynchronizationContext.SetSynchronizationContext(null);
        }
#endif
        #endregion

        #region PrettyPrint Tests
        [TestMethod]
        public void TestFullName_IncludesSchema()
        {
            var sp = new StoredProcedure("foo", "blah");

            Assert.AreEqual("[foo].[blah]", sp.FullName);
        }

        [TestMethod]
        public void TestArguments_PrintsSingleInputArgument()
        {
            var sp = new StoredProcedure("foo").WithInput(new { foo = "Bar" });

            Assert.AreEqual("@foo = 'Bar'", sp.Arguments);
        }

        [TestMethod]
        public void TestArguments_PrintsMultipleInputArguments()
        {
            var sp = new StoredProcedure("foo").WithInput(new { foo = "Bar", date = DateTime.Today });

            Assert.AreEqual("@foo = 'Bar', @date = '" + DateTime.Today.ToString() + "'", sp.Arguments);
        }

        [TestMethod]
        public void TestToString_PrintsFullName()
        {
            var sp = new StoredProcedure("foo", "blah");

            Assert.AreEqual("[foo].[blah]", sp.ToString());
        }

        [TestMethod]
        public void TestToString_PrintsSingleInputArgument()
        {
            var sp = new StoredProcedure("foo", "blah").WithInput(new { foo = "Bar" });

            Assert.AreEqual("[foo].[blah](@foo = 'Bar')", sp.ToString());
        }

        [TestMethod]
        public void TestToString_PrintsMultipleInputArguments()
        {
            var sp = new StoredProcedure("foo", "blah").WithInput(new { foo = "Bar", date = DateTime.Today });

            Assert.AreEqual("[foo].[blah](@foo = 'Bar', @date = '" + DateTime.Today.ToString() + "')", sp.ToString());
        }
        #endregion

        #region MapResultType Tests
        [TestMethod]
        public void UnMappedInterface_ThrowsWhenConstructingStoredProcedure()
        {
            lock (CodeOnlyStoredProcedure.TypeExtensions.interfaceMap)
            {
                // clear any mapped interfaces
                CodeOnlyStoredProcedure.TypeExtensions.interfaceMap.Clear();
                try
                {
                    var sp = new StoredProcedure<Interface>("foo");
                    Assert.Fail("Did not throw exception when constructing an unmapped interface");
                }
                catch (Exception ex)
                {
                    // This exception is a ContractException but, since it isn't a public 
                    // class, we can't catch it directly.
                    Assert.AreEqual("Precondition failed: typeof(T1).IsValidResultType()",
                                    ex.Message);
                }
            }
        }
        #endregion

        private static void AssertProcValues(
            StoredProcedure proc,
            Type procType,
            string schema,
            string name,
            int parmCount, 
            int outputCount)
        {
            Assert.AreEqual(procType, proc.GetType());
            Assert.AreEqual(schema, proc.Schema);
            Assert.AreEqual(name, proc.Name);
            Assert.AreEqual(String.Format("[{0}].[{1}]", schema, name), proc.FullName);
            Assert.AreEqual(parmCount, proc.Parameters.Count());
        }

        private class NoDefaultCtor
        {
            public string Id { get; private set; }

            public NoDefaultCtor(string id)
            {
                Id = id;
            }
        }

        private interface Interface
        {
            string Id { get; set; }
        }

        private class InterfaceImpl : Interface
        {
            public string Id { get; set; }
        }
    }
}
