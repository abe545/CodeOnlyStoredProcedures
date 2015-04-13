using System;
using System.Diagnostics.Contracts;
using CodeOnlyStoredProcedure;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

#if NET40
namespace CodeOnlyTests.Net40
#else
namespace CodeOnlyTests
#endif
{
    [TestClass]
    public class HierarchicalStoredProcedureTests
    {
        #region Constructor Tests
        [TestMethod]
        public void TestConstructorSetsNameAndDefaultSchema()
        {
            var toTest = new HierarchicalStoredProcedure<string>("TestProc", new [] { typeof(string) });
            toTest.Name.Should().Be("TestProc", "because it was passed in the constructor");
            toTest.Schema.Should().Be("dbo", "because it is the default when not passed in the constructor");
        }

        [TestMethod]
        public void TestConstructorSetsNameAndSchema()
        {
            var toTest = new HierarchicalStoredProcedure<string>("tEst", "Proc", new[] { typeof(string) });
            toTest.Name.Should().Be("Proc", "because it was passed in the constructor");
            toTest.Schema.Should().Be("tEst", "because it was passed in the constructor");
        }
        #endregion
        
        #region CloneCore Tests
        [TestMethod]
        public void TestCloneCore_CreatesNewStoredProcedureWithParameters()
        {
            var p1 = Mock.Of<IStoredProcedureParameter>(p => p.ParameterName == "Foo");
            var p2 = Mock.Of<IStoredProcedureParameter>(p => p.ParameterName == "Bar");
            var t1 = Mock.Of<IDataTransformer>();
            var t2 = Mock.Of<IDataTransformer>();

            var parms = new[] { p1, p2 };
            var transformers = new[] { t1, t2 };

            var sp = new StoredProcedure("schema", "Test");
            var toTest = sp.CloneCore(parms, transformers);

            toTest.Name.Should().Be("Test", "because it should have been cloned");
            toTest.Schema.Should().Be("schema", "because it should have been cloned");
            toTest.Parameters.Should().ContainInOrder(new[] { p1, p2 }, "because they should be copied when cloned");
            toTest.DataTransformers.Should().ContainInOrder(new[] { t1, t2 }, "because they should be copied when cloned");
        }
        #endregion

        #region CloneWith Tests
        [TestMethod]
        public void TestCloneWithDoesNotAlterOriginalProcedure()
        {
            var sp = new StoredProcedure("test", "proc");

            var toTest = sp.CloneWith(Mock.Of<IStoredProcedureParameter>());

            AssertProcValues(sp, typeof(StoredProcedure), "test", "proc");
        }

        [TestMethod]
        public void TestCloneWithTransformerDoesNotAlterOriginalProcedure()
        {
            var sp = new StoredProcedure("test", "proc");

            var toTest = sp.CloneWith(Mock.Of<IDataTransformer>());

            AssertProcValues(sp, typeof(StoredProcedure), "test", "proc");
        }

        [TestMethod]
        public void TestCloneWithRetainsNameAndDefaultSchema()
        {
            var sp = new StoredProcedure("test_proc");

            var toTest = sp.CloneWith(Mock.Of<IStoredProcedureParameter>());

            AssertProcValues(toTest, typeof(StoredProcedure), "dbo", "test_proc");
        }

        [TestMethod]
        public void TestCloneWithRetainsNameAndSchema()
        {
            var sp = new StoredProcedure("test", "proc");

            var toTest = sp.CloneWith(Mock.Of<IStoredProcedureParameter>());

            AssertProcValues(toTest, typeof(StoredProcedure), "test", "proc");
        }

        [TestMethod]
        public void TestCloneWithTransformerRetainsNameAndDefaultSchema()
        {
            var sp = new StoredProcedure("test_proc");

            var toTest = sp.CloneWith(new Mock<IDataTransformer>().Object);

            AssertProcValues(toTest, typeof(StoredProcedure), "dbo", "test_proc");
        }

        [TestMethod]
        public void TestCloneWithTransformerRetainsNameAndSchema()
        {
            var sp = new StoredProcedure("test", "proc");

            var toTest = sp.CloneWith(new Mock<IDataTransformer>().Object);

            AssertProcValues(toTest, typeof(StoredProcedure), "test", "proc");
        }

        [TestMethod]
        public void TestCloneWithStoresParameter()
        {
            var p1 = Mock.Of<IStoredProcedureParameter>(p => p.ParameterName == "Parm");

            var toTest = new StoredProcedure("test").CloneWith(p1);

            toTest.Parameters.Should().ContainSingle(p => p == p1, "because it should be copied to the clone.");
        }

        [TestMethod]
        public void TestCloneWithTransformerStoresTransformer()
        {
            var x = new Mock<IDataTransformer>().Object;

            var toTest = new StoredProcedure("Test").CloneWith(x);

            toTest.DataTransformers.Should().ContainSingle(d => d == x, "because it should be copied to the clone.");
        }
        #endregion

        #region AsHierarchical Tests
        [TestMethod]
        public void AsHierarchical_SetsName()
        {
            var toTest = StoredProcedure.Create("foo")
                                        .WithResults<string, int>()
                                        .AsHierarchical<string>();

            AssertProcValues(toTest, typeof(HierarchicalStoredProcedure<string>), "dbo", "foo");
        }

        [TestMethod]
        public void AsHierarchical_SetsNameAndSchema()
        {
            var toTest = StoredProcedure.Create("foo", "bar")
                                        .WithResults<string, int>()
                                        .AsHierarchical<string>();

            AssertProcValues(toTest, typeof(HierarchicalStoredProcedure<string>), "foo", "bar");
        }

        [TestMethod]
        public void AsHierarchical_SetsParameters()
        {
            var toTest = StoredProcedure.Create("foo")
                                        .WithParameter("name", "baz")
                                        .WithResults<string, int>()
                                        .AsHierarchical<string>();

            var parm = toTest.Parameters.Should().ContainSingle("because one Parameter was set").Which;
            parm.ParameterName.Should().Be("name", "because that is the parameter that was set");
            ((IInputStoredProcedureParameter)parm).Value.Should().Be("baz", "because that is the parameter that was set");
        }

        [TestMethod]
        public void AsHierarchical_SetsDataTransformers()
        {
            var xForm = Mock.Of<IDataTransformer>();
            var toTest = StoredProcedure.Create("foo")
                                        .WithDataTransformer(xForm)
                                        .WithResults<string, int>()
                                        .AsHierarchical<string>();

            toTest.DataTransformers.Should().ContainSingle("because one DataTransformer was set")
                  .Which.Should().Be(xForm, "because it was the one set");
        }

        [TestMethod]
        public void AsHierarchical_ThrowsWhenTypeNotSpecifiedByWithResults()
        {
            var toTest = StoredProcedure.Create("foo")
                                        .WithResults<string, int>();

            toTest.Invoking(s => s.AsHierarchical<byte>())
                  .ShouldThrow<ArgumentException>("because the type specified was not passed in WithResults")
                  .And.Message.Should().Be("The type of TFactory must be one of the types the Stored Procedure has already been declared to return.");
        }
        #endregion

        private static void AssertProcValues(
            StoredProcedure proc,
            Type procType,
            string schema,
            string name)
        {
            proc.Name.Should().Be(name);
            proc.Schema.Should().Be(schema);
            proc.FullName.Should().Be(String.Format("[{0}].[{1}]", schema, name));
            proc.Should().BeOfType(procType);
        }
    }
}
