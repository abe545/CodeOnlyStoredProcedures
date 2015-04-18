using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeOnlyStoredProcedure;
using CodeOnlyStoredProcedure.DataTransformation;
using CodeOnlyStoredProcedure.RowFactory;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

#if NET40
namespace CodeOnlyTests.Net40.RowFactory
#else
namespace CodeOnlyTests.RowFactory
#endif
{
    [TestClass]
    public class ComplexTypeRowFactoryTests
    {
        [TestClass]
        public class ParseRows
        {
            [TestMethod]
            public void CancelsWhenTokenCanceledBeforeExecuting()
            {
                var reader  = new Mock<IDataReader>();

                var cts = new CancellationTokenSource();
                cts.Cancel();

                var toTest = new ComplexTypeRowFactory<SingleResultSet>();
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

                var toTest = new ComplexTypeRowFactory<RenamedColumn>();
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

                var toTest = new ComplexTypeRowFactory<SingleResultSet>();
                var res    = toTest.ParseRows(reader, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);

                res.Should().ContainSingle().Which
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

                var toTest = new ComplexTypeRowFactory<SingleResultSet>();
                var res    = toTest.ParseRows(reader, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);

                res.Should().ContainSingle().Which
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

                var toTest = new ComplexTypeRowFactory<RenamedColumn>();
                var res    = toTest.ParseRows(reader, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);

                res.Should().ContainSingle().Which
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

                var toTest = new ComplexTypeRowFactory<NullableColumns>();
                var res    = toTest.ParseRows(reader, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);

                // all values are null by default
                res.Should().ContainSingle().Which.ShouldBeEquivalentTo(new NullableColumns());
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

                var toTest = new ComplexTypeRowFactory<SingleColumn>();

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
                var toTest = new ComplexTypeRowFactory<WithStaticValue>();
                var res    = toTest.ParseRows(reader, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);

                res.Should().ContainSingle().Which.Name.Should().Be("Foobar", "the database value should be passed through the DataTransformerAttribute");
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

                var toTest = new ComplexTypeRowFactory<WithStaticValue>();
                var res    = toTest.ParseRows(reader, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);

                res.Should().ContainSingle().Which.Name.Should().Be("Foobar", "the database value should be passed through the DataTransformerAttribute");
            }

            [TestMethod]
            public void TransformsRenamedColumnWhenPropertyDecoratedWithTransformer()
            {
                var values = new Dictionary<string, object>
                {
                    { "MyRenamedColumn", "Hello, world!" }
                };

                var reader = CreateDataReader(values);
                var toTest = new ComplexTypeRowFactory<RenamedColumnWithStaticValue>();
                var res    = toTest.ParseRows(reader, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);

                res.Should().ContainSingle().Which.Name.Should().Be("Foobar", "the database value should be passed through the DataTransformerAttribute");
            }

            [TestMethod]
            public void ChainsTransformPropertyDecoratedWithTransformerAttributesInOrder()
            {
                var values = new Dictionary<string, object>
                {
                    { "Name", "Hello, world!" }
                };

                var reader = CreateDataReader(values);
                var toTest = new ComplexTypeRowFactory<WithStaticValueToUpper>();
                var res    = toTest.ParseRows(reader, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);

                res.Should().ContainSingle().Which.Name.Should().Be("IS UPPER?", "the database value should be passed through the DataTransformerAttributes in the order they specify");
            }
            
            [TestMethod]
            public void IDataTransformerTransformsData()
            {
                var values = new Dictionary<string, object>
                {
                    { "Column", "Hello, world!" }
                };

                var reader = CreateDataReader(values);
                var toTest = new ComplexTypeRowFactory<SingleColumn>();
                var res    = toTest.ParseRows(reader,
                                              new IDataTransformer[] { new StaticTransformer { Result = "Foobar" } },
                                              CancellationToken.None);

                res.Should().ContainSingle().Which.ShouldBeEquivalentTo(new SingleColumn { Column = "Foobar" }, "the column should be transformed");
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

                var toTest = new ComplexTypeRowFactory<SingleColumn>();
                var res    = toTest.ParseRows(reader,
                                              new IDataTransformer[] { new StaticTransformer { Result = "Foobar" } },
                                              CancellationToken.None);

                res.Should().ContainSingle().Which.ShouldBeEquivalentTo(new SingleColumn { Column = "Foobar" }, "the column should be transformed");
            }

            [TestMethod]
            public void IDataTransformerTransformsDataWithRenamedColumn()
            {
                var values = new Dictionary<string, object>
                {
                    { "MyRenamedColumn", "Hello, world!" }
                };

                var reader = CreateDataReader(values);
                var toTest = new ComplexTypeRowFactory<RenamedColumn>();
                var res    = toTest.ParseRows(reader,
                                              new IDataTransformer[] { new StaticTransformer { Result = "Foobar" } },
                                              CancellationToken.None);

                res.Should().ContainSingle().Which.ShouldBeEquivalentTo(new RenamedColumn { Column = "Foobar" }, "the column should be transformed");
            }

            [TestMethod]
            public void IDataTransformerNotCalledWhenCanNotTransformValue()
            {
                var values = new Dictionary<string, object>
                {
                    { "Column", "Hello, world!" }
                };

                var reader = CreateDataReader(values, true);
                var toTest = new ComplexTypeRowFactory<SingleColumn>();
                var res    = toTest.ParseRows(reader,
                                              new IDataTransformer[] { new NeverTransformer() },
                                              CancellationToken.None);

                res.Should().ContainSingle().Which.ShouldBeEquivalentTo(new SingleColumn { Column = "Hello, world!" }, "the column should not be transformed");
            }

            [TestMethod]
            public void IDataTransformerNotCalledWhenCanNotTransformValueWithDBNullValue()
            {
                var values = new Dictionary<string, object>
                {
                    { "Column", null }
                };

                var reader = CreateDataReader(values, true);
                Mock.Get(reader).Setup(r => r.GetFieldType(0)).Returns(typeof(string));
                var toTest = new ComplexTypeRowFactory<SingleColumn>();
                var res    = toTest.ParseRows(reader,
                                              new IDataTransformer[] { new NeverTransformer() },
                                              CancellationToken.None);

                res.Should().ContainSingle().Which.ShouldBeEquivalentTo(new SingleColumn(), "the column should not be transformed");
            }

            [TestMethod]
            public void IDataTransformerNotCalledWhenCanNotTransformValueWithRenamedColumn()
            {
                var values = new Dictionary<string, object>
                {
                    { "MyRenamedColumn", "Hello, world!" }
                };

                var reader = CreateDataReader(values, true);
                var toTest = new ComplexTypeRowFactory<RenamedColumn>();
                var res    = toTest.ParseRows(reader,
                                              new IDataTransformer[] { new NeverTransformer() },
                                              CancellationToken.None);

                res.Should().ContainSingle().Which.ShouldBeEquivalentTo(new RenamedColumn { Column = "Hello, world!" }, "the column should not be transformed");
            }

            [TestMethod]
            public void DoesNotPassNullableToTransformationAttributes()
            {
                var values = new Dictionary<string, object>
                {
                    { "Value", 1.0 },
                    { "FooBar", 4L }
                };

                var reader = CreateDataReader(values, true);

                var toTest = new ComplexTypeRowFactory<NullableChecker>();
                var res    = toTest.ParseRows(reader, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);

                res.Should().ContainSingle().Which.ShouldBeEquivalentTo(new NullableChecker { Value = 1.0, FooBar = FooBar.Foo });
            }

            [TestMethod]
            public void DoesNotPassNullableToTransformationAttributesWhenPassedNull()
            {
                var values = new Dictionary<string, object>
                {
                    { "Value", null },
                    { "FooBar", null }
                };

                var reader = CreateDataReader(values, true);
                Mock.Get(reader).Setup(r => r.GetFieldType(0)).Returns(typeof(int));
                Mock.Get(reader).Setup(r => r.GetFieldType(1)).Returns(typeof(double));
                var xformer = new Mock<IDataTransformer>();

                var toTest = new ComplexTypeRowFactory<NullableChecker>();
                var res    = toTest.ParseRows(reader, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);

                res.Should().ContainSingle().Which.ShouldBeEquivalentTo(new NullableChecker());
            }

            [TestMethod]
            public void DoesNotPassNullableToTransformers()
            {
                var values = new Dictionary<string, object>
                {
                    { "Value", 42.0 },
                    { "FooBar", 6 }
                };

                var reader  = CreateDataReader(values, true);
                Mock.Get(reader).Setup(r => r.GetFieldType(0)).Returns(typeof(int));
                Mock.Get(reader).Setup(r => r.GetFieldType(1)).Returns(typeof(double));
                var xformer = new Mock<IDataTransformer>();

                xformer.Setup(x => x.CanTransform(It.IsAny<object>(), typeof(double), true, It.Is<IEnumerable<Attribute>>(attrs => attrs != null)))
                       .Returns(true)
                       .Verifiable();
                xformer.Setup(x => x.Transform(It.IsAny<object>(), typeof(double), true, It.Is<IEnumerable<Attribute>>(attrs => attrs != null)))
                       .Returns<object, Type, bool, IEnumerable<Attribute>>((o, t, b, e) =>
                       {
                           Assert.IsTrue(b, "isNullable must be true for a Nullable<T> property");
                           if (t.IsGenericType)
                               Assert.AreNotEqual(typeof(Nullable<>), t.GetGenericTypeDefinition(), "A Nullable<T> type can not be passed to a DataTransformerAttributeBase");

                           return o;
                       })
                       .Verifiable();

                var toTest = new ComplexTypeRowFactory<NullableChecker>();
                var res    = toTest.ParseRows(reader, new[] { xformer.Object }, CancellationToken.None);

                res.Should().ContainSingle().Which.ShouldBeEquivalentTo(new NullableChecker { Value = 42, FooBar = FooBar.Bar });
            }

            [TestMethod]
            public void DoesNotPassNullableToTransformersWhenPassedNull()
            {
                var values = new Dictionary<string, object>
                {
                    { "Value", null },
                    { "FooBar", null }
                };

                var reader  = CreateDataReader(values, true);
                Mock.Get(reader).Setup(r => r.GetFieldType(0)).Returns(typeof(int));
                Mock.Get(reader).Setup(r => r.GetFieldType(1)).Returns(typeof(double));
                var xformer = new Mock<IDataTransformer>();

                xformer.Setup(x => x.CanTransform(It.IsAny<object>(), typeof(double), true, It.Is<IEnumerable<Attribute>>(attrs => attrs != null)))
                       .Returns(true)
                       .Verifiable();
                xformer.Setup(x => x.Transform(It.IsAny<object>(), typeof(double), true, It.Is<IEnumerable<Attribute>>(attrs => attrs != null)))
                       .Returns<object, Type, bool, IEnumerable<Attribute>>((o, t, b, e) =>
                       {
                           b.Should().BeTrue("isNullable must be true for a Nullable<T> property");
                           
                           if (t.IsGenericType)
                               Assert.AreNotEqual(typeof(Nullable<>), t.GetGenericTypeDefinition(), "A Nullable<T> type can not be passed to a DataTransformerAttributeBase");

                           return o;
                       })
                       .Verifiable();

                var toTest = new ComplexTypeRowFactory<NullableChecker>();
                var res    = toTest.ParseRows(reader, new[] { xformer.Object }, CancellationToken.None);

                res.Should().ContainSingle().Which.ShouldBeEquivalentTo(new NullableChecker());
            }

            [TestMethod]
            public void TypedTransformerTransformsStrings()
            {
                var values = new Dictionary<string, object>
                {
                    { "Column", "Foo" }
                };

                var reader  = CreateDataReader(values, false);
                var xformer = new Mock<IDataTransformer<string>>();

                xformer.Setup(x => x.Transform("Foo", It.Is<IEnumerable<Attribute>>(attrs => attrs != null)))
                       .Returns("Bar");

                var toTest = new ComplexTypeRowFactory<SingleColumn>();
                var res    = toTest.ParseRows(reader, new[] { xformer.Object }, CancellationToken.None);

                res.Should().ContainSingle().Which.ShouldBeEquivalentTo(new SingleColumn { Column = "Bar" });
                xformer.Verify(x => x.CanTransform(It.IsAny<object>(), It.IsAny<Type>(), It.IsAny<bool>(), It.IsAny<IEnumerable<Attribute>>()), Times.Never());
                xformer.Verify(x => x.Transform(It.IsAny<object>(), It.IsAny<Type>(), It.IsAny<bool>(), It.IsAny<IEnumerable<Attribute>>()), Times.Never());
            }

            [TestMethod]
            public void TypedTransformerNotUsedForColumnWithDifferentType()
            {
                var values = new Dictionary<string, object>
                {
                    { "Column", "Foo" }
                };

                var reader  = CreateDataReader(values, false);
                var xformer = new Mock<IDataTransformer<int>>();

                var toTest = new ComplexTypeRowFactory<SingleColumn>();
                var res    = toTest.ParseRows(reader, new[] { xformer.Object }, CancellationToken.None);

                res.Should().ContainSingle().Which.ShouldBeEquivalentTo(new SingleColumn { Column = "Foo" });
                xformer.Verify(x => x.CanTransform(It.IsAny<object>(), It.IsAny<Type>(), It.IsAny<bool>(), It.IsAny<IEnumerable<Attribute>>()), Times.Never());
                xformer.Verify(x => x.Transform(It.IsAny<object>(), It.IsAny<Type>(), It.IsAny<bool>(), It.IsAny<IEnumerable<Attribute>>()), Times.Never());
                xformer.Verify(x => x.Transform(It.IsAny<int>(), It.IsAny<IEnumerable<Attribute>>()), Times.Never());
            }

            [TestMethod]
            public void TypedTransformerTransformsValueTypes()
            {
                var values = new Dictionary<string, object>
                {
                    { "Value", 13 }
                };

                var reader  = CreateDataReader(values, false);
                var xformer = new Mock<IDataTransformer<int>>();
                xformer.Setup(x => x.Transform(13, It.IsAny<IEnumerable<Attribute>>()))
                       .Returns(42);

                var toTest = new ComplexTypeRowFactory<ConvertToInt>();
                var res    = toTest.ParseRows(reader, new[] { xformer.Object }, CancellationToken.None);

                res.Should().ContainSingle().Which.ShouldBeEquivalentTo(new ConvertToInt { Value = 42 });
                xformer.Verify(x => x.CanTransform(It.IsAny<object>(), It.IsAny<Type>(), It.IsAny<bool>(), It.IsAny<IEnumerable<Attribute>>()), Times.Never());
                xformer.Verify(x => x.Transform(It.IsAny<object>(), It.IsAny<Type>(), It.IsAny<bool>(), It.IsAny<IEnumerable<Attribute>>()), Times.Never());
            }

            [TestMethod]
            public void TypedTransformerTransformsConvertedValueTypes()
            {
                var values = new Dictionary<string, object>
                {
                    { "Value", 13M }
                };

                var reader  = CreateDataReader(values, false);
                var xformer = new Mock<IDataTransformer<double>>();
                xformer.Setup(x => x.Transform(13, It.IsAny<IEnumerable<Attribute>>()))
                       .Returns(42);

                var toTest = new ComplexTypeRowFactory<ConvertToDouble>();
                var res    = toTest.ParseRows(reader, new[] { xformer.Object }, CancellationToken.None);

                res.Should().ContainSingle().Which.ShouldBeEquivalentTo(new ConvertToDouble { Value = 42 });
                xformer.Verify(x => x.CanTransform(It.IsAny<object>(), It.IsAny<Type>(), It.IsAny<bool>(), It.IsAny<IEnumerable<Attribute>>()), Times.Never());
                xformer.Verify(x => x.Transform(It.IsAny<object>(), It.IsAny<Type>(), It.IsAny<bool>(), It.IsAny<IEnumerable<Attribute>>()), Times.Never());
            }

            [TestMethod]
            public void MultipleTypedTransformersDoNotCauseValuesToBeRetrievedBoxed()
            {
                var values = new Dictionary<string, object>
                {
                    { "Value",  42M  },
                    { "FooBar", "Not a Match" }
                };

                var reader = CreateDataReader(values);
                var xformer1 = new Transformer<string>(Tuple.Create("Not a Match", "Bar"));
                var xformer2 = new Transformer<double>(Tuple.Create(42.0, 84.0));

                var toTest = new ComplexTypeRowFactory<MultipleChecker>();
                var res    = toTest.ParseRows(reader, new IDataTransformer[] { xformer1, xformer2 }, CancellationToken.None);

                res.Should().ContainSingle().Which
                   .ShouldBeEquivalentTo(
                   new MultipleChecker
                   {
                       Value = 84.0,
                       FooBar = FooBar.Bar
                   });
            }

            [TestMethod]
            public void AllTypedTransformersDoNotCauseValuesToBeRetrievedBoxed()
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
                var xformer1 = new Transformer<string>(Tuple.Create("Hello, World!", "Hello, World!Hello, World!"));
                var xformer2 = new Transformer<int>(Tuple.Create(99, 33));
                var xformer3 = new Transformer<double>(Tuple.Create(42.0, 45.0));

                var toTest = new ComplexTypeRowFactory<SingleResultSet>();
                var res    = toTest.ParseRows(reader, new IDataTransformer[] { xformer1, xformer2, xformer3 }, CancellationToken.None);

                res.Should().ContainSingle().Which
                   .ShouldBeEquivalentTo(
                   new SingleResultSet
                   {
                       String = "Hello, World!Hello, World!",
                       Double = 45.0,
                       Decimal = 100M,
                       Int = 33,
                       Long = 1028130L,
                       Date = new DateTime(1982, 1, 31),
                       FooBar = FooBar.Bar
                   });
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
                var toTest = new ComplexTypeRowFactory<Row>().ParseRows(reader, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);

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
                var toTest = new ComplexTypeRowFactory<Row>().ParseRows(reader, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);

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
                var toTest = new ComplexTypeRowFactory<Row>();

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
                var toTest = new ComplexTypeRowFactory<Row>();

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
                var toTest = new ComplexTypeRowFactory<Row>();

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
                var toTest = new ComplexTypeRowFactory<Row>().ParseRows(reader, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);

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
                using (GlobalSettings.UseTestInstance())
                {
                    StoredProcedure.MapResultType<Interface, InterfaceImpl>();
                    var data = new Dictionary<string, object>
                    {
                        { "Id", "42" }
                    };

                    var reader = CreateDataReader(data);
                    var toTest = new ComplexTypeRowFactory<Interface>().ParseRows(reader, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);

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
                    });

                var toTest = new ComplexTypeRowFactory<WithStrongTypedDataTransformer>().ParseRows(
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
                    });

                var toTest = new ComplexTypeRowFactory<EnumValue>();
                
                toTest.Invoking(t => t.ParseRows(reader, Enumerable.Empty<IDataTransformer>(), CancellationToken.None))
                      .ShouldThrow<StoredProcedureColumnException>("the stored procedure returns a different type than the underlying type of the enum.");
            }

            [TestMethod]
            public void EnumPropertiesWillConvertNumericTypes()
            {
                var reader = CreateDataReader(new Dictionary<string, object>
                    {
                        { "FooBar", 4L }
                    });

                var toTest = new ComplexTypeRowFactory<EnumValueTypesConverted>().ParseRows(
                    reader, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);

                toTest.Single().Should().NotBeNull("one row should have been returned")
                      .And.Match<EnumValueTypesConverted>(i => i.FooBar == FooBar.Foo, "The value should be converted to the underlying type.");
            }

            [TestMethod]
            public void EnumPropertiesCallTypedConverterTransformNumericResult()
            {
                var reader = CreateDataReader(new Dictionary<string, object>
                    {
                        { "FooBar", 4L }
                    });

                var xformer = new Mock<IDataTransformer<FooBar>>();
                xformer.Setup(x => x.Transform(FooBar.Foo, It.IsAny<IEnumerable<Attribute>>()))
                       .Returns(FooBar.Bar);

                var toTest = new ComplexTypeRowFactory<EnumValueTypesConverted>().ParseRows(
                    reader, new[] { xformer.Object }, CancellationToken.None);

                toTest.Single().Should().NotBeNull("one row should have been returned")
                      .And.Match<EnumValueTypesConverted>(i => i.FooBar == FooBar.Bar, "The value should be altered by the Transformer.");

                xformer.Verify(x => x.CanTransform(It.IsAny<object>(), It.IsAny<Type>(), It.IsAny<bool>(), It.IsAny<IEnumerable<Attribute>>()), Times.Never());
                xformer.Verify(x => x.Transform(It.IsAny<object>(), It.IsAny<Type>(), It.IsAny<bool>(), It.IsAny<IEnumerable<Attribute>>()), Times.Never());
            }

            [TestMethod]
            public void EnumPropertiesCallTypedConverterTransformStringResult()
            {
                var reader = CreateDataReader(new Dictionary<string, object>
                    {
                        { "FooBar", "Foo" }
                    });

                var xformer = new Mock<IDataTransformer<FooBar>>();
                xformer.Setup(x => x.Transform(FooBar.Foo, It.IsAny<IEnumerable<Attribute>>()))
                       .Returns(FooBar.Bar);

                var toTest = new ComplexTypeRowFactory<EnumValueTypesConverted>().ParseRows(
                    reader, new[] { xformer.Object }, CancellationToken.None);

                toTest.Single().Should().NotBeNull("one row should have been returned")
                      .And.Match<EnumValueTypesConverted>(i => i.FooBar == FooBar.Bar, "The value should be altered by the Transformer.");

                xformer.Verify(x => x.CanTransform(It.IsAny<object>(), It.IsAny<Type>(), It.IsAny<bool>(), It.IsAny<IEnumerable<Attribute>>()), Times.Never());
                xformer.Verify(x => x.Transform(It.IsAny<object>(), It.IsAny<Type>(), It.IsAny<bool>(), It.IsAny<IEnumerable<Attribute>>()), Times.Never());
            }

            [TestMethod]
            public void NumericTypeWillConvertToTrueBoolIfMarkedWithConvert()
            {
                var reader = CreateDataReader(new Dictionary<string, object>
                    {
                        { "IsEnabled", 1 }
                    });

                var toTest = new ComplexTypeRowFactory<ConvertToBool>().ParseRows(
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
                    });

                var toTest = new ComplexTypeRowFactory<ConvertToBool>().ParseRows(
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

                var toTest = new ComplexTypeRowFactory<ConvertToBool>().ParseRows(
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

                var toTest = new ComplexTypeRowFactory<ConvertToBool>().ParseRows(
                    reader, new[] { Mock.Of<IDataTransformer>() }, CancellationToken.None);

                toTest.Should().ContainSingle("because one row is returned").Which
                    .Should().Match<ConvertToBool>(i => !i.IsEnabled, "the property should be converted to bool");
            }

            [TestMethod]
            public void NumericTypeWillConvertToTrueBoolIfMarkedWithConvert_WhenTypedTransformerPassed()
            {
                var reader = CreateDataReader(new Dictionary<string, object>
                    {
                        { "IsEnabled", 0 }
                    }, true);

                var xformer = new Mock<IDataTransformer<bool>>();
                xformer.Setup(x => x.Transform(false, It.IsAny<IEnumerable<Attribute>>()))
                       .Returns(true);

                var toTest = new ComplexTypeRowFactory<ConvertToBool>().ParseRows(
                    reader, new[] { xformer.Object }, CancellationToken.None);

                toTest.Should().ContainSingle("because one row is returned").Which
                    .Should().Match<ConvertToBool>(i => i.IsEnabled == true, "the property should be converted to bool");
            }

            [TestMethod]
            public void NumericTypeWillConvertToFalseBoolIfMarkedWithConvert_WhenTypedTransformerPassed()
            {
                var reader = CreateDataReader(new Dictionary<string, object>
                    {
                        { "IsEnabled", 0L }
                    }, true);

                var toTest = new ComplexTypeRowFactory<ConvertToBool>().ParseRows(
                    reader, new[] { Mock.Of<IDataTransformer<bool>>() }, CancellationToken.None);

                toTest.Should().ContainSingle("because one row is returned").Which
                    .Should().Match<ConvertToBool>(i => !i.IsEnabled, "the property should be converted to bool");
            }

            [TestMethod]
            public void NumericTypeWillConvertToTrueNullableBoolIfMarkedWithConvert()
            {
                var reader = CreateDataReader(new Dictionary<string, object>
                    {
                        { "IsEnabled", 1M }
                    });

                var toTest = new ComplexTypeRowFactory<ConvertToNullableBool>().ParseRows(
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

                var toTest = new ComplexTypeRowFactory<ConvertToNullableBool>().ParseRows(
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
                    });

                var toTest = new ComplexTypeRowFactory<ConvertToNullableBool>().ParseRows(
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

                var toTest = new ComplexTypeRowFactory<ConvertToNullableBool>().ParseRows(
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

                var toTest = new ComplexTypeRowFactory<ConvertToNullableBool>().ParseRows(
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

                var toTest = new ComplexTypeRowFactory<ConvertToNullableBool>().ParseRows(
                    reader, new[] { Mock.Of<IDataTransformer>() }, CancellationToken.None);

                toTest.Should().ContainSingle("because one row is returned").Which
                    .Should().Match<ConvertToNullableBool>(i => !i.IsEnabled.HasValue, "the property should not have a value");
            }

            [TestMethod]
            public void TrueBoolWillConvertToNumericTypeIfMarkedWithConvert()
            {
                var reader = CreateDataReader(new Dictionary<string, object>
                    {
                        { "Value", true }
                    });

                var toTest = new ComplexTypeRowFactory<ConvertToInt>().ParseRows(
                    reader, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);

                toTest.Should().ContainSingle("because one row is returned").Which
                    .Should().Match<ConvertToInt>(i => i.Value == 1, "the property should be converted from bool");
            }

            [TestMethod]
            public void FalseBoolWillConvertToNumericTypeIfMarkedWithConvert()
            {
                var reader = CreateDataReader(new Dictionary<string, object>
                    {
                        { "Value", false }
                    });

                var toTest = new ComplexTypeRowFactory<ConvertToInt>().ParseRows(
                    reader, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);

                toTest.Should().ContainSingle("because one row is returned").Which
                    .Should().Match<ConvertToInt>(i => i.Value == 0, "the property should be converted from bool");
            }

            [TestMethod]
            public void GlobalTransformer_UsedToTransformData()
            {
                using (GlobalSettings.UseTestInstance())
                {
                    StoredProcedure.AddGlobalTransformer(new StaticTransformer { Result = "Foobar" });

                    var values = new Dictionary<string, object>
                    {
                        { "Column", "Hello, world!" }
                    };

                    var reader = CreateDataReader(values);
                    var toTest = new ComplexTypeRowFactory<SingleColumn>();
                    var res    = toTest.ParseRows(reader, new IDataTransformer[0], CancellationToken.None);

                    res.Should().ContainSingle().Which.ShouldBeEquivalentTo(new SingleColumn { Column = "Foobar" }, "the column should be transformed");
                }
            }

            [TestMethod]
            public void GlobalTypedTransformer_UsedToTransformOnlyRequiredData()
            {
                using (GlobalSettings.UseTestInstance())
                {
                    StoredProcedure.AddGlobalTransformer(new Transformer<string>(Tuple.Create("Hello, World!", "Hello, World!Hello, World!")));
                    StoredProcedure.AddGlobalTransformer(new Transformer<int>(Tuple.Create(99, 33)));
                    StoredProcedure.AddGlobalTransformer(new Transformer<double>(Tuple.Create(42.0, 45.0)));

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
                    var toTest = new ComplexTypeRowFactory<SingleResultSet>();
                    var res    = toTest.ParseRows(reader, new IDataTransformer[0], CancellationToken.None);

                    res.Should().ContainSingle().Which
                       .ShouldBeEquivalentTo(
                       new SingleResultSet
                       {
                           String = "Hello, World!Hello, World!",
                           Double = 45.0,
                           Decimal = 100M,
                           Int = 33,
                           Long = 1028130L,
                           Date = new DateTime(1982, 1, 31),
                           FooBar = FooBar.Bar
                       });
                }
            }

            private static IDataReader CreateDataReader(Dictionary<string, object> values, bool setupGetValue = false)
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

        [TestClass]
        public class MatchesColumns
        {
            [TestMethod]
            public void SingleColumnMatches_SingleResultColumn()
            {
                int leftoverColumns;
                var toTest = new ComplexTypeRowFactory<SingleColumn>();
                var result = toTest.MatchesColumns(new[] { "Column" }, out leftoverColumns);

                result.Should().BeTrue("because all required property columns were returned");
                leftoverColumns.Should().Be(0, "because only one column exists, and it is used");
            }

            [TestMethod]
            public void SingleColumnMatches_MultipleResultColumn()
            {
                int leftoverColumns;
                var toTest = new ComplexTypeRowFactory<SingleColumn>();
                var result = toTest.MatchesColumns(new[] { "Column", "Foo", "Bar" }, out leftoverColumns);

                result.Should().BeTrue("because all required property columns were returned");
                leftoverColumns.Should().Be(2, "because two of the result columns were not used");
            }

            [TestMethod]
            public void SingleColumn_ReturnsFalse_IfDoesNotMatch()
            {
                int leftoverColumns;
                var toTest = new ComplexTypeRowFactory<SingleColumn>();
                var result = toTest.MatchesColumns(new[] { "Foo", "Bar" }, out leftoverColumns);

                result.Should().BeFalse("because the required column is not in the results");
                leftoverColumns.Should().Be(2, "because 2 of the columns do not match columns on the result");
            }

            [TestMethod]
            public void RenamedColumn_ReturnsTrue_WhenDbColumnNameExists()
            {
                int leftoverColumns;
                var toTest = new ComplexTypeRowFactory<RenamedColumn>();
                var result = toTest.MatchesColumns(new[] { "MyRenamedColumn" }, out leftoverColumns);

                result.Should().BeTrue("because the property's renamed column was returned");
                leftoverColumns.Should().Be(0, "because only one column exists, and it is used");
            }

            [TestMethod]
            public void OptionalColumn_ReturnsTrue_IfOptionalColumnNotPresent()
            {
                int leftoverColumns;
                var toTest = new ComplexTypeRowFactory<WithOptional>();
                var result = toTest.MatchesColumns(new[] { "Column" }, out leftoverColumns);

                result.Should().BeTrue("because all required property columns were returned");
                leftoverColumns.Should().Be(0, "because only one column exists, and it is used");
            }

            [TestMethod]
            public void OptionalColumn_ReturnsFalse_IfRequiredColumnNotPresent()
            {
                int leftoverColumns;
                var toTest = new ComplexTypeRowFactory<WithOptional>();
                var result = toTest.MatchesColumns(new[] { "Optional" }, out leftoverColumns);

                result.Should().BeFalse("because not all required property columns were returned");
                leftoverColumns.Should().Be(0, "because only one column returned, and it is used");
            }

            [TestMethod]
            public void OptionalColumn_ReturnsTrue_IfOptionalColumnIsPresent()
            {
                int leftoverColumns;
                var toTest = new ComplexTypeRowFactory<WithOptional>();
                var result = toTest.MatchesColumns(new[] { "Column", "Optional" }, out leftoverColumns);

                result.Should().BeTrue("because all required property columns were returned");
                leftoverColumns.Should().Be(0, "because only all columns returned are used");
            }

            [TestMethod]
            public void ChildrenHierarchicalPropertyNames_NotRequired()
            {
                int leftoverColumns;
                var toTest = new ComplexTypeRowFactory<WithChildren>();
                var result = toTest.MatchesColumns(new[] { "Id" }, out leftoverColumns);

                result.Should().BeTrue("because enumerable properties can't be set, and should be ignored");
                leftoverColumns.Should().Be(0, "because only one column exists, and it is used");
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

        private class WithOptional
        {
            public string Column { get; set; }
            [OptionalResult]
            public string Optional { get; set; }
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

        private class MultipleChecker
        {
            [ConvertNumeric]
            public double Value { get; set; }
            [ConvertNumeric]
            public FooBar FooBar { get; set; }
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

        private class ConvertToInt
        {
            [ConvertNumeric]
            public int Value { get; set; }
        }

        private class ConvertToDouble
        {
            [ConvertNumeric]
            public double Value { get; set; }
        }

        private class WithChildren
        {
            public int Id { get; set; }
            public IEnumerable<SingleColumn> Children { get; set; }
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
            public string Result { get; set; }

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
                Assert.Fail("Boxed Transform method should never be called.");
                return Transform((uint)value);
            }
        }

        private class Transformer<T> : IDataTransformer<T>
        {
            private readonly Dictionary<T, T> transforms;

            public Transformer(params Tuple<T, T>[] transforms)
            {
                this.transforms = transforms.ToDictionary(t => t.Item1, t => t.Item2);
            }

            public T Transform(T value, IEnumerable<Attribute> propertyAttributes)
            {
                T result;
                if (transforms.TryGetValue(value, out result))
                    return result;

                return value;
            }

            public bool CanTransform(object value, Type targetType, bool isNullable, IEnumerable<Attribute> propertyAttributes)
            {
                throw new NotSupportedException();
            }

            public object Transform(object value, Type targetType, bool isNullable, IEnumerable<Attribute> propertyAttributes)
            {
                throw new NotSupportedException();
            }
        }

        public enum FooBar
        {
            Foo = 4,
            Bar = 6
        }
        #endregion
    }
}
