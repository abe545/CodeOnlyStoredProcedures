using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
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
    public partial class StoredProcedureExtensionsTests
    {
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
            reader.Setup(r => r.GetString(0)).Returns((int _) => results[index]);
            reader.Setup(r => r.GetFieldType(0)).Returns(typeof(string));

            var connection = new Mock<IDbConnection>();
            connection.Setup(c => c.CreateCommand()).Returns(command.Object);

            var sp = new StoredProcedure<SingleColumn>("foo");
            var res = sp.Execute(connection.Object);

            var idx = 0;
            foreach (var toTest in res)
                toTest.ShouldBeEquivalentTo(new SingleColumn { Column = results[idx++] });
        }

#if NET40
        [TestMethod]
        public void TestDoExecute_DoesNotExecuteMethodFromSameThreadWhenRunningInSynchronizationContext()
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
                  .Returns("Column");
            reader.Setup(r => r.GetString(0)).Returns((int _) => results[index]);
            reader.Setup(r => r.GetFieldType(0)).Returns(typeof(string));

            var connection = new Mock<IDbConnection>();
            connection.Setup(c => c.CreateCommand())
                      .Callback(() => ctx.Worker.Should().NotBeSameAs(Thread.CurrentThread, "Command called from the same thread as the reentrant Task."))
                      .Returns(command.Object);

            var sp = new StoredProcedure<SingleColumn>("foo");
            var res = sp.ExecuteAsync(connection.Object).Result;

            var idx = 0;
            foreach (var toTest in res)
                toTest.ShouldBeEquivalentTo(new SingleColumn { Column = results[idx++] });

            SynchronizationContext.SetSynchronizationContext(null);
        }
#else
        [TestMethod]
        public async Task TestDoExecute_DoesNotExecuteMethodFromSameThreadWhenRunningInSynchronizationContext()
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
                  .Returns("Column");
            reader.Setup(r => r.GetString(0)).Returns((int _) => results[index]);
            reader.Setup(r => r.GetFieldType(0)).Returns(typeof(string));

            var connection = new Mock<IDbConnection>();
            connection.Setup(c => c.CreateCommand())
                      .Callback(() => ctx.Worker.Should().NotBeSameAs(Thread.CurrentThread, "Command called from the same thread as the reentrant Task."))
                      .Returns(command.Object);

            var sp = new StoredProcedure<SingleColumn>("foo");
            var res = await sp.ExecuteAsync(connection.Object);

            var idx = 0;
            foreach (var toTest in res)
                toTest.ShouldBeEquivalentTo(new SingleColumn { Column = results[idx++] });

            SynchronizationContext.SetSynchronizationContext(null);
        }
#endif

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
