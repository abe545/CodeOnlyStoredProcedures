using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeOnlyStoredProcedure;
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
    public class SimpleTypeRowFactoryTests
    {
        [TestClass]
        public class ParseRows
        {
            [TestMethod]
            public void ParsesSingleInteger()
            {
                var rdr = new Mock<IDataReader>();
                rdr.Setup(r => r.GetFieldType(0)).Returns(typeof(int));
                rdr.Setup(r => r.GetInt32(0)).Returns(42);
                rdr.SetupSequence(r => r.Read())
                   .Returns(true)
                   .Returns(false);

                var toTest = new SimpleTypeRowFactory<int>();

                toTest.ParseRows(rdr.Object, Enumerable.Empty<IDataTransformer>(), CancellationToken.None)
                      .Single().Should().Be(42);
            }
            
            [TestMethod]
            public void GlobalConvertAll_WillTransformSingleInteger()
            {
                var rdr = new Mock<IDataReader>();
                rdr.Setup(r => r.GetFieldType(0)).Returns(typeof(int));
                rdr.Setup(r => r.GetInt32(0)).Returns(42);
                rdr.SetupSequence(r => r.Read())
                   .Returns(true)
                   .Returns(false);

                using (GlobalSettings.UseTestInstance())
                {
                    StoredProcedure.EnableConvertOnAllNumericValues();
                    var toTest = new SimpleTypeRowFactory<double>();

                    toTest.ParseRows(rdr.Object, Enumerable.Empty<IDataTransformer>(), CancellationToken.None)
                          .Single().Should().Be(42.0);
                }
            }

            // This test will fail, but I'm not 100% sure if I want to support this.
            // Should values be converted after going through IDataTransformers? If they should,
            // then I'm going to need a lot more tests to cover all the edge cases.
            [Ignore]
            [TestMethod]
            public void GlobalConvertAll_WillTransformSingleIntegerWhenTransformerUsed()
            {
                var rdr = new Mock<IDataReader>();
                rdr.Setup(r => r.GetFieldType(0)).Returns(typeof(double));
                rdr.Setup(r => r.GetValue(0)).Returns(42.0);
                rdr.SetupSequence(r => r.Read())
                   .Returns(true)
                   .Returns(false);

                var xf = new Mock<IDataTransformer>();
                xf.Setup(x => x.CanTransform(42.0, typeof(float), false, It.IsAny<IEnumerable<Attribute>>()))
                  .Returns(true);
                xf.Setup(x => x.Transform(42.0, typeof(float), false, It.IsAny<IEnumerable<Attribute>>()))
                  .Returns(2f);

                using (GlobalSettings.UseTestInstance())
                {
                    StoredProcedure.EnableConvertOnAllNumericValues();
                    var toTest = new SimpleTypeRowFactory<float>();

                    toTest.ParseRows(rdr.Object, new[] { xf.Object }, CancellationToken.None)
                          .Single().Should().Be(2f);
                }
            }

            [TestMethod]
            public void ParsesThroughDataTransformer()
            {
                var rdr = new Mock<IDataReader>();
                rdr.Setup(r => r.GetFieldType(0)).Returns(typeof(int));
                rdr.Setup(r => r.GetValue(0)).Returns(99);
                rdr.SetupSequence(r => r.Read())
                   .Returns(true)
                   .Returns(false);

                var xformer = new Mock<IDataTransformer>();
                xformer.Setup(x => x.CanTransform(99, typeof(int), false, It.Is<IEnumerable<Attribute>>(attrs => attrs != null)))
                       .Returns(true);
                xformer.Setup(x => x.Transform(99, typeof(int), false, It.Is<IEnumerable<Attribute>>(attrs => attrs != null)))
                       .Returns(42);

                var toTest = new SimpleTypeRowFactory<int>();

                toTest.ParseRows(rdr.Object, new[] { xformer.Object }, CancellationToken.None)
                      .Single().Should().Be(42);
            }

            [TestMethod]
            public void ParsesThroughGlobalDataTransformer()
            {
                var rdr = new Mock<IDataReader>();
                rdr.Setup(r => r.GetFieldType(0)).Returns(typeof(int));
                rdr.Setup(r => r.GetValue(0)).Returns(99);
                rdr.SetupSequence(r => r.Read())
                   .Returns(true)
                   .Returns(false);

                var xformer = new Mock<IDataTransformer>();
                xformer.Setup(x => x.CanTransform(99, typeof(int), false, It.Is<IEnumerable<Attribute>>(attrs => attrs != null)))
                       .Returns(true);
                xformer.Setup(x => x.Transform(99, typeof(int), false, It.Is<IEnumerable<Attribute>>(attrs => attrs != null)))
                       .Returns(42);

                using (GlobalSettings.UseTestInstance())
                {
                    StoredProcedure.AddGlobalTransformer(xformer.Object);
                    var toTest = new SimpleTypeRowFactory<int>();

                    toTest.ParseRows(rdr.Object, new IDataTransformer[0], CancellationToken.None)
                          .Single().Should().Be(42);
                }
            }

            [TestMethod]
            public void ParsesNullableDoubleAsNull()
            {
                var rdr = new Mock<IDataReader>();
                rdr.Setup(r => r.GetFieldType(0)).Returns(typeof(double));
                rdr.Setup(r => r.IsDBNull(0)).Returns(true);
                rdr.SetupSequence(r => r.Read())
                   .Returns(true)
                   .Returns(false);

                var toTest = RowFactory<double?>.Create();

                toTest.ParseRows(rdr.Object, Enumerable.Empty<IDataTransformer>(), CancellationToken.None)
                      .Single().Should().Be(default(Nullable<double>));
            }

            [TestMethod]
            public void ReturnsMultipleStrings()
            {
                var rdr = new Mock<IDataReader>();
                rdr.Setup(r => r.GetFieldType(0)).Returns(typeof(string));
                rdr.SetupSequence(r => r.IsDBNull(0))
                   .Returns(false)
                   .Returns(true)
                   .Returns(false);
                rdr.SetupSequence(r => r.Read())
                   .Returns(true)
                   .Returns(true)
                   .Returns(true)
                   .Returns(false);
                rdr.SetupSequence(r => r.GetString(0))
                   .Returns("Foo")
                   .Returns("Bar");

                var toTest = new SimpleTypeRowFactory<string>();

                toTest.ParseRows(rdr.Object, Enumerable.Empty<IDataTransformer>(), CancellationToken.None)
                      .Should().ContainInOrder("Foo", null, "Bar");
            }

            [TestMethod]
            public void ParsesByteArray_AsObject()
            {
                var rdr = new Mock<IDataReader>();
                rdr.Setup(r => r.GetFieldType(0)).Returns(typeof(byte[]));
                rdr.SetupSequence(r => r.IsDBNull(0))
                    .Returns(false)
                    .Returns(true)
                    .Returns(false);
                rdr.SetupSequence(r => r.Read())
                   .Returns(true)
                   .Returns(true)
                   .Returns(true)
                   .Returns(false);
                rdr.SetupSequence(r => r.GetValue(0))
                    .Returns(new byte[] { 0 })
                    .Returns(new byte[] { 1 });

                var toTest = new SimpleTypeRowFactory<byte[]>();

                var res = toTest.ParseRows(rdr.Object, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);
                res.First().Should().ContainSingle().Which.Should().Be(0);
                res.Skip(1).First().Should().BeNull();
                res.Last().Should().ContainSingle().Which.Should().Be(1);
            }

            [TestMethod]
            public void ParsesByteArray_AsInt_WhenGlobalConvert()
            {
                using (GlobalSettings.UseTestInstance())
                {
                    GlobalSettings.Instance.ConvertAllNumericValues = true;

                    var rdr = new Mock<IDataReader>();
                    rdr.Setup(r => r.GetFieldType(0)).Returns(typeof(int));
                    rdr.SetupSequence(r => r.IsDBNull(0))
                        .Returns(false)
                        .Returns(true)
                        .Returns(false);
                    rdr.SetupSequence(r => r.Read())
                       .Returns(true)
                       .Returns(true)
                       .Returns(true)
                       .Returns(false);
                    rdr.SetupSequence(r => r.GetValue(0))
                        .Returns(0)
                        .Returns(1);

                    var toTest = new SimpleTypeRowFactory<byte[]>();

                    var res = toTest.ParseRows(rdr.Object, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);
                    res.First().Should().ContainInOrder(BitConverter.GetBytes(0));
                    res.Skip(1).First().Should().BeNull();
                    res.Last().Should().ContainInOrder(BitConverter.GetBytes(1));
                }
            }

            [TestMethod]
            public void HelpfulExceptionWhenColumnWrongType()
            {
                var reader  = new Mock<IDataReader>();

                reader.SetupGet(r => r.FieldCount)
                      .Returns(1);

                reader.SetupSequence(r => r.Read())
                      .Returns(true)
                      .Returns(false);

                reader.Setup(r => r.GetFieldType(0)).Returns(typeof(Decimal));
                reader.Setup(r => r.GetDecimal(0))  .Returns(42M);

                var toTest = new SimpleTypeRowFactory<int>();

                toTest.Invoking(f => f.ParseRows(reader.Object, new IDataTransformer[0], CancellationToken.None))
                      .ShouldThrow<StoredProcedureColumnException>()
                      .WithMessage("Error setting [Int32] result. Stored Procedure returns [Decimal].");
            }
        }

#if !NET40
        [TestClass]
        public class ParseRowsAsync
        {
            [TestMethod]
            public async Task ParsesSingleInteger()
            {
                var rdr = new Mock<DbDataReader>();
                rdr.Setup(r => r.GetFieldType(0)).Returns(typeof(int));
                rdr.Setup(r => r.GetInt32(0)).Returns(42);
                rdr.SetupSequence(r => r.ReadAsync(It.IsAny<CancellationToken>()))
                   .ReturnsAsync(true)
                   .ReturnsAsync(false);

                var toTest = new SimpleTypeRowFactory<int>();

                var res = await toTest.ParseRowsAsync(rdr.Object, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);
                res.Single().Should().Be(42);
            }

            [TestMethod]
            public async Task ParsesThroughDataTransformer()
            {
                var rdr = new Mock<DbDataReader>();
                rdr.Setup(r => r.GetFieldType(0)).Returns(typeof(int));
                rdr.Setup(r => r.GetValue(0)).Returns(99);
                rdr.SetupSequence(r => r.ReadAsync(It.IsAny<CancellationToken>()))
                   .ReturnsAsync(true)
                   .ReturnsAsync(false);

                var xformer = new Mock<IDataTransformer>();
                xformer.Setup(x => x.CanTransform(99, typeof(int), false, It.Is<IEnumerable<Attribute>>(attrs => attrs != null)))
                       .Returns(true);
                xformer.Setup(x => x.Transform(99, typeof(int), false, It.Is<IEnumerable<Attribute>>(attrs => attrs != null)))
                       .Returns(42);

                var toTest = new SimpleTypeRowFactory<int>();

                var res = await toTest.ParseRowsAsync(rdr.Object, new[] { xformer.Object }, CancellationToken.None);
                res.Single().Should().Be(42);
            }

            [TestMethod]
            public async Task ParsesThroughGlobalDataTransformer()
            {
                var rdr = new Mock<DbDataReader>();
                rdr.Setup(r => r.GetFieldType(0)).Returns(typeof(int));
                rdr.Setup(r => r.GetValue(0)).Returns(99);
                rdr.SetupSequence(r => r.ReadAsync(It.IsAny<CancellationToken>()))
                   .ReturnsAsync(true)
                   .ReturnsAsync(false);

                var xformer = new Mock<IDataTransformer>();
                xformer.Setup(x => x.CanTransform(99, typeof(int), false, It.Is<IEnumerable<Attribute>>(attrs => attrs != null)))
                       .Returns(true);
                xformer.Setup(x => x.Transform(99, typeof(int), false, It.Is<IEnumerable<Attribute>>(attrs => attrs != null)))
                       .Returns(42);

                using (GlobalSettings.UseTestInstance())
                {
                    StoredProcedure.AddGlobalTransformer(xformer.Object);
                    var toTest = new SimpleTypeRowFactory<int>();

                    var res = await toTest.ParseRowsAsync(rdr.Object, new IDataTransformer[0], CancellationToken.None);
                    res.Single().Should().Be(42);
                }
            }

            [TestMethod]
            public async Task ParsesNullableDoubleAsNull()
            {
                var rdr = new Mock<DbDataReader>();
                rdr.Setup(r => r.GetFieldType(0)).Returns(typeof(double));
                rdr.Setup(r => r.IsDBNull(0)).Returns(true);
                rdr.SetupSequence(r => r.ReadAsync(It.IsAny<CancellationToken>()))
                   .ReturnsAsync(true)
                   .ReturnsAsync(false);

                var toTest = RowFactory<double?>.Create(); 

                var res = await toTest.ParseRowsAsync(rdr.Object, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);
                res.Single().Should().Be(default(Nullable<double>));
            }

            [TestMethod]
            public async Task ReturnsMultipleStrings()
            {
                var rdr = new Mock<DbDataReader>();
                rdr.Setup(r => r.GetFieldType(0)).Returns(typeof(string));
                rdr.SetupSequence(r => r.IsDBNull(0))
                   .Returns(false)
                   .Returns(true)
                   .Returns(false);
                rdr.SetupSequence(r => r.ReadAsync(It.IsAny<CancellationToken>()))
                   .ReturnsAsync(true)
                   .ReturnsAsync(true)
                   .ReturnsAsync(true)
                   .ReturnsAsync(false);
                rdr.SetupSequence(r => r.GetString(0))
                   .Returns("Foo")
                   .Returns("Bar");

                var toTest = new SimpleTypeRowFactory<string>();

                var res = await toTest.ParseRowsAsync(rdr.Object, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);
                res.Should().ContainInOrder("Foo", null, "Bar");
            }
        }
#endif
    }
}
