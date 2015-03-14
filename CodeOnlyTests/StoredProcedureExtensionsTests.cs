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

        //[TestMethod]
        //public void TestExecuteReturnsMultipleRowsInOneResultSet()
        //{
        //    var reader = new Mock<IDataReader>();
        //    var command = new Mock<IDbCommand>();

        //    command.Setup(d => d.ExecuteReader())
        //           .Returns(reader.Object);

        //    reader.SetupGet(r => r.FieldCount)
        //          .Returns(1);

        //    var results = new[] { "Hello", ", ", "World!" };

        //    int index = -1;
        //    reader.Setup(r => r.Read())
        //          .Callback(() => ++index)
        //          .Returns(() => index < results.Length);

        //    reader.Setup(r => r.GetName(0))
        //          .Returns("Column");
        //    reader.Setup(r => r.GetValues(It.IsAny<object[]>()))
        //          .Callback((object[] arr) => arr[0] = results[index])
        //          .Returns(1);

        //    var res = command.Object.Execute(CancellationToken.None, new[] { typeof(SingleColumn) }, Enumerable.Empty<IDataTransformer>());

        //    var toTest = (IList<SingleColumn>)res[0];

        //    Assert.AreEqual(3, toTest.Count);

        //    for (int i = 0; i < results.Length; i++)
        //    {
        //        var item = toTest[i];

        //        Assert.AreEqual(results[i], item.Column);
        //    }
        //}

        //[TestMethod]
        //public void TestDoExecute_DoesNotExecuteMethodFromSameThreadWhenRunningInSynchronizationContext()
        //{
        //    var cmd = Mock.Of<IDbCommand>();
        //    var ctx = new TestSynchronizationContext();

        //    SynchronizationContext.SetSynchronizationContext(ctx);

        //    Task.Factory
        //        .StartNew(() => "Hello, world!")
        //        .ContinueWith(s => cmd.DoExecute(_ =>
        //        {
        //            Assert.AreNotEqual(ctx.Worker, Thread.CurrentThread, "Command called from the same thread as the reentrant Task.");
        //            return 0;
        //        }, CancellationToken.None), TaskScheduler.FromCurrentSynchronizationContext())
        //        .Wait();

        //    SynchronizationContext.SetSynchronizationContext(null);
        //}

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

        private class NullableChecker
        {
            [NullableTransformation]
            public double? Value { get; set; }
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
            public Type OutputType { get { return typeof(string); } }

            public string Result { get; set; }
            
            public bool CanTransformInput(Type type)
            {
                return true;
            }

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
            public Type OutputType { get { return typeof(object); } }
            
            public bool CanTransformInput(Type type)
            {
                return false;
            }

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

        private class NullableTransformationAttribute : DataTransformerAttributeBase
        {
            public override object Transform(object value, Type targetType, bool isNullable)
            {
                Assert.IsTrue(isNullable, "isNullable must be true for a Nullable<T> property.");
                if (targetType.IsGenericType)
                    Assert.AreNotEqual(typeof(Nullable<>), targetType.GetGenericTypeDefinition(), "A Nullable<T> type can not be passed to a DataTransformerAttributeBase.");

                return value;
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
