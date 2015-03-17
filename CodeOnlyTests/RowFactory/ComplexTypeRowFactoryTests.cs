using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeOnlyStoredProcedure;
using CodeOnlyStoredProcedure.DataTransformation;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CodeOnlyTests.RowFactory
{
    [TestClass]
    public class ComplexTypeRowFactoryTests
    {
        [TestClass]
        public class Parse
        {
            [TestMethod]
            public void CancelsWhenTokenCanceledBeforeExecuting()
            {
                var reader  = new Mock<IDataReader>();

                var cts = new CancellationTokenSource();
                cts.Cancel();

                var toTest = RowFactory<SingleResultSet>.Create();
                toTest.Invoking(f => f.ParseRows(reader.Object, Enumerable.Empty<IDataTransformer>(), cts.Token))
                      .ShouldThrow<OperationCanceledException>("the operation was cancelled");

                reader.Verify(d => d.Read(), Times.Never);
            }
            
            [TestMethod]
            public void CancelsWhenTokenCanceled()
            {
                var sema    = new SemaphoreSlim(0, 1);
                var values = new Dictionary<string, object>
                {
                    { "MyRenamedColumn", "Hello, World!" }
                };

                var reader = CreateDataReader(values);
                var execCount = 0;

                Mock.Get(reader)
                    .Setup(d => d.Read())
                    .Callback(() =>
                    {
                        sema.Release();
                        Thread.Sleep(100);
                        ++execCount;
                    })
                    .Returns(() => execCount == 1);

                var cts = new CancellationTokenSource();

                var toTest = RowFactory<RenamedColumn>.Create();
                var task = Task.Factory.StartNew(() => toTest.Invoking(f => f.ParseRows(reader, Enumerable.Empty<IDataTransformer>(), cts.Token))
                                                             .ShouldThrow<OperationCanceledException>("the operation was cancelled"),
                                                 cts.Token);

                sema.Wait(TimeSpan.FromMilliseconds(250));
                cts.Cancel();

                task.Wait(TimeSpan.FromMilliseconds(250)).Should().BeTrue();
            }

            [TestMethod]
            public void ReturnsSingleResultSetOneRow()
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

                var reader = CreateDataReader(values);

                var toTest = RowFactory<SingleResultSet>.Create();
                var res    = toTest.ParseRows(reader, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);

                res.Single()
                   .ShouldBeEquivalentTo(
                   new SingleResultSet
                   {
                       String  = "Hello, World!",
                       Double  = 42.0,
                       Decimal = 100M,
                       Int     = 99,
                       Long    = 1028130L,
                       Date    = new DateTime(1982, 1, 31),
                       FooBar  = FooBar.Bar
                   });
            }


            [TestMethod]
            public void ReturnsSingleResultSetOneRowWithStringEnumValue()
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

                var reader = CreateDataReader(values);

                var toTest = RowFactory<SingleResultSet>.Create();
                var res    = toTest.ParseRows(reader, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);

                res.Single()
                   .ShouldBeEquivalentTo(
                   new SingleResultSet
                   {
                       String  = "Hello, World!",
                       Double  = 42.0,
                       Decimal = 100M,
                       Int     = 99,
                       Long    = 1028130L,
                       Date    = new DateTime(1982, 1, 31),
                       FooBar  = FooBar.Bar
                   });
            }

            [TestMethod]
            public void TestExecuteHandlesRenamedColumns()
            {
                var values = new Dictionary<string, object>
                {
                    { "MyRenamedColumn", "Hello, World!" }
                };

                var reader = CreateDataReader(values);

                var toTest = RowFactory<RenamedColumn>.Create();
                var res    = toTest.ParseRows(reader, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);

                res.Single()
                   .ShouldBeEquivalentTo(
                   new RenamedColumn
                   {
                       Column = "Hello, World!"
                   });
            }

            [TestMethod]
            public void ConvertsDbNullToNullValues()
            {
                var values = new Dictionary<string, object>
                {
                    { "Name",           null },
                    { "NullableDouble", null },
                    { "NullableInt",    null }
                };

                var reader = CreateDataReader(values);
                Mock.Get(reader).Setup(r => r.GetFieldType(0)).Returns(typeof(string));
                Mock.Get(reader).Setup(r => r.GetFieldType(1)).Returns(typeof(double));
                Mock.Get(reader).Setup(r => r.GetFieldType(2)).Returns(typeof(int));
                Mock.Get(reader).Setup(r => r.IsDBNull(It.IsAny<int>())).Returns(true);

                var toTest = RowFactory<NullableColumns>.Create();
                var res    = toTest.ParseRows(reader, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);

                // all values are null by default
                res.Single().ShouldBeEquivalentTo(new NullableColumns());
            }

            [TestMethod]
            public void ThrowsIfMappedColumnDoesNotExistInResultSet()
            {
                var values = new Dictionary<string, object>
                {
                    { "OtherColumnName", null }
                };

                var reader = CreateDataReader(values);
                Mock.Get(reader).Setup(r => r.GetFieldType(0)).Returns(typeof(string));

                var toTest = RowFactory<SingleColumn>.Create();

                toTest.Invoking(f => f.ParseRows(reader, Enumerable.Empty<IDataTransformer>(), CancellationToken.None))
                      .ShouldThrow<StoredProcedureResultsException>("one of the mapped columns isn't returned")
                      .WithMessage("No column with name Column was found in the result set for type SingleColumn.\nThis property will be ignored if it is decorated with a NotMappedAttribute.\nYou can also map the property to a different column in the result set with the ColumnAttribute.\nIf the stored procedure can sometimes return the column, decorate the column with the OptionalAttribute.", "we should provide friendly error messages");
            }

            [TestMethod]
            public void TransformsValueWhenPropertyDecoratedWithTransformer()
            {
                var values = new Dictionary<string, object>
                {
                    { "Name", "Hello, world!" }
                };

                var reader = CreateDataReader(values);
                var toTest = RowFactory<WithStaticValue>.Create();
                var res    = toTest.ParseRows(reader, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);

                res.Single().Name.Should().Be("Foobar", "the database value should be passed through the DataTransformerAttribute");
            }

            [TestMethod]
            public void TransformsDBNullValueWhenPropertyDecoratedWithTransformer()
            {
                var values = new Dictionary<string, object>
                {
                    { "Name", "Hello, world!" }
                };

                var reader = CreateDataReader(values);
                Mock.Get(reader).Setup(r => r.GetFieldType(0)).Returns(typeof(string));

                var toTest = RowFactory<WithStaticValue>.Create();
                var res    = toTest.ParseRows(reader, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);

                res.Single().Name.Should().Be("Foobar", "the database value should be passed through the DataTransformerAttribute");
            }

            [TestMethod]
            public void TransformsRenamedColumnWhenPropertyDecoratedWithTransformer()
            {
                var values = new Dictionary<string, object>
                {
                    { "MyRenamedColumn", "Hello, world!" }
                };

                var reader = CreateDataReader(values);
                var toTest = RowFactory<RenamedColumnWithStaticValue>.Create();
                var res    = toTest.ParseRows(reader, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);

                res.Single().Name.Should().Be("Foobar", "the database value should be passed through the DataTransformerAttribute");
            }

            [TestMethod]
            public void ChainsTransformPropertyDecoratedWithTransformerAttributesInOrder()
            {
                var values = new Dictionary<string, object>
                {
                    { "Name", "Hello, world!" }
                };

                var reader = CreateDataReader(values);
                var toTest = RowFactory<WithStaticValueToUpper>.Create();
                var res    = toTest.ParseRows(reader, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);

                res.Single().Name.Should().Be("IS UPPER?", "the database value should be passed through the DataTransformerAttributes in the order they specify");
            }
            
            [TestMethod]
            public void IDataTransformerTransformsData()
            {
                var values = new Dictionary<string, object>
                {
                    { "Column", "Hello, world!" }
                };

                var reader = CreateDataReader(values);
                var toTest = RowFactory<SingleColumn>.Create();
                var res    = toTest.ParseRows(reader,
                                              new IDataTransformer[] { new StaticTransformer { Result = "Foobar" } },
                                              CancellationToken.None);

                res.Single().ShouldBeEquivalentTo(new SingleColumn { Column = "Foobar" }, "the column should be transformed");
            }

            [TestMethod]
            public void IDataTransformerTransformsDBNullValue()
            {
                var values = new Dictionary<string, object>
                {
                    { "Column", null }
                };

                var reader = CreateDataReader(values);

                Mock.Get(reader).Setup(r => r.GetFieldType(0)).Returns(typeof(string));

                var toTest = RowFactory<SingleColumn>.Create();
                var res    = toTest.ParseRows(reader,
                                              new IDataTransformer[] { new StaticTransformer { Result = "Foobar" } },
                                              CancellationToken.None);

                res.Single().ShouldBeEquivalentTo(new SingleColumn { Column = "Foobar" }, "the column should be transformed");
            }

            [TestMethod]
            public void IDataTransformerTransformsDataWithRenamedColumn()
            {
                var values = new Dictionary<string, object>
                {
                    { "MyRenamedColumn", "Hello, world!" }
                };

                var reader = CreateDataReader(values);
                var toTest = RowFactory<RenamedColumn>.Create();
                var res    = toTest.ParseRows(reader,
                                              new IDataTransformer[] { new StaticTransformer { Result = "Foobar" } },
                                              CancellationToken.None);

                res.Single().ShouldBeEquivalentTo(new RenamedColumn { Column = "Foobar" }, "the column should be transformed");
            }

            [TestMethod]
            public void IDataTransformerNotCalledWhenCanNotTransformValue()
            {
                var values = new Dictionary<string, object>
                {
                    { "Column", "Hello, world!" }
                };

                var reader = CreateDataReader(values);
                var toTest = RowFactory<SingleColumn>.Create();
                var res    = toTest.ParseRows(reader,
                                              new IDataTransformer[] { new NeverTransformer() },
                                              CancellationToken.None);

                res.Single().ShouldBeEquivalentTo(new SingleColumn { Column = "Hello, world!" }, "the column should not be transformed");
            }

            [TestMethod]
            public void IDataTransformerNotCalledWhenCanNotTransformValueWithDBNullValue()
            {
                var values = new Dictionary<string, object>
                {
                    { "Column", null }
                };

                var reader = CreateDataReader(values);
                Mock.Get(reader).Setup(r => r.GetFieldType(0)).Returns(typeof(string));
                var toTest = RowFactory<SingleColumn>.Create();
                var res    = toTest.ParseRows(reader,
                                              new IDataTransformer[] { new NeverTransformer() },
                                              CancellationToken.None);

                res.Single().ShouldBeEquivalentTo(new SingleColumn(), "the column should not be transformed");
            }

            [TestMethod]
            public void IDataTransformerNotCalledWhenCanNotTransformValueWithRenamedColumn()
            {
                var values = new Dictionary<string, object>
                {
                    { "MyRenamedColumn", "Hello, world!" }
                };

                var reader = CreateDataReader(values);
                var toTest = RowFactory<RenamedColumn>.Create();
                var res    = toTest.ParseRows(reader,
                                              new IDataTransformer[] { new NeverTransformer() },
                                              CancellationToken.None);

                res.Single().ShouldBeEquivalentTo(new RenamedColumn { Column = "Hello, world!" }, "the column should not be transformed");
            }

            [TestMethod]
            public void DoesNotPassNullableToTransformationAttributes()
            {
                var values = new Dictionary<string, object>
                {
                    { "Value", 1.0 },
                    { "FooBar", 4L }
                };

                var reader = CreateDataReader(values);

                var toTest = RowFactory<NullableChecker>.Create();
                var res    = toTest.ParseRows(reader, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);

                res.Single().ShouldBeEquivalentTo(new NullableChecker { Value = 1.0, FooBar = FooBar.Foo });
            }

            [TestMethod]
            public void DoesNotPassNullableToTransformationAttributesWhenPassedNull()
            {
                var values = new Dictionary<string, object>
                {
                    { "Value", null },
                    { "FooBar", null }
                };

                var reader = CreateDataReader(values);
                Mock.Get(reader).Setup(r => r.GetFieldType(0)).Returns(typeof(int));
                Mock.Get(reader).Setup(r => r.GetFieldType(1)).Returns(typeof(double));
                var xformer = new Mock<IDataTransformer>();

                var toTest = RowFactory<NullableChecker>.Create();
                var res    = toTest.ParseRows(reader, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);

                res.Single().ShouldBeEquivalentTo(new NullableChecker());
            }

            [TestMethod]
            public void DoesNotPassNullableToTransformers()
            {
                var values = new Dictionary<string, object>
                {
                    { "Value", 42.0 },
                    { "FooBar", 6 }
                };

                var reader  = CreateDataReader(values);
                Mock.Get(reader).Setup(r => r.GetFieldType(0)).Returns(typeof(int));
                Mock.Get(reader).Setup(r => r.GetFieldType(1)).Returns(typeof(double));
                var xformer = new Mock<IDataTransformer>();

                xformer.Setup(x => x.CanTransform(It.IsAny<object>(), typeof(double), true, It.IsAny<IEnumerable<Attribute>>()))
                       .Returns(true)
                       .Verifiable();
                xformer.Setup(x => x.Transform(It.IsAny<object>(), typeof(double), true, It.IsAny<IEnumerable<Attribute>>()))
                       .Returns<object, Type, bool, IEnumerable<Attribute>>((o, t, b, e) =>
                       {
                           Assert.IsTrue(b, "isNullable must be true for a Nullable<T> property");
                           if (t.IsGenericType)
                               Assert.AreNotEqual(typeof(Nullable<>), t.GetGenericTypeDefinition(), "A Nullable<T> type can not be passed to a DataTransformerAttributeBase");

                           return o;
                       })
                       .Verifiable();

                var toTest = RowFactory<NullableChecker>.Create();
                var res    = toTest.ParseRows(reader, new[] { xformer.Object }, CancellationToken.None);

                res.Single().ShouldBeEquivalentTo(new NullableChecker { Value = 42, FooBar = FooBar.Bar });
            }

            [TestMethod]
            public void DoesNotPassNullableToTransformersWhenPassedNull()
            {
                var values = new Dictionary<string, object>
                {
                    { "Value", null },
                    { "FooBar", null }
                };

                var reader  = CreateDataReader(values);
                Mock.Get(reader).Setup(r => r.GetFieldType(0)).Returns(typeof(int));
                Mock.Get(reader).Setup(r => r.GetFieldType(1)).Returns(typeof(double));
                var xformer = new Mock<IDataTransformer>();

                xformer.Setup(x => x.CanTransform(It.IsAny<object>(), typeof(double), true, It.IsAny<IEnumerable<Attribute>>()))
                       .Returns(true)
                       .Verifiable();
                xformer.Setup(x => x.Transform(It.IsAny<object>(), typeof(double), true, It.IsAny<IEnumerable<Attribute>>()))
                       .Returns<object, Type, bool, IEnumerable<Attribute>>((o, t, b, e) =>
                       {
                           Assert.IsTrue(b, "isNullable must be true for a Nullable<T> property");
                           if (t.IsGenericType)
                               Assert.AreNotEqual(typeof(Nullable<>), t.GetGenericTypeDefinition(), "A Nullable<T> type can not be passed to a DataTransformerAttributeBase");

                           return o;
                       })
                       .Verifiable();

                var toTest = RowFactory<NullableChecker>.Create();
                var res    = toTest.ParseRows(reader, new[] { xformer.Object }, CancellationToken.None);

                res.Single().ShouldBeEquivalentTo(new NullableChecker());
            }

            [TestMethod]
            public void ReturnsNewRowWithoutOptionalProperty()
            {
                var data = new Dictionary<string, object>
                {
                    { "Name", "Foo" },
                    { "Price", 13.3 },
                    { "Rename", "Bar" }
                }; 

                var reader = CreateDataReader(data);
                var toTest = RowFactory<Row>.Create().ParseRows(reader, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);

                toTest.Single().Should().NotBeNull("one row should have been returned")
                      .And.BeOfType<Row>()
                      .And.Subject
                      .Should().Match<Row>(r => r.Name == "Foo", "Name column returned Foo")
                      .And.Match<Row>(r => r.Price == 13.3, "Price column returned 13.3")
                      .And.Match<Row>(r => r.Other == "Bar", "Other column returned Bar");
            }

            [TestMethod]
            public void SetsOptionalProperties()
            {
                var data = new Dictionary<string, object>
                {
                    { "Name", "Foo" },
                    { "Price", 13.3 },
                    { "Rename", "Bar" },
                    { "Optional", true }
                }; 

                var reader = CreateDataReader(data);
                var toTest = RowFactory<Row>.Create().ParseRows(reader, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);

                toTest.Single().Should().NotBeNull("one row should have been returned")
                      .And.BeOfType<Row>()
                      .And.Subject
                      .Should().Match<Row>(r => r.Name == "Foo", "Name column returned Foo")
                      .And.Match<Row>(r => r.Price == 13.3, "Price column returned 13.3")
                      .And.Match<Row>(r => r.Other == "Bar", "Other column returned Bar")
                      .And.Match<Row>(r => r.Optional, "Optional column returned true");
            }

            [TestMethod]
            public void WithoutPropertyInResultsSpecifiesThemInException()
            {
                var data = new Dictionary<string, object>
                {
                    { "Name", "Foo" }
                };

                var reader = CreateDataReader(data);
                var toTest = RowFactory<Row>.Create();

                toTest.Invoking(t => t.ParseRows(reader, Enumerable.Empty<IDataTransformer>(), CancellationToken.None))
                      .ShouldThrow<StoredProcedureResultsException>("not all columns are returned")
                      .WithMessage("No columns with name Price or Rename were found in the result set for type Row.\nThis property will be ignored if it is decorated with a NotMappedAttribute.\nYou can also map the property to a different column in the result set with the ColumnAttribute.\nIf the stored procedure can sometimes return the column, decorate the column with the OptionalAttribute.", "we should provide friendly error messages");
            }

            [TestMethod]
            public void ExceptionsIncludeUsefulInformation()
            {
                var data = new Dictionary<string, object>
                {
                    { "Name", "Foo" },
                    { "Price", "Blah" },
                    { "Rename", "Bar" }
                };

                var reader = CreateDataReader(data);
                var toTest = RowFactory<Row>.Create();

                toTest.Invoking(t => t.ParseRows(reader, Enumerable.Empty<IDataTransformer>(), CancellationToken.None))
                      .ShouldThrow<StoredProcedureColumnException>("because the property result type does not match")
                      .WithMessage("Error setting [Double] Price. Stored Procedure returns [String].");
            }

            [TestMethod]
            public void ExceptionIncludesTypeForValueReturned()
            {
                var data = new Dictionary<string, object>
                {
                    { "Name", "Foo" },
                    { "Price", 42M },
                    { "Rename", "Bar" }
                };

                var reader = CreateDataReader(data);
                var toTest = RowFactory<Row>.Create();

                toTest.Invoking(t => t.ParseRows(reader, Enumerable.Empty<IDataTransformer>(), CancellationToken.None))
                      .ShouldThrow<StoredProcedureColumnException>("because the property result type does not match")
                      .WithMessage("Error setting [Double] Price. Stored Procedure returns [Decimal].");
            }

            [TestMethod]
            public void WithPropertyTransformerWillCallTransformer()
            {
                var data = new Dictionary<string, object>
                {
                    { "Name", "Foo" },
                    { "Price", 13.3 },
                    { "Rename", "Bar" },
                    { "Transformed", 42.0 }
                };

                var reader = CreateDataReader(data);
                var toTest = RowFactory<Row>.Create().ParseRows(reader, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);

                toTest.Single().Should().NotBeNull("one row should have been returned")
                      .And.BeOfType<Row>()
                      .And.Subject
                      .Should().Match<Row>(r => r.Name == "Foo", "Name column returned Foo")
                      .And.Match<Row>(r => r.Price == 13.3, "Price column returned 13.3")
                      .And.Match<Row>(r => r.Other == "Bar", "Other column returned Bar")
                      .And.Match<Row>(r => r.Transformed == 42M, "Transformed column returned 42.0 double, which should have been converted to 42 decimal");
            }

            [TestMethod]
            public void MappedInterface_ReturnsImplementation()
            {
                lock (CodeOnlyStoredProcedure.TypeExtensions.interfaceMap)
                {
                    StoredProcedure.MapResultType<Interface, InterfaceImpl>();
                    var data = new Dictionary<string, object>
                    {
                        { "Id", "42" }
                    };

                    var reader = CreateDataReader(data);
                    var toTest = RowFactory<Interface>.Create().ParseRows(reader, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);

                    toTest.Single().Should().NotBeNull("one row should have been returned")
                          .And.BeOfType<InterfaceImpl>()
                          .And.Match<Interface>(i => i.Id == "42");
                }
            }

            [TestMethod]
            public void StrongTypedDataTransformers_RetrievesValuesUnboxed()
            {
                var reader = CreateDataReader(new Dictionary<string, object>
                    {
                        { "Value", 42 }
                    }, false);

                var toTest = RowFactory<WithStrongTypedDataTransformer>.Create().ParseRows(
                    reader, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);
                
                toTest.Single().Should().NotBeNull("one row should have been returned")
                      .And.Match<WithStrongTypedDataTransformer>(i => i.Value == 84, "the transformer doubles the result from the database");
            }

            [TestMethod]
            public void EnumPropertiesWithWrongUnderlyingTypeWillThrowIfNotMarkedWithConvert()
            {
                var reader = CreateDataReader(new Dictionary<string, object>
                    {
                        { "FooBar", 4L }
                    }, false);

                var toTest = RowFactory<EnumValue>.Create();
                
                toTest.Invoking(t => t.ParseRows(reader, Enumerable.Empty<IDataTransformer>(), CancellationToken.None))
                      .ShouldThrow<StoredProcedureColumnException>("the stored procedure returns a different type than the underlying type of the enum.");
            }

            [TestMethod]
            public void EnumPropertiesWillConvertNumericTypes()
            {
                var reader = CreateDataReader(new Dictionary<string, object>
                    {
                        { "FooBar", 4L }
                    }, false);

                var toTest = RowFactory<EnumValueTypesConverted>.Create().ParseRows(
                    reader, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);

                toTest.Single().Should().NotBeNull("one row should have been returned")
                      .And.Match<EnumValueTypesConverted>(i => i.FooBar == FooBar.Foo, "the transformer doubles the result from the database");
            }

            [TestMethod]
            public void NumericTypeWillConvertToTrueBoolIfMarkedWithConvert()
            {
                var reader = CreateDataReader(new Dictionary<string, object>
                    {
                        { "IsEnabled", 1 }
                    }, false);

                var toTest = RowFactory<ConvertToBool>.Create().ParseRows(
                    reader, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);

                toTest.Should().ContainSingle("because one row is returned").Which
                    .Should().Match<ConvertToBool>(i => i.IsEnabled, "the property should be converted to bool");
            }

            [TestMethod]
            public void NumericTypeWillConvertToFalseBoolIfMarkedWithConvert()
            {
                var reader = CreateDataReader(new Dictionary<string, object>
                    {
                        { "IsEnabled", 0 }
                    }, false);

                var toTest = RowFactory<ConvertToBool>.Create().ParseRows(
                    reader, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);

                toTest.Should().ContainSingle("because one row is returned").Which
                    .Should().Match<ConvertToBool>(i => !i.IsEnabled, "the property should be converted to bool");
            }

            [TestMethod]
            public void NumericTypeWillConvertToTrueBoolIfMarkedWithConvert_WhenTransformerPassed()
            {
                var reader = CreateDataReader(new Dictionary<string, object>
                    {
                        { "IsEnabled", 1 }
                    }, true);

                var toTest = RowFactory<ConvertToBool>.Create().ParseRows(
                    reader, new[] { Mock.Of<IDataTransformer>() }, CancellationToken.None);

                toTest.Should().ContainSingle("because one row is returned").Which
                    .Should().Match<ConvertToBool>(i => i.IsEnabled, "the property should be converted to bool");
            }

            [TestMethod]
            public void NumericTypeWillConvertToFalseBoolIfMarkedWithConvert_WhenTransformerPassed()
            {
                var reader = CreateDataReader(new Dictionary<string, object>
                    {
                        { "IsEnabled", 0L }
                    }, true);

                var toTest = RowFactory<ConvertToBool>.Create().ParseRows(
                    reader, new[] { Mock.Of<IDataTransformer>() }, CancellationToken.None);

                toTest.Should().ContainSingle("because one row is returned").Which
                    .Should().Match<ConvertToBool>(i => !i.IsEnabled, "the property should be converted to bool");
            }

            [TestMethod]
            public void NumericTypeWillConvertToTrueNullableBoolIfMarkedWithConvert()
            {
                var reader = CreateDataReader(new Dictionary<string, object>
                    {
                        { "IsEnabled", 1M }
                    }, false);

                var toTest = RowFactory<ConvertToNullableBool>.Create().ParseRows(
                    reader, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);

                toTest.Should().ContainSingle("because one row is returned").Which
                    .Should().Match<ConvertToNullableBool>(i => i.IsEnabled.Value, "the property should be converted to bool");
            }

            [TestMethod]
            public void NullNumericTypeWillConvertToNullNullableBoolIfMarkedWithConvert()
            {
                var reader = CreateDataReader(new Dictionary<string, object>
                    {
                        { "IsEnabled", null }
                    }, true);
                Mock.Get(reader).Setup(rdr => rdr.GetFieldType(0)).Returns(typeof(int));

                var toTest = RowFactory<ConvertToNullableBool>.Create().ParseRows(
                    reader, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);

                toTest.Should().ContainSingle("because one row is returned").Which
                    .Should().Match<ConvertToNullableBool>(i => !i.IsEnabled.HasValue, "the property should not have a value");
            }

            [TestMethod]
            public void NumericTypeWillConvertToFalseNullableBoolIfMarkedWithConvert()
            {
                var reader = CreateDataReader(new Dictionary<string, object>
                    {
                        { "IsEnabled", 0 }
                    }, false);

                var toTest = RowFactory<ConvertToNullableBool>.Create().ParseRows(
                    reader, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);

                toTest.Should().ContainSingle("because one row is returned").Which
                    .Should().Match<ConvertToNullableBool>(i => !i.IsEnabled.Value, "the property should be converted to bool");
            }

            [TestMethod]
            public void NumericTypeWillConvertToTrueNullableBoolIfMarkedWithConvert_WhenTransformerPassed()
            {
                var reader = CreateDataReader(new Dictionary<string, object>
                    {
                        { "IsEnabled", 1.0 }
                    }, true);

                var toTest = RowFactory<ConvertToNullableBool>.Create().ParseRows(
                    reader, new[] { Mock.Of<IDataTransformer>() }, CancellationToken.None);

                toTest.Should().ContainSingle("because one row is returned").Which
                    .Should().Match<ConvertToNullableBool>(i => i.IsEnabled.Value, "the property should be converted to bool");
            }

            [TestMethod]
            public void NumericTypeWillConvertToFalseNullableBoolIfMarkedWithConvert_WhenTransformerPassed()
            {
                var reader = CreateDataReader(new Dictionary<string, object>
                    {
                        { "IsEnabled", 0 }
                    }, true);

                var toTest = RowFactory<ConvertToNullableBool>.Create().ParseRows(
                    reader, new[] { Mock.Of<IDataTransformer>() }, CancellationToken.None);

                toTest.Should().ContainSingle("because one row is returned").Which
                    .Should().Match<ConvertToNullableBool>(i => !i.IsEnabled.Value, "the property should be converted to bool");
            }

            [TestMethod]
            public void NullNumericTypeWillConvertToNullNullableBoolIfMarkedWithConvert_WhenTransformerPassed()
            {
                var reader = CreateDataReader(new Dictionary<string, object>
                    {
                        { "IsEnabled", null }
                    }, true);
                Mock.Get(reader).Setup(rdr => rdr.GetFieldType(0)).Returns(typeof(short));

                var toTest = RowFactory<ConvertToNullableBool>.Create().ParseRows(
                    reader, new[] { Mock.Of<IDataTransformer>() }, CancellationToken.None);

                toTest.Should().ContainSingle("because one row is returned").Which
                    .Should().Match<ConvertToNullableBool>(i => !i.IsEnabled.HasValue, "the property should not have a value");
            }

            [TestMethod]
            public void TrueBoolWillConvertToNumericTypeIfMarkedWithConvert()
            {
                var reader = CreateDataReader(new Dictionary<string, object>
                    {
                        { "IsEnabled", true }
                    }, false);

                var toTest = RowFactory<ConvertFromBool>.Create().ParseRows(
                    reader, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);

                toTest.Should().ContainSingle("because one row is returned").Which
                    .Should().Match<ConvertFromBool>(i => i.IsEnabled == 1, "the property should be converted from bool");
            }

            [TestMethod]
            public void FalseBoolWillConvertToNumericTypeIfMarkedWithConvert()
            {
                var reader = CreateDataReader(new Dictionary<string, object>
                    {
                        { "IsEnabled", false }
                    }, false);

                var toTest = RowFactory<ConvertFromBool>.Create().ParseRows(
                    reader, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);

                toTest.Should().ContainSingle("because one row is returned").Which
                    .Should().Match<ConvertFromBool>(i => i.IsEnabled == 0, "the property should be converted from bool");
            }

            private static IDataReader CreateDataReader(Dictionary<string, object> values, bool setupGetValue = true)
            {
                var keys = values.Keys.OrderBy(s => s).ToList();
                var vals = values.OrderBy(kv => kv.Key).Select(kv => kv.Value).ToArray();

                var reader  = new Mock<IDataReader>();

                reader.SetupGet(r => r.FieldCount)
                      .Returns(keys.Count);
                reader.Setup(r => r.GetFieldType(It.IsAny<int>()))
                      .Returns((int i) => vals[i].GetType());
                reader.Setup(r => r.GetOrdinal(It.IsAny<string>()))
                      .Returns((string s) => keys.IndexOf(s));
                reader.Setup(r => r.IsDBNull(It.IsAny<int>()))
                      .Returns((int i) => vals[i] == null);

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

                if (setupGetValue)
                {
                    reader.Setup(r => r.GetValue(It.IsAny<int>()))
                          .Returns((int i) => vals[i]);
                }

                reader.Setup(r => r.GetName(It.IsAny<int>()))
                      .Returns((int i) => keys[i]);
                reader.Setup(r => r.GetString(It.IsAny<int>()))
                      .Returns((int i) => (string)vals[i]);
                reader.Setup(r => r.GetInt16(It.IsAny<int>()))
                      .Returns((int i) => (short)vals[i]);
                reader.Setup(r => r.GetInt32(It.IsAny<int>()))
                      .Returns((int i) => (int)vals[i]);
                reader.Setup(r => r.GetInt64(It.IsAny<int>()))
                      .Returns((int i) => (long)vals[i]);
                reader.Setup(r => r.GetFloat(It.IsAny<int>()))
                      .Returns((int i) => (float)vals[i]);
                reader.Setup(r => r.GetDecimal(It.IsAny<int>()))
                      .Returns((int i) => (decimal)vals[i]);
                reader.Setup(r => r.GetDouble(It.IsAny<int>()))
                      .Returns((int i) => (double)vals[i]);
                reader.Setup(r => r.GetDateTime(It.IsAny<int>()))
                      .Returns((int i) => (DateTime)vals[i]);
                reader.Setup(r => r.GetChar(It.IsAny<int>()))
                      .Returns((int i) => (char)vals[i]);
                reader.Setup(r => r.GetByte(It.IsAny<int>()))
                      .Returns((int i) => (byte)vals[i]);
                reader.Setup(r => r.GetBoolean(It.IsAny<int>()))
                      .Returns((int i) => (bool)vals[i]);

                return reader.Object;
            }
        }

        #region Test Helper Classes
        private class SingleResultSet
        {
            public String String { get; set; }
            public Double Double { get; set; }
            public Decimal Decimal { get; set; }
            public Int32 Int { get; set; }
            public Int64 Long { get; set; }
            public DateTime Date { get; set; }
            public FooBar FooBar { get; set; }
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
            public string Name { get; set; }
            public int? NullableInt { get; set; }
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
            [NullableTransformation, ConvertNumeric]
            public FooBar? FooBar { get; set; }
        }

        private interface Interface
        {
            string Id { get; set; }
        }

        private class InterfaceImpl : Interface
        {
            public string Id { get; set; }
        }

        private class Row
        {
            public string Name { get; set; }
            public double Price { get; set; }
            [Column("Rename")]
            public string Other { get; set; }
            [OptionalResult]
            public bool Optional { get; set; }
            [OptionalResult, ConvertNumeric]
            public decimal Transformed { get; set; }
        }

        private class WithStrongTypedDataTransformer
        {
            [StrongTypedDataTransformer]
            public uint Value { get; set; }
        }

        private class EnumValue
        {
            public FooBar FooBar { get; set; }
        }

        private class EnumValueTypesConverted
        {
            [ConvertNumeric]
            public FooBar FooBar { get; set; }
        }

        private class ConvertToBool
        {
            [ConvertNumeric]
            public bool IsEnabled { get; set; }
        }

        private class ConvertToNullableBool
        {
            [ConvertNumeric]
            public bool? IsEnabled { get; set; }
        }

        private class ConvertFromBool
        {
            [ConvertNumeric]
            public int IsEnabled { get; set; }
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
                isNullable.Should().BeTrue("isNullable must be true for a Nullable<T> property");
                if (targetType.IsGenericType)
                    targetType.GetGenericTypeDefinition().Should().NotBe(typeof(Nullable<>), "a Nullable<T> type should not be passed to a DataTransformerAttributeBase");

                return value;
            }
        }

        private class StrongTypedDataTransformer : DataTransformerAttributeBase, IDataTransformerAttribute<uint>
        {
            public uint Transform(uint value)
            {
                return value * 2;
            }

            public override object Transform(object value, Type targetType, bool isNullable)
            {
                return Transform((uint)value);
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
