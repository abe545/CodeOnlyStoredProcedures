using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Text;
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
    public class HierarchicalTypeRowFactoryTests
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

                var toTest = RowFactory<SolarSystem>.Create();
                toTest.Invoking(f => f.ParseRows(reader.Object, Enumerable.Empty<IDataTransformer>(), cts.Token))
                      .ShouldThrow<OperationCanceledException>("the operation was cancelled");

                reader.Verify(d => d.Read(), Times.Never);
            }

            [TestMethod]
            public void CancelsWhenTokenCanceled()
            {
                var sema   = new SemaphoreSlim(0, 1);
                var values = new Dictionary<string, object>
                {
                    { "Id", 0 },
                    { "Galaxy_Id", 0 },
                    { "Name", "Sol" }
                };

                var reader = new Mock<IDataReader>();
                SetupDataReader(reader, values);
                var execCount = 0;

                reader.Setup(d => d.Read())
                      .Callback(() =>
                      {
                          sema.Release();
                          Thread.Sleep(100);
                          ++execCount;
                      })
                      .Returns(() => execCount == 1);

                var cts = new CancellationTokenSource();

                var toTest = RowFactory<SolarSystem>.Create();
                var task = Task.Factory.StartNew(() => toTest.Invoking(f => f.ParseRows(reader.Object, Enumerable.Empty<IDataTransformer>(), cts.Token))
                                                             .ShouldThrow<OperationCanceledException>("the operation was cancelled"),
                                                 cts.Token);

                sema.Wait(TimeSpan.FromMilliseconds(250));
                cts.Cancel();

                task.Wait(TimeSpan.FromMilliseconds(250)).Should().BeTrue();
            }

            [TestMethod]
            public void ParentResultsReturnedBeforeChildren_AssociatesChildren()
            {

            }
        }

        private static void SetupDataReader(Mock<IDataReader> reader, params Dictionary<string, object>[] values)
        {
            var index = -1;
            var count = values.Length;
            var keys  = values.Select(v => v.Keys.OrderBy(s => s).ToList()).ToArray();
            var vals  = values.Select(v => v.OrderBy(kv => kv.Key).Select(kv => kv.Value).ToArray()).ToArray();

            reader.SetupGet(r => r.FieldCount)
                  .Returns(() => keys[index].Count);
            reader.Setup(r => r.GetFieldType(It.IsAny<int>()))
                  .Returns((int i) => vals[index][i].GetType());
            reader.Setup(r => r.GetOrdinal(It.IsAny<string>()))
                  .Returns((string s) => keys[index].IndexOf(s));
            reader.Setup(r => r.IsDBNull(It.IsAny<int>()))
                  .Returns((int i) => vals[index][i] == null);

            reader.Setup(r => r.Read())
                  .Returns(() =>
                  {
                      ++index;
                      return index < count;
                  });

            reader.Setup(r => r.GetValue(It.IsAny<int>()))
                  .Returns((int i) => vals[index][i]);

            reader.Setup(r => r.GetName(It.IsAny<int>()))
                  .Returns((int i) => keys[index][i]);
            reader.Setup(r => r.GetString(It.IsAny<int>()))
                  .Returns((int i) => (string)vals[index][i]);
            reader.Setup(r => r.GetInt16(It.IsAny<int>()))
                  .Returns((int i) => (short)vals[index][i]);
            reader.Setup(r => r.GetInt32(It.IsAny<int>()))
                  .Returns((int i) => (int)vals[index][i]);
            reader.Setup(r => r.GetInt64(It.IsAny<int>()))
                  .Returns((int i) => (long)vals[index][i]);
            reader.Setup(r => r.GetFloat(It.IsAny<int>()))
                  .Returns((int i) => (float)vals[index][i]);
            reader.Setup(r => r.GetDecimal(It.IsAny<int>()))
                  .Returns((int i) => (decimal)vals[index][i]);
            reader.Setup(r => r.GetDouble(It.IsAny<int>()))
                  .Returns((int i) => (double)vals[index][i]);
            reader.Setup(r => r.GetDateTime(It.IsAny<int>()))
                  .Returns((int i) => (DateTime)vals[index][i]);
            reader.Setup(r => r.GetChar(It.IsAny<int>()))
                  .Returns((int i) => (char)vals[index][i]);
            reader.Setup(r => r.GetByte(It.IsAny<int>()))
                  .Returns((int i) => (byte)vals[index][i]);
            reader.Setup(r => r.GetBoolean(It.IsAny<int>()))
                  .Returns((int i) => (bool)vals[index][i]);
        }

        private class Galaxy
        {
            [Key]
            public int GalaxyId { get; set; }
            public string Name { get; set; }
            [ForeignKey("Galaxy_Id")]
            public IEnumerable<SolarSystem> SolarSystems { get; set; }
        }

        private class SolarSystem
        {
            public int Id { get; set; }
            public int Galaxy_Id { get; set; }
            public string Name { get; set; }
            public Planet[] Planets { get; set; }
        }

        private class Planet
        {
            public int Id { get; set; }
            public int SolarSystemId { get; set; }
            public string Name { get; set; }
        }
    }
}
