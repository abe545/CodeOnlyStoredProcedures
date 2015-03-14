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
            public void TestExecuteReturnsDynamicResultOneRow()
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

                var toTest = RowFactory<dynamic>.Create();

                var res = toTest.ParseRows(reader.Object, new IDataTransformer[0], CancellationToken.None).ToList();

                Assert.AreEqual(1, res.Count);
                var item = res[0];

                ((string)  item.String) .Should().Be("Hello, World!",           "the String column has this value");
                ((double)  item.Double) .Should().Be(42.0,                      "the Double column has this value");
                ((decimal) item.Decimal).Should().Be(100M,                      "the Deciaml column has this value");
                ((int)     item.Int)    .Should().Be(99,                        "the Int column has this value");
                ((long)    item.Long)   .Should().Be(1028130L,                  "the Long column has this value");
                ((DateTime)item.Date)   .Should().Be(new DateTime(1982, 1, 31), "the Date column has this value");
            }
        }
    }
}
