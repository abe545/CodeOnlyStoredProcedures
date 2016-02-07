using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using System.Data;
using Moq;
using CodeOnlyStoredProcedure.RowFactory;
using CodeOnlyStoredProcedure;
using System.Threading;
using System.Dynamic;

#if NET40
namespace CodeOnlyTests.Net40.RowFactory
#else
namespace CodeOnlyTests.RowFactory
#endif
{
    [TestClass]
    public class ExpandoObjectRowFactoryTests
    {
        [TestClass]
        public class Parse
        {
            [TestMethod]
            public void ReturnsAllColumns()
            {
                var values = new Dictionary<string, object>
                {
                    { "String",  "Hello, World!"           },
                    { "Double",  42.0                      },
                    { "Decimal", 100M                      },
                    { "Int",     99                        },
                    { "Long",    1028130L                  },
                    { "Date",    new DateTime(1982, 1, 31) }
                };

                var keys = values.Keys.OrderBy(s => s).ToArray();
                var vals = values.OrderBy(kv => kv.Key).Select(kv => kv.Value).ToArray();

                var reader  = new Mock<IDataReader>();
                var command = new Mock<IDbCommand>();

                command.Setup(d => d.ExecuteReader())
                       .Returns(reader.Object);

                reader.SetupGet(r => r.FieldCount)
                      .Returns(keys.Length);

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

                reader.Setup(r => r.GetValues(It.IsAny<object[]>()))
                      .Callback<object[]>(os => values.OrderBy(kv => kv.Key).Select(kv => kv.Value).ToArray().CopyTo(os, 0));

                reader.Setup(r => r.GetFieldType(It.IsAny<int>()))
                      .Returns((int i) => values[keys[i]].GetType());
                reader.Setup(r => r.GetName(It.IsAny<int>()))
                      .Returns((int i) => keys[i]);

                var toTest = new ExpandoObjectRowFactory<dynamic>();

                var res = toTest.ParseRows(reader.Object, new IDataTransformer[0], CancellationToken.None).ToList();
                var item = res.Should().ContainSingle("because only 1 row should have been returned").Which;

                ((string)  item.String) .Should().Be("Hello, World!",           "the String column has this value");
                ((double)  item.Double) .Should().Be(42.0,                      "the Double column has this value");
                ((decimal) item.Decimal).Should().Be(100M,                      "the Deciaml column has this value");
                ((int)     item.Int)    .Should().Be(99,                        "the Int column has this value");
                ((long)    item.Long)   .Should().Be(1028130L,                  "the Long column has this value");
                ((DateTime)item.Date)   .Should().Be(new DateTime(1982, 1, 31), "the Date column has this value");
            }

            [TestMethod]
            public void IDataTransformer_TransformsData()
            {
                var rdr = new Mock<IDataReader>();
                rdr.Setup(r => r.FieldCount).Returns(1);
                rdr.Setup(r => r.GetFieldType(0)).Returns(typeof(string));
                rdr.Setup(r => r.GetName(0)).Returns("Name");
                rdr.Setup(r => r.GetValues(It.IsAny<object[]>()))
                   .Callback<object[]>(os => os[0] = "Blah");
                rdr.SetupSequence(r => r.Read())
                   .Returns(true)
                   .Returns(false);

                var xf = new Mock<IDataTransformer>();
                xf.Setup(x => x.CanTransform("Blah", typeof(string), true, It.IsAny<IEnumerable<Attribute>>()))
                  .Returns(true);
                xf.Setup(x => x.Transform("Blah", typeof(string), true, It.IsAny<IEnumerable<Attribute>>()))
                  .Returns("foobar");

                var toTest = new ExpandoObjectRowFactory<dynamic>();
                var res    = toTest.ParseRows(rdr.Object,
                                              new IDataTransformer[] { xf.Object },
                                              CancellationToken.None);
                
                var item = res.Should().ContainSingle("because only 1 row should have been returned").Which;
                ((string)item.Name).Should().Be("foobar", "because it should have been transformed");
            }

            [TestMethod]
            public void GlobalIDataTransformer_TransformsData()
            {
                var rdr = new Mock<IDataReader>();
                rdr.Setup(r => r.FieldCount).Returns(1);
                rdr.Setup(r => r.GetFieldType(0)).Returns(typeof(string));
                rdr.Setup(r => r.GetName(0)).Returns("Name");
                rdr.Setup(r => r.GetValues(It.IsAny<object[]>()))
                   .Callback<object[]>(os => os[0] = "Blah");
                rdr.SetupSequence(r => r.Read())
                   .Returns(true)
                   .Returns(false);

                var xf = new Mock<IDataTransformer>();
                xf.Setup(x => x.CanTransform("Blah", typeof(string), true, It.IsAny<IEnumerable<Attribute>>()))
                  .Returns(true);
                xf.Setup(x => x.Transform("Blah", typeof(string), true, It.IsAny<IEnumerable<Attribute>>()))
                  .Returns("foobar");

                using (GlobalSettings.UseTestInstance())
                {
                    StoredProcedure.AddGlobalTransformer(xf.Object);

                    var toTest = new ExpandoObjectRowFactory<dynamic>();
                    var res    = toTest.ParseRows(rdr.Object, new IDataTransformer[0], CancellationToken.None);

                    var item = res.Should().ContainSingle("because only 1 row should have been returned").Which;
                    ((string)item.Name).Should().Be("foobar", "because it should have been transformed");
                }
            }

            [TestMethod]
            public void IDataTransformers_CalledInOrder()
            {
                var rdr = new Mock<IDataReader>();
                rdr.Setup(r => r.FieldCount).Returns(1);
                rdr.Setup(r => r.GetFieldType(0)).Returns(typeof(string));
                rdr.Setup(r => r.GetName(0)).Returns("Name");
                rdr.Setup(r => r.GetValues(It.IsAny<object[]>()))
                   .Callback<object[]>(os => os[0] = "Blah");
                rdr.SetupSequence(r => r.Read())
                   .Returns(true)
                   .Returns(false);

                var xf1 = new Mock<IDataTransformer>();
                xf1.Setup(x => x.CanTransform("Blah", typeof(string), true, It.IsAny<IEnumerable<Attribute>>()))
                   .Returns(true);
                xf1.Setup(x => x.Transform("Blah", typeof(string), true, It.IsAny<IEnumerable<Attribute>>()))
                   .Returns("foo");
                var xf2 = new Mock<IDataTransformer>();
                xf2.Setup(x => x.CanTransform("foo", typeof(string), true, It.IsAny<IEnumerable<Attribute>>()))
                   .Returns(true);
                xf2.Setup(x => x.Transform("foo", typeof(string), true, It.IsAny<IEnumerable<Attribute>>()))
                   .Returns("bar");

                var toTest = new ExpandoObjectRowFactory<dynamic>();
                var res    = toTest.ParseRows(rdr.Object,
                                              new IDataTransformer[] { xf1.Object, xf2.Object },
                                              CancellationToken.None);

                var item = res.Should().ContainSingle("because only 1 row should have been returned").Which;
                ((string)item.Name).Should().Be("bar", "because it should have been transformed by both transformers");
            }

            [TestMethod]
            public void IDataTransformers_CalledForAppropriateColumns()
            {
                var rdr = new Mock<IDataReader>();
                rdr.Setup(r => r.FieldCount).Returns(2);
                rdr.Setup(r => r.GetFieldType(0)).Returns(typeof(string));
                rdr.Setup(r => r.GetName(0)).Returns("Name");
                rdr.Setup(r => r.GetFieldType(1)).Returns(typeof(int));
                rdr.Setup(r => r.GetName(1)).Returns("Age");
                rdr.Setup(r => r.GetValues(It.IsAny<object[]>()))
                   .Callback<object[]>(os => { os[0] = "Blah"; os[1] = 42; });
                rdr.SetupSequence(r => r.Read())
                   .Returns(true)
                   .Returns(false);

                var xf1 = new Mock<IDataTransformer>();
                xf1.Setup(x => x.CanTransform("Blah", typeof(string), true, It.IsAny<IEnumerable<Attribute>>()))
                   .Returns(true);
                xf1.Setup(x => x.Transform("Blah", typeof(string), true, It.IsAny<IEnumerable<Attribute>>()))
                   .Returns("foo");
                var xf2 = new Mock<IDataTransformer>();
                xf2.Setup(x => x.CanTransform(42, typeof(int), true, It.IsAny<IEnumerable<Attribute>>()))
                   .Returns(true);
                xf2.Setup(x => x.Transform(42, typeof(int), true, It.IsAny<IEnumerable<Attribute>>()))
                   .Returns(55);

                var toTest = new ExpandoObjectRowFactory<dynamic>();
                var res    = toTest.ParseRows(rdr.Object,
                                              new IDataTransformer[] { xf1.Object, xf2.Object },
                                              CancellationToken.None);

                var item = res.Should().ContainSingle("because only 1 row should have been returned").Which;
                ((string)item.Name).Should().Be("foo", "because it should have been transformed by the string transformer");
                ((int)item.Age).Should().Be(55, "because it should have been transformed by the int transformer");
            }

            [TestMethod]
            public void UsesProper_NullableValues_ForNullObject()
            {
                var rdr = new Mock<IDataReader>();
                rdr.Setup(r => r.FieldCount).Returns(1);
                rdr.Setup(r => r.GetFieldType(0)).Returns(typeof(int));
                rdr.Setup(r => r.GetName(0)).Returns("Id");
                rdr.Setup(r => r.IsDBNull(0)).Returns(true);
                rdr.Setup(r => r.GetValues(It.IsAny<object[]>()))
                   .Callback<object[]>(os => os[0] = DBNull.Value);
                rdr.SetupSequence(r => r.Read())
                   .Returns(true)
                   .Returns(false);

                var toTest = new ExpandoObjectRowFactory<dynamic>();
                var res = toTest.ParseRows(rdr.Object,
                                              new IDataTransformer[0],
                                              CancellationToken.None);

                var item = res.Should().ContainSingle("because only 1 row should have been returned").Which;
                int? asNullableInt = item.Id;
                asNullableInt.Should().NotHaveValue("because the returned item was null");
            }
        }

        [TestClass]
        public class MatchesColumns
        {
            [TestMethod]
            public void ReturnsTrueForAnyInput_WithZeroLeftoverColumns()
            {
                int leftover;
                var toTest = new ExpandoObjectRowFactory<dynamic>();
                var result = toTest.MatchesColumns(new[] { "String", "Double", "Decimal", "Int", "Long", "Date" }, out leftover);

                result.Should().BeTrue("because any and all columns will be matched");
                leftover.Should().Be(0, "because all columns get matched, none should be leftover");
            }
        }
    }
}
