using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeOnlyStoredProcedure;
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
    public class EnumRowFactoryTests
    {
        [TestClass]
        public class ParseRows
        {
            [TestMethod]
            public void ReturnsEnumViaValue()
            {
                var rdr = new Mock<IDataReader>();
                rdr.Setup(r => r.GetFieldType(0)).Returns(typeof(int));
                rdr.Setup(r => r.GetInt32(0)).Returns(1);
                rdr.SetupSequence(r => r.Read())
                   .Returns(true)
                   .Returns(false);

                var toTest = RowFactory<IntEnum>.Create();

                toTest.ParseRows(rdr.Object, Enumerable.Empty<IDataTransformer>(), CancellationToken.None)
                      .Single().Should().Be(IntEnum.One);
            }

            [TestMethod]
            public void ReturnsEnumViaBoxedValue()
            {
                var rdr = new Mock<IDataReader>();
                rdr.Setup(r => r.GetFieldType(0)).Returns(typeof(int));
                rdr.Setup(r => r.GetValue(0)).Returns(-1);
                rdr.SetupSequence(r => r.Read())
                   .Returns(true)
                   .Returns(false);

                var toTest = RowFactory<IntEnum>.Create();
                var xf = new Mock<IDataTransformer>();
                xf.Setup(x => x.CanTransform(-1, typeof(IntEnum), false, It.Is<IEnumerable<Attribute>>(attrs => attrs != null)))
                  .Returns(true);
                xf.Setup(x => x.Transform(-1, typeof(IntEnum), false, It.Is<IEnumerable<Attribute>>(attrs => attrs != null)))
                  .Returns(1);

                toTest.ParseRows(rdr.Object, new[] { xf.Object }, CancellationToken.None)
                      .Single().Should().Be(IntEnum.One);
            }

            [TestMethod]
            public void ReturnsUshortEnumViaValue()
            {
                var rdr = new Mock<IDataReader>();
                rdr.Setup(r => r.GetFieldType(0)).Returns(typeof(short));
                rdr.Setup(r => r.GetInt16(0)).Returns(-1);
                rdr.SetupSequence(r => r.Read())
                   .Returns(true)
                   .Returns(false);

                var toTest = RowFactory<UshortEnum>.Create();

                toTest.ParseRows(rdr.Object, Enumerable.Empty<IDataTransformer>(), CancellationToken.None)
                      .Single().Should().Be(UshortEnum.Max);
            }

            [TestMethod]
            public void ReturnsUshortEnumViaBoxedValue()
            {
                var rdr = new Mock<IDataReader>();
                rdr.Setup(r => r.GetFieldType(0)).Returns(typeof(short));
                rdr.Setup(r => r.GetValue(0)).Returns((short)-1);
                rdr.SetupSequence(r => r.Read())
                   .Returns(true)
                   .Returns(false);

                var toTest = RowFactory<UshortEnum>.Create();
                var xf = new Mock<IDataTransformer>();
                xf.Setup(x => x.CanTransform((short)-1, typeof(UshortEnum), false, It.Is<IEnumerable<Attribute>>(attrs => attrs != null)))
                  .Returns(true);
                xf.Setup(x => x.Transform((short)-1, typeof(UshortEnum), false, It.Is<IEnumerable<Attribute>>(attrs => attrs != null)))
                  .Returns(ushort.MaxValue);

                toTest.ParseRows(rdr.Object, new[] { xf.Object }, CancellationToken.None)
                      .Single().Should().Be(UshortEnum.Max);
            }

            [TestMethod]
            public void ReturnsNullableEnumValuesViaValue()
            {
                var rdr = new Mock<IDataReader>();
                rdr.Setup(r => r.GetFieldType(0)).Returns(typeof(int));
                rdr.SetupSequence(r => r.IsDBNull(0))
                   .Returns(false)
                   .Returns(true)
                   .Returns(false)
                   .Returns(false);
                rdr.SetupSequence(r => r.GetInt32(0))
                   .Returns(1)
                   .Returns(2)
                   .Returns(0);
                rdr.SetupSequence(r => r.Read())
                   .Returns(true)
                   .Returns(true)
                   .Returns(true)
                   .Returns(true)
                   .Returns(false);

                var toTest = RowFactory<IntEnum?>.Create();

                toTest.ParseRows(rdr.Object, Enumerable.Empty<IDataTransformer>(), CancellationToken.None)
                      .Should().ContainInOrder(IntEnum.One, null, IntEnum.Two, IntEnum.Zero);
            }

            [TestMethod]
            public void ReturnsNullableEnumValuesViaBoxedValue()
            {
                var rdr = new Mock<IDataReader>();
                rdr.Setup(r => r.GetFieldType(0)).Returns(typeof(int));
                rdr.SetupSequence(r => r.IsDBNull(0))
                   .Returns(false)
                   .Returns(true)
                   .Returns(false)
                   .Returns(false);
                rdr.SetupSequence(r => r.GetValue(0))
                   .Returns(-1)
                   .Returns(2)
                   .Returns(0);
                rdr.SetupSequence(r => r.Read())
                   .Returns(true)
                   .Returns(true)
                   .Returns(true)
                   .Returns(true)
                   .Returns(false);

                var toTest = RowFactory<IntEnum?>.Create();
                var xf = new Mock<IDataTransformer>();
                xf.Setup(x => x.CanTransform(-1, typeof(IntEnum), true, It.Is<IEnumerable<Attribute>>(attrs => attrs != null)))
                  .Returns(true);
                xf.Setup(x => x.Transform(-1, typeof(IntEnum), true, It.Is<IEnumerable<Attribute>>(attrs => attrs != null)))
                  .Returns(1);

                toTest.ParseRows(rdr.Object, new[] { xf.Object }, CancellationToken.None)
                      .Should().ContainInOrder(IntEnum.One, null, IntEnum.Two, IntEnum.Zero);
            }

            [TestMethod]
            public void ReturnsEnumViaName()
            {
                var rdr = new Mock<IDataReader>();
                rdr.Setup(r => r.GetFieldType(0)).Returns(typeof(string));
                rdr.Setup(r => r.GetString(0)).Returns("One");
                rdr.SetupSequence(r => r.Read())
                   .Returns(true)
                   .Returns(false);

                var toTest = RowFactory<IntEnum>.Create();

                toTest.ParseRows(rdr.Object, Enumerable.Empty<IDataTransformer>(), CancellationToken.None)
                      .Single().Should().Be(IntEnum.One);
            }

            [TestMethod]
            public void ParsesMultipleFlagsViaName()
            {
                var rdr = new Mock<IDataReader>();
                rdr.Setup(r => r.GetFieldType(0)).Returns(typeof(string));
                rdr.Setup(r => r.GetString(0)).Returns("Red, Blue");
                rdr.SetupSequence(r => r.Read())
                   .Returns(true)
                   .Returns(false);

                var toTest = RowFactory<FlagsEnum>.Create();

                toTest.ParseRows(rdr.Object, Enumerable.Empty<IDataTransformer>(), CancellationToken.None)
                      .Single().Should().Be(FlagsEnum.Red | FlagsEnum.Blue);
            }

            [TestMethod]
            public void ReturnsEnumViaNameTransformsStringBeforeParsing()
            {
                var rdr = new Mock<IDataReader>();
                rdr.Setup(r => r.GetFieldType(0)).Returns(typeof(string));
                rdr.Setup(r => r.GetString(0)).Returns("One");
                rdr.SetupSequence(r => r.Read())
                   .Returns(true)
                   .Returns(false);

                var toTest = RowFactory<IntEnum>.Create();
                var xf = new Mock<IDataTransformer>();
                xf.Setup(x => x.CanTransform("One", typeof(IntEnum), false, It.Is<IEnumerable<Attribute>>(attrs => attrs != null)))
                  .Returns(true);
                xf.Setup(x => x.Transform("One", typeof(IntEnum), false, It.Is<IEnumerable<Attribute>>(attrs => attrs != null)))
                  .Returns("Two");

                toTest.ParseRows(rdr.Object, new[] { xf.Object }, CancellationToken.None)
                      .Single().Should().Be(IntEnum.Two);
            }

            [TestMethod]
            public void ReturnsNullableEnumValuesViaName()
            {
                var rdr = new Mock<IDataReader>();
                rdr.Setup(r => r.GetFieldType(0)).Returns(typeof(string));
                rdr.SetupSequence(r => r.IsDBNull(0))
                   .Returns(false)
                   .Returns(true)
                   .Returns(false)
                   .Returns(false);
                rdr.SetupSequence(r => r.GetString(0))
                   .Returns("One")
                   .Returns("Two")
                   .Returns("Zero");
                rdr.SetupSequence(r => r.Read())
                   .Returns(true)
                   .Returns(true)
                   .Returns(true)
                   .Returns(true)
                   .Returns(false);

                var toTest = RowFactory<IntEnum?>.Create();

                toTest.ParseRows(rdr.Object, Enumerable.Empty<IDataTransformer>(), CancellationToken.None)
                      .Should().ContainInOrder(IntEnum.One, null, IntEnum.Two, IntEnum.Zero);
            }

            [TestMethod]
            public void ReturnsNullableEnumValuesViaNameTransformsBeforeParsing()
            {
                var rdr = new Mock<IDataReader>();
                rdr.Setup(r => r.GetFieldType(0)).Returns(typeof(string));
                rdr.SetupSequence(r => r.IsDBNull(0))
                   .Returns(false)
                   .Returns(true)
                   .Returns(false)
                   .Returns(false);
                rdr.SetupSequence(r => r.GetString(0))
                   .Returns("Blah")
                   .Returns("Two")
                   .Returns("Zero");
                rdr.SetupSequence(r => r.Read())
                   .Returns(true)
                   .Returns(true)
                   .Returns(true)
                   .Returns(true)
                   .Returns(false);

                var toTest = RowFactory<IntEnum?>.Create();
                var xf = new Mock<IDataTransformer>();
                xf.Setup(x => x.CanTransform("Blah", typeof(IntEnum), true, It.Is<IEnumerable<Attribute>>(attrs => attrs != null)))
                  .Returns(true);
                xf.Setup(x => x.Transform("Blah", typeof(IntEnum), true, It.Is<IEnumerable<Attribute>>(attrs => attrs != null)))
                  .Returns("One");

                toTest.ParseRows(rdr.Object, new[] { xf.Object }, CancellationToken.None)
                      .Should().ContainInOrder(IntEnum.One, null, IntEnum.Two, IntEnum.Zero);
            }
        }

#if !NET40
        [TestClass]
        public class ParseRowsAsync
        {
            [TestMethod]
            public async Task ReturnsEnumViaValue()
            {
                var rdr = new Mock<DbDataReader>();
                rdr.Setup(r => r.GetFieldType(0)).Returns(typeof(int));
                rdr.Setup(r => r.GetInt32(0)).Returns(1);
                rdr.SetupSequence(r => r.ReadAsync(It.IsAny<CancellationToken>()))
                   .ReturnsAsync(true)
                   .ReturnsAsync(false);

                var toTest = RowFactory<IntEnum>.Create();

                var res = await toTest.ParseRowsAsync(rdr.Object, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);
                res.Single().Should().Be(IntEnum.One);
            }

            [TestMethod]
            public async Task ReturnsUshortEnumViaValue()
            {
                var rdr = new Mock<DbDataReader>();
                rdr.Setup(r => r.GetFieldType(0)).Returns(typeof(short));
                rdr.Setup(r => r.GetInt16(0)).Returns(-1);
                rdr.SetupSequence(r => r.ReadAsync(It.IsAny<CancellationToken>()))
                   .ReturnsAsync(true)
                   .ReturnsAsync(false);

                var toTest = RowFactory<UshortEnum>.Create();

                var res = await toTest.ParseRowsAsync(rdr.Object, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);
                res.Single().Should().Be(UshortEnum.Max);
            }

            [TestMethod]
            public async Task ReturnsNullableEnumValuesViaValue()
            {
                var rdr = new Mock<DbDataReader>();
                rdr.Setup(r => r.GetFieldType(0)).Returns(typeof(int));
                rdr.SetupSequence(r => r.IsDBNull(0))
                   .Returns(false)
                   .Returns(true)
                   .Returns(false)
                   .Returns(false);
                rdr.SetupSequence(r => r.GetInt32(0))
                   .Returns(1)
                   .Returns(2)
                   .Returns(0);
                rdr.SetupSequence(r => r.ReadAsync(It.IsAny<CancellationToken>()))
                   .ReturnsAsync(true)
                   .ReturnsAsync(true)
                   .ReturnsAsync(true)
                   .ReturnsAsync(true)
                   .ReturnsAsync(false);

                var toTest = RowFactory<IntEnum?>.Create();

                var res = await toTest.ParseRowsAsync(rdr.Object, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);
                res.Should().ContainInOrder(IntEnum.One, null, IntEnum.Two, IntEnum.Zero);
            }

            [TestMethod]
            public async Task ReturnsEnumViaName()
            {
                var rdr = new Mock<DbDataReader>();
                rdr.Setup(r => r.GetFieldType(0)).Returns(typeof(string));
                rdr.Setup(r => r.GetString(0)).Returns("One");
                rdr.SetupSequence(r => r.ReadAsync(It.IsAny<CancellationToken>()))
                   .ReturnsAsync(true)
                   .ReturnsAsync(false);

                var toTest = RowFactory<IntEnum>.Create();

                var res = await toTest.ParseRowsAsync(rdr.Object, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);
                res.Single().Should().Be(IntEnum.One);
            }

            [TestMethod]
            public async Task ReturnsNullableEnumValuesViaName()
            {
                var rdr = new Mock<DbDataReader>();
                rdr.Setup(r => r.GetFieldType(0)).Returns(typeof(string));
                rdr.SetupSequence(r => r.IsDBNull(0))
                   .Returns(false)
                   .Returns(true)
                   .Returns(false)
                   .Returns(false);
                rdr.SetupSequence(r => r.GetString(0))
                   .Returns("One")
                   .Returns("Two")
                   .Returns("Zero");
                rdr.SetupSequence(r => r.ReadAsync(It.IsAny<CancellationToken>()))
                   .ReturnsAsync(true)
                   .ReturnsAsync(true)
                   .ReturnsAsync(true)
                   .ReturnsAsync(true)
                   .ReturnsAsync(false);

                var toTest = RowFactory<IntEnum?>.Create();

                var res = await toTest.ParseRowsAsync(rdr.Object, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);
                res.Should().ContainInOrder(IntEnum.One, null, IntEnum.Two, IntEnum.Zero);
            }
        }
#endif

        private enum IntEnum : int
        {
            Zero = 0,
            One = 1,
            Two = 2
        }

        private enum UshortEnum : ushort
        {
            Zero = 0,
            Max = ushort.MaxValue
        }

        [Flags]
        private enum FlagsEnum : long
        {
            Black = 0,
            Red = 1,
            Green = 2,
            Blue = 4
        }
    }
}
