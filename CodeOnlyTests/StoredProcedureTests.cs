using CodeOnlyStoredProcedure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
#if !NET40
using System.Collections.Immutable;
#endif
using System.Collections.Generic;
using System.Collections.Concurrent;

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
                foreach (var tt in TypeExtensions.integralTpes)
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
                foreach (var tt in TypeExtensions.integralTpes)
                {
                    try
                    {
                        var type = t.MakeGenericType(Enumerable.Range(0, parameterCount)
                                                               .Select(_ => typeof(NoDefaultCtor))
                                                               .ToArray());
                        var sp = (StoredProcedure)Activator.CreateInstance(type, "usp_Test");
                        Assert.Fail("Expected exception not thrown.");
                    }
                    catch(TypeInitializationException ex)
                    {
                        Assert.IsInstanceOfType(ex.InnerException, typeof(NotSupportedException));
                        Assert.AreEqual("Stored Procedure result must either be a built in type, or have a parameterless constructor.",
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
            Assert.AreEqual(0, sp.OutputParameterSetters.Count());
        }

        [TestMethod]
        public void TestCreateSetsNameAndSchema()
        {
            var sp = StoredProcedure.Create("dummy", "usp_test");

            Assert.AreEqual("dummy", sp.Schema);
            Assert.AreEqual("usp_test", sp.Name);
            Assert.AreEqual("[dummy].[usp_test]", sp.FullName);
            Assert.AreEqual(0, sp.Parameters.Count());
            Assert.AreEqual(0, sp.OutputParameterSetters.Count());
        } 
        #endregion

        #region CloneCore Tests
        [TestMethod]
        public void TestCloneCore_CreatesNewStoredProcedureWithParameters()
        {
            var p1 = new SqlParameter("Foo", 12);
            var p2 = new SqlParameter("Bar", 42.0);

#if NET40
            var parms = new[] { p1, p2 };
            var outputParms = new ReadOnlyDictionary<string, Action<object>>(
                new Dictionary<string, Action<object>>());
            var transformers = new IDataTransformer[0];
#else
            var parms = ImmutableList<SqlParameter>.Empty
                                                   .Add(p1)
                                                   .Add(p2);
            var outputParms = ImmutableDictionary<string, Action<object>>.Empty;
            var transformers = ImmutableList<IDataTransformer>.Empty;
#endif
            var sp = new StoredProcedure("schema", "Test");
            var toTest = sp.CloneCore(parms, outputParms, transformers);

            Assert.AreEqual("schema", toTest.Schema, "Schema not cloned");
            Assert.AreEqual("Test", toTest.Name, "Name not cloned");
            CollectionAssert.AreEquivalent(
                new[] { p1, p2 }, 
                toTest.Parameters.ToArray(), 
                "Parameters are not equal");
            CollectionAssert.AreEquivalent(
                new Dictionary<string, Action<object>>(),
                toTest.OutputParameterSetters.ToArray(), 
                "Output Parameters are not eqaul");
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

            var toTest = sp.CloneWith(new SqlParameter());

            Assert.AreEqual("test", sp.Schema);
            Assert.AreEqual("proc", sp.Name);
            Assert.AreEqual("[test].[proc]", sp.FullName);
            Assert.AreEqual(0, sp.Parameters.Count());
            Assert.AreEqual(0, sp.OutputParameterSetters.Count());
            Assert.AreEqual(0, sp.DataTransformers.Count());
        }

        [TestMethod]
        public void TestCloneWithOutputParameterDoesNotAlterOriginalProcedure()
        {
            var sp = new StoredProcedure("test", "proc");

            var toTest = sp.CloneWith(new SqlParameter(), o => { });

            Assert.AreEqual("test", sp.Schema);
            Assert.AreEqual("proc", sp.Name);
            Assert.AreEqual("[test].[proc]", sp.FullName);
            Assert.AreEqual(0, sp.Parameters.Count());
            Assert.AreEqual(0, sp.OutputParameterSetters.Count());
            Assert.AreEqual(0, sp.DataTransformers.Count());
        }

        [TestMethod]
        public void TestCloneWithTransformerDoesNotAlterOriginalProcedure()
        {
            var sp = new StoredProcedure("test", "proc");

            var toTest = sp.CloneWith(new Mock<IDataTransformer>().Object);

            Assert.AreEqual("test", sp.Schema);
            Assert.AreEqual("proc", sp.Name);
            Assert.AreEqual("[test].[proc]", sp.FullName);
            Assert.AreEqual(0, sp.Parameters.Count());
            Assert.AreEqual(0, sp.OutputParameterSetters.Count());
            Assert.AreEqual(0, sp.DataTransformers.Count());
        }

        [TestMethod]
        public void TestCloneWithRetainsNameAndDefaultSchema()
        {
            var sp = new StoredProcedure("test_proc");

            var toTest = sp.CloneWith(new SqlParameter());

            Assert.AreEqual("dbo", toTest.Schema);
            Assert.AreEqual("test_proc", toTest.Name);
            Assert.AreEqual("[dbo].[test_proc]", toTest.FullName);
            Assert.AreEqual(1, toTest.Parameters.Count());
            Assert.AreEqual(0, toTest.OutputParameterSetters.Count());
            Assert.AreEqual(0, toTest.DataTransformers.Count());
        }

        [TestMethod]
        public void TestCloneWithRetainsNameAndSchema()
        {
            var sp = new StoredProcedure("test", "proc");

            var toTest = sp.CloneWith(new SqlParameter());

            Assert.AreEqual("test", toTest.Schema);
            Assert.AreEqual("proc", toTest.Name);
            Assert.AreEqual("[test].[proc]", toTest.FullName);
            Assert.AreEqual(1, toTest.Parameters.Count());
            Assert.AreEqual(0, toTest.OutputParameterSetters.Count());
            Assert.AreEqual(0, toTest.DataTransformers.Count());
        }

        [TestMethod]
        public void TestCloneWithOutputParameterRetainsNameAndDefaultSchema()
        {
            var sp = new StoredProcedure("test_proc");

            var toTest = sp.CloneWith(new SqlParameter(), o => { });

            Assert.AreEqual("dbo", toTest.Schema);
            Assert.AreEqual("test_proc", toTest.Name);
            Assert.AreEqual("[dbo].[test_proc]", toTest.FullName);
            Assert.AreEqual(1, toTest.Parameters.Count());
            Assert.AreEqual(1, toTest.OutputParameterSetters.Count());
            Assert.AreEqual(0, toTest.DataTransformers.Count());
        }

        [TestMethod]
        public void TestCloneWithOutputParameterRetainsNameAndSchema()
        {
            var sp = new StoredProcedure("test", "proc");

            var toTest = sp.CloneWith(new SqlParameter(), o => { });

            Assert.AreEqual("test", toTest.Schema);
            Assert.AreEqual("proc", toTest.Name);
            Assert.AreEqual("[test].[proc]", toTest.FullName);
            Assert.AreEqual(1, toTest.Parameters.Count());
            Assert.AreEqual(1, toTest.OutputParameterSetters.Count());
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
            Assert.AreEqual(0, toTest.OutputParameterSetters.Count());
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
            Assert.AreEqual(0, toTest.OutputParameterSetters.Count());
            Assert.AreEqual(1, toTest.DataTransformers.Count());
        }

        [TestMethod]
        public void TestCloneWithStoresParameter()
        {
            var p = new SqlParameter { ParameterName = "Parm" };

            var toTest = new StoredProcedure("test").CloneWith(p);

            Assert.AreEqual(p, toTest.Parameters.Single());
        }

        [TestMethod]
        public void TestCloneWithStoresParameterAndSetter()
        {
            var p = new SqlParameter { ParameterName = "Parm" };
            var a = new Action<object>(o => { });

            var toTest = new StoredProcedure("test").CloneWith(p, a);

            Assert.AreEqual(p, toTest.Parameters.Single());
            var kv = toTest.OutputParameterSetters.Single();
            Assert.AreEqual("Parm", kv.Key);
            Assert.AreEqual(a, kv.Value);
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

            var res = toTest.Execute(conn.Object, CancellationToken.None, 100);

            Assert.AreEqual(0, res.Count);
            Assert.AreEqual("[foo].[bar]", cmd.Object.CommandText);
            Assert.AreEqual(CommandType.StoredProcedure, cmd.Object.CommandType);
            Assert.AreEqual(100, cmd.Object.CommandTimeout);

            cmd.Verify(c => c.ExecuteNonQuery(), Times.Once());
        }

        [TestMethod]
        public void TestExecuteDoesNotExecuteCommandIfPassedCancelledToken()
        {
            var cmd = new Mock<IDbCommand>();
            cmd.SetupAllProperties();

            var conn = new Mock<IDbConnection>();
            conn.Setup(c => c.CreateCommand())
                .Returns(cmd.Object);

            var cts = new CancellationTokenSource();
            var toTest = new StoredProcedure("foo", "bar");

            var token = cts.Token;
            var task = new Task(() =>
                {
                    cts.Cancel();
                    toTest.Execute(conn.Object, token);
                }, token);

            task.RunSynchronously();

            cmd.Verify(c => c.ExecuteNonQuery(), Times.Never(), "StoredProcedure executed after the token was cancelled.");
            Assert.IsTrue(task.IsCanceled, "Cancellation not processed successfully");
        }

        [TestMethod]
        public void TestExecuteAbortsWhenTimeoutPasses()
        {
            var cmd = new Mock<IDbCommand>();
            cmd.SetupAllProperties();
            cmd.Setup(c => c.ExecuteNonQuery())
               .Callback(() => Thread.Sleep(2000))
               .Returns(2);

            var conn = new Mock<IDbConnection>();
            conn.Setup(c => c.CreateCommand())
                .Returns(cmd.Object);

            var toTest = new StoredProcedure("foo", "bar");

            try
            {
                toTest.Execute(conn.Object, CancellationToken.None, 1);
                Assert.Fail("Execute returned even though it should have timed out.");
            }
            catch (TimeoutException)
            {
            }
        }

        [TestMethod]
        public void TestExecuteAsyncDoesNotExecuteOnSameThreadWhenCalledFromATaskRunningOnSynchronizationContextScheduler()
        {
            var ctx = new TestSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(ctx);

            var cmd = new Mock<IDbCommand>();
            cmd.SetupAllProperties();
            cmd.Setup(c => c.ExecuteNonQuery())
               .Callback(() => Assert.AreNotEqual(ctx.Worker, Thread.CurrentThread, "Command called from the same thread as the reentrant Task."));

            var conn = new Mock<IDbConnection>();
            conn.Setup(c => c.CreateCommand())
                .Callback(() => Assert.AreNotEqual(ctx.Worker, Thread.CurrentThread, "Command called from the same thread as the reentrant Task."))
                .Returns(cmd.Object);

            var toTest = new StoredProcedure("foo", "bar");

            Task.Factory
                .StartNew(() => "Hello, world!")
                .ContinueWith(_ =>
                             {
                                 return toTest.ExecuteAsync(conn.Object, CancellationToken.None, 100); 
                             }, TaskScheduler.FromCurrentSynchronizationContext())
                .Unwrap()
                .Wait();

            SynchronizationContext.SetSynchronizationContext(null);
        }
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
            Assert.AreEqual(outputCount, proc.OutputParameterSetters.Count());
        }

        private class NoDefaultCtor
        {
            public string Id { get; private set; }

            public NoDefaultCtor(string id)
            {
                Id = id;
            }
        }
    }
}
