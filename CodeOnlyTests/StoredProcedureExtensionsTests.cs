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
