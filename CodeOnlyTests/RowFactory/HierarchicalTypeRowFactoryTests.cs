using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
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
    public class HierarchicalTypeRowFactoryTests
    {
        [TestClass]
        public class Parse
        {
            [TestMethod]
            public void CancelsWhenTokenCanceledBeforeExecuting()
            {
                var reader = new Mock<IDataReader>();

                var cts = new CancellationTokenSource();
                cts.Cancel();

                var toTest = new HierarchicalTypeRowFactory<SolarSystem>();
                toTest.Invoking(f => f.ParseRows(reader.Object, Enumerable.Empty<IDataTransformer>(), cts.Token))
                      .ShouldThrow<OperationCanceledException>("the operation was cancelled");

                reader.Verify(d => d.Read(), Times.Never);
            }

            [TestMethod]
            public void CanBuildHierarchyMethodsForMappedInterfaces()
            {
                using (GlobalSettings.UseTestInstance())
                {
                    GlobalSettings.Instance.InterfaceMap.TryAdd(typeof(ILevel1), typeof(Level1));
                    GlobalSettings.Instance.InterfaceMap.TryAdd(typeof(ILevel2), typeof(Level2));
                    GlobalSettings.Instance.InterfaceMap.TryAdd(typeof(ILevel3), typeof(Level3));
                    GlobalSettings.Instance.InterfaceMap.TryAdd(typeof(ILevel4), typeof(Level4));

                    this.Invoking(_ => new HierarchicalTypeRowFactory<ILevel1>())
                        .ShouldNotThrow();
                }
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

                var toTest = new HierarchicalTypeRowFactory<SolarSystem>();
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
                var reader = SetupDataReader(
                    new Dictionary<string, object>
                    {
                        { "Id", 0 },
                        { "Galaxy_Id", 0 },
                        { "Name", "Sol" }
                    },
                    new Dictionary<string, object>
                    {
                        { "Id", 3 },
                        { "SolarSystemId", 0 },
                        { "Name", "Earth" }
                    });

                var toTest = new HierarchicalTypeRowFactory<SolarSystem>();
                var res = toTest.ParseRows(reader, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);

                res.Should().ContainSingle("because only one row was setup").Which
                    .ShouldBeEquivalentTo(new SolarSystem
                    {
                        Id = 0,
                        Galaxy_Id = 0,
                        Name = "Sol",
                        Planets = new[]
                        { 
                            new Planet
                            {
                                Id = 3,
                                SolarSystemId = 0,
                                Name = "Earth"
                            }   
                        }
                    });
            }

            [TestMethod]
            public void ChildrenResultsReturnedBeforeParents_AssociatesChildren()
            {
                var reader = SetupDataReader(
                    new Dictionary<string, object>
                    {
                        { "Id", 4 },
                        { "SolarSystemId", 1 },
                        { "Name", "Mars" }
                    },
                    new Dictionary<string, object>
                    {
                        { "Id", 1 },
                        { "Galaxy_Id", 0 },
                        { "Name", "Sol" }
                    });

                var toTest = new HierarchicalTypeRowFactory<SolarSystem>();
                var res = toTest.ParseRows(reader, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);

                res.Should().ContainSingle("because only one row was setup").Which
                    .ShouldBeEquivalentTo(new SolarSystem
                    {
                        Id = 1,
                        Galaxy_Id = 0,
                        Name = "Sol",
                        Planets = new[]
                        { 
                            new Planet
                            {
                                Id = 4,
                                SolarSystemId = 1,
                                Name = "Mars"
                            }   
                        }
                    });
            }

            [TestMethod]
            public void MultipleHierarchyWithCustomKeyAndForeignKey_AssociatesChildren()
            {
                var reader = SetupDataReader(
                    new Dictionary<string, object>
                    {
                        { "Id", 0 },
                        { "Galaxy_Id", 0 },
                        { "Name", "Sol" }
                    },
                    new Dictionary<string, object>
                    {
                        { "Galaxy_Id", 0 },
                        { "Name", "Milky Way" }
                    },
                    new Dictionary<string, object>
                    {
                        { "Id", 3 },
                        { "SolarSystemId", 0 },
                        { "Name", "Earth" }
                    });

                var toTest = new HierarchicalTypeRowFactory<Galaxy>();
                var res = toTest.ParseRows(reader, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);

                res.Should().ContainSingle("because only one row was setup").Which
                    .ShouldBeEquivalentTo(
                    new Galaxy
                    {
                        GalaxyId = 0,
                        Name = "Milky Way",
                        SolarSystems = new[] 
                        {
                            new SolarSystem
                            {
                                Id = 0,
                                Galaxy_Id = 0,
                                Name = "Sol",
                                Planets = new[]
                                { 
                                    new Planet
                                    {
                                        Id = 3,
                                        SolarSystemId = 0,
                                        Name = "Earth"
                                    }   
                                }
                            }
                        }
                    });
            }

            [TestMethod]
            public void MultipleHierarchyWithRenamedParentKey_AssociatesChildren()
            {
                var reader = SetupDataReader(
                    new Dictionary<string, object>
                    {
                        { "Id", 0 },
                        { "Galaxy_Id", 0 },
                        { "Name", "Sol" }
                    },
                    new Dictionary<string, object>
                    {
                        { "Galaxy_Id", 0 },
                        { "Universe_Id", 0 },
                        { "Name", "Milky Way" }
                    },
                    new Dictionary<string, object>
                    {
                        { "Id", 3 },
                        { "SolarSystemId", 0 },
                        { "Name", "Earth" }
                    },
                    new Dictionary<string, object>
                    {
                        { "UniverseId", 0 },
                        { "Name", "The Known" }
                    });

                var toTest = new HierarchicalTypeRowFactory<Universe>();
                var res = toTest.ParseRows(reader, Enumerable.Empty<IDataTransformer>(), CancellationToken.None);

                res.Should().ContainSingle("because only one row was setup").Which
                    .ShouldBeEquivalentTo(
                    new Universe
                    {
                        Name = "The Known",
                        UniverseId = 0,
                        Galaxies = new[]
                        {
                            new Galaxy
                            {
                                GalaxyId = 0,
                                UniverseId = 0,
                                Name = "Milky Way",
                                SolarSystems = new[] 
                                {
                                    new SolarSystem
                                    {
                                        Id = 0,
                                        Galaxy_Id = 0,
                                        Name = "Sol",
                                        Planets = new[]
                                        { 
                                            new Planet
                                            {
                                                Id = 3,
                                                SolarSystemId = 0,
                                                Name = "Earth"
                                            }   
                                        }
                                    }
                                }
                            }
                        }
                    });
            }

            [TestMethod]
            public void MultipleHierarchyWithRenamedParentKey_WithDataTransformers_AssociatesChildren()
            {
                var reader = SetupDataReader(
                    new Dictionary<string, object>
                    {
                        { "Id", 0 },
                        { "Galaxy_Id", 0 },
                        { "Name", "Sol" }
                    },
                    new Dictionary<string, object>
                    {
                        { "Galaxy_Id", 0 },
                        { "Universe_Id", 0 },
                        { "Name", "Milky Way" }
                    },
                    new Dictionary<string, object>
                    {
                        { "Id", 3 },
                        { "SolarSystemId", 0 },
                        { "Name", "Earth" }
                    },
                    new Dictionary<string, object>
                    {
                        { "UniverseId", 0 },
                        { "Name", "The Known" }
                    });

                var toTest = new HierarchicalTypeRowFactory<Universe>();
                var res = toTest.ParseRows(reader, new[] { Mock.Of<IDataTransformer>() }, CancellationToken.None);

                res.Should().ContainSingle("because only one row was setup").Which
                    .ShouldBeEquivalentTo(
                    new Universe
                    {
                        Name = "The Known",
                        UniverseId = 0,
                        Galaxies = new[]
                        {
                            new Galaxy
                            {
                                GalaxyId = 0,
                                UniverseId = 0,
                                Name = "Milky Way",
                                SolarSystems = new[] 
                                {
                                    new SolarSystem
                                    {
                                        Id = 0,
                                        Galaxy_Id = 0,
                                        Name = "Sol",
                                        Planets = new[]
                                        { 
                                            new Planet
                                            {
                                                Id = 3,
                                                SolarSystemId = 0,
                                                Name = "Earth"
                                            }   
                                        }
                                    }
                                }
                            }
                        }
                    });
            }

            [TestMethod]
            public void OrderSpecifiedInCtor_IsUsedToParse()
            {
                var reader = SetupDataReader(
                    new Dictionary<string, object>
                    {
                        { "Key", 0 },
                        { "OtherKey", 1 }
                    },
                    new Dictionary<string, object>
                    {
                        { "Key", 1 },
                        { "OtherKey", 0 }
                    });

                var toTest = new HierarchicalTypeRowFactory<Foo>(new Type[] { typeof(Foo), typeof(Bar) });
                var res    = toTest.ParseRows(reader, new IDataTransformer[0], CancellationToken.None);

                res.Should().ContainSingle("because only one row was setup").Which
                    .ShouldBeEquivalentTo(
                        new Foo
                        {
                            Key = 0,
                            OtherKey = 1,
                            Bars = new Bar[]
                            {
                                new Bar
                                {
                                    Key = 1,
                                    OtherKey = 0
                                }
                            }
                        });

                reader = SetupDataReader(
                    new Dictionary<string, object>
                    {
                        { "Key", 2 },
                        { "OtherKey", 3 }
                    },
                    new Dictionary<string, object>
                    {
                        { "Key", 3 },
                        { "OtherKey", 2 }
                    });
                toTest = new HierarchicalTypeRowFactory<Foo>(new Type[] { typeof(Bar), typeof(Foo) });
                res    = toTest.ParseRows(reader, new IDataTransformer[0], CancellationToken.None);

                res.Should().ContainSingle("because only one row was setup").Which
                    .ShouldBeEquivalentTo(
                        new Foo
                        {
                            Key = 3,
                            OtherKey = 2,
                            Bars = new Bar[]
                            {
                                new Bar
                                {
                                    Key = 2,
                                    OtherKey = 3
                                }
                            }
                        });
            }

            [TestMethod]
            public void CompoundKeys_CanStillBeUsedToGenerateHierarchy()
            {
                var reader = SetupDataReader(
                    new Dictionary<string, object>
                    {
                        { "Age", 42 },
                        { "Name", "The Answer" }
                    },
                    new Dictionary<string, object>
                    {
                        { "Age", 16 },
                        { "Name", "Candles" },
                        { "ParentAge", 42 },
                        { "ParentName", "The Answer" }
                    });

                var toTest = new HierarchicalTypeRowFactory<UnmappedKeyParent>();
                var res    = toTest.ParseRows(reader, new IDataTransformer[0], CancellationToken.None);

                res.Should().ContainSingle("because only one row was setup").Which
                    .ShouldBeEquivalentTo(
                        new UnmappedKeyParent
                        {
                            Name = "The Answer",
                            Age  = 42,
                            Children = new[]
                            {
                                new UnmappedKeyChild
                                {
                                    Name       = "Candles",
                                    Age        = 16,
                                    ParentName = "The Answer",
                                    ParentAge  = 42
                                }
                            }
                        });
            }

            [TestMethod]
            public void OptionalChildren_AreAdded_When_They_Are_Returned()
            {
                var reader = SetupDataReader(
                    new Dictionary<string, object>
                    {
                        { "Id", 42 }
                    },
                    new Dictionary<string, object>
                    {
                        { "ParentId", 42 },
                        { "Value", "Foo" }
                    });

                var toTest = new HierarchicalTypeRowFactory<OptionalChildren>();
                var res = toTest.ParseRows(reader, new IDataTransformer[0], CancellationToken.None);

                res.Should().ContainSingle("because only one row was setup").Which
                    .ShouldBeEquivalentTo(
                        new OptionalChildren
                        {
                            Id = 42,
                            Children = new[]
                            {
                                new Level4
                                {
                                    ParentId = 42,
                                    Value = "Foo"
                                }
                            }
                        });
            }

            [TestMethod]
            public void OptionalChildren_IsSetToEmptySet_When_They_Are_Not_Returned()
            {
                var reader = SetupDataReader(
                    new Dictionary<string, object>
                    {
                        { "Id", 42 }
                    });

                var toTest = new HierarchicalTypeRowFactory<OptionalChildren>();
                var res = toTest.ParseRows(reader, new IDataTransformer[0], CancellationToken.None);

                res.Should().ContainSingle("because only one row was setup").Which
                    .ShouldBeEquivalentTo(
                        new OptionalChildren
                        {
                            Id = 42,
                            Children = new Level4[0]
                        });
            }

            [TestMethod]
            public void OptionalChildren_With_Same_Type_As_Required_Children_But_Different_Keys_Are_Both_Returned()
            {
                var reader = SetupDataReader(
                    new Dictionary<string, object>
                    {
                        { "Id", 42 }
                    },
                    new Dictionary<string, object>
                    {
                        { "ParentId", 42 },
                        { "Value", "Foo" }
                    },
                    new Dictionary<string, object>
                    {
                        { "CousinId", 42 },
                        { "Value", "Bar" }
                    });

                var toTest = new HierarchicalTypeRowFactory<OptionalCousins>();
                var res = toTest.ParseRows(reader, new IDataTransformer[0], CancellationToken.None);

                res.Should().ContainSingle("because only one row was setup").Which
                    .ShouldBeEquivalentTo(
                        new OptionalCousins
                        {
                            Id = 42,
                            Children = new[]
                            {
                                new Relative
                                {
                                    ParentId = 42,
                                    Value = "Foo"
                                }
                            },
                            Cousins = new[]
                            {
                                new Relative
                                {
                                    CousinId = 42,
                                    Value = "Bar"
                                }
                            }
                        });
            }

            [TestMethod]
            public void OptionalChildren_AreNot_Chosen_Over_Required_Children()
            {
                var reader = SetupDataReader(
                    new Dictionary<string, object>
                    {
                        { "Id", 42 }
                    },
                    new Dictionary<string, object>
                    {
                        { "ParentId", 42 },
                        { "Value", "Foo" }
                    });

                var toTest = new HierarchicalTypeRowFactory<OptionalCousins>();
                var res = toTest.ParseRows(reader, new IDataTransformer[0], CancellationToken.None);

                res.Should().ContainSingle("because only one row was setup").Which
                    .ShouldBeEquivalentTo(
                        new OptionalCousins
                        {
                            Id = 42,
                            Children = new[]
                            {
                                new Relative
                                {
                                    ParentId = 42,
                                    Value = "Foo"
                                }
                            },
                            Cousins = new Relative[0]
                        });
            }

            [TestMethod]
            public void OptionalChildren_Still_Throws_When_Required_Children_Are_Not_Returned()
            {
                var reader = SetupDataReader(
                    new Dictionary<string, object>
                    {
                        { "Id", 42 }
                    });

                var toTest = new HierarchicalTypeRowFactory<OptionalChildren>();
                toTest.Invoking(f => f.ParseRows(reader, new IDataTransformer[0], CancellationToken.None))
                      .ShouldThrow<StoredProcedureException>("because a required result set was missing");
            }

            [TestMethod]
            public void RequiredChildren_Of_OptionalChildren_Set_If_All_Result_Sets_Returned()
            {
                using (GlobalSettings.UseTestInstance())
                {
                    GlobalSettings.Instance.InterfaceMap.TryAdd(typeof(ILevel4), typeof(Level4));

                    var reader = SetupDataReader(
                    new Dictionary<string, object>
                    {
                        { "Id", 42 }
                    },
                    new Dictionary<string, object>
                    {
                        { "Level2Id", 42 },
                        { "Name", 15 }
                    },
                    new Dictionary<string, object>
                    {
                        { "ParentId", 15 },
                        { "Value", "Bar" }
                    });

                    var toTest = new HierarchicalTypeRowFactory<OptionalRequired>();
                    var res = toTest.ParseRows(reader, new IDataTransformer[0], CancellationToken.None);

                    res.Should().ContainSingle("because only one row was setup").Which
                        .ShouldBeEquivalentTo(
                            new OptionalRequired
                            {
                                Id = 42,
                                Children = new List<Level3>
                                {
                                new Level3
                                {
                                    Level2Id = 42,
                                    Name = 15,
                                    Level4s = new[]
                                    {
                                        new Level4
                                        {
                                            ParentId = 15,
                                            Value = "Bar"
                                        }
                                    }
                                }
                                }
                            });
                }
            }

            [TestMethod]
            public void DoesNotThrow_If_OptionalChildren_And_RequiredChildren_Of_OptionalChildren_NotReturned()
            {
                using (GlobalSettings.UseTestInstance())
                {
                    GlobalSettings.Instance.InterfaceMap.TryAdd(typeof(ILevel4), typeof(Level4));
                    var reader = SetupDataReader(
                    new Dictionary<string, object>
                    {
                        { "Id", 42 }
                    });

                    var toTest = new HierarchicalTypeRowFactory<OptionalRequired>();
                    var res = toTest.ParseRows(reader, new IDataTransformer[0], CancellationToken.None);

                    res.Should().ContainSingle("because only one row was setup").Which
                        .ShouldBeEquivalentTo(
                            new OptionalRequired
                            {
                                Id = 42,
                                Children = new List<Level3>()
                            });
                }
            }

            [TestMethod]
            public void Throws_If_OptionalChildren_Returned_But_RequiredChildren_Of_OptionalChildren_AreNot()
            {
                using (GlobalSettings.UseTestInstance())
                {
                    GlobalSettings.Instance.InterfaceMap.TryAdd(typeof(ILevel4), typeof(Level4));

                    var reader = SetupDataReader(
                    new Dictionary<string, object>
                    {
                        { "Id", 42 }
                    },
                    new Dictionary<string, object>
                    {
                        { "Level2Id", 42 },
                        { "Name", 15 }
                    });

                    var toTest = new HierarchicalTypeRowFactory<OptionalRequired>();
                    toTest.Invoking(t => t.ParseRows(reader, new IDataTransformer[0], CancellationToken.None))
                          .ShouldThrow<StoredProcedureException>("because a required result set was missing");
                }
            }
        }

        private static IDataReader SetupDataReader(params Dictionary<string, object>[] values)
        {
            Contract.Requires(values.Length > 0);

            var reader = new Mock<IDataReader>();
            var index = 0;

            SetupDataReader(reader, values[0]);
            reader.Setup(r => r.NextResult())
                  .Returns(() =>
                  {
                      ++index;
                      if (index < values.Length)
                      {
                          SetupDataReader(reader, values[index]);
                          return true;
                      }

                      return false;
                  });

            return reader.Object;
        }

        private static void SetupDataReader(Mock<IDataReader> reader, Dictionary<string, object> values)
        {
            var keys  = values.Keys.OrderBy(s => s).ToList();
            var vals  = values.OrderBy(kv => kv.Key).Select(kv => kv.Value).ToArray();

            reader.SetupGet(r => r.FieldCount)
                  .Returns(() => keys.Count);
            reader.Setup(r => r.GetFieldType(It.IsAny<int>()))
                  .Returns((int i) => vals[i].GetType());
            reader.Setup(r => r.IsDBNull(It.IsAny<int>()))
                  .Returns((int i) => vals[i] == null);

            var isFirst = true;
            reader.Setup(r => r.Read())
                  .Returns(() =>
                  {
                      if (isFirst)
                      {
                          isFirst = false;
                          return true;
                      }

                      return false;
                  });

            reader.Setup(r => r.GetValue(It.IsAny<int>()))
                  .Returns((int i) => vals[i]);

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
        }

        private class Universe
        {
            public int UniverseId { get; set; }
            public string Name { get; set; }
            public IList<Galaxy> Galaxies { get; set; }
        }

        private class Galaxy
        {
            [Key, Column("Galaxy_Id")]
            public int GalaxyId { get; set; }
            [Column("Universe_Id"), OptionalResult]
            public int UniverseId { get; set; }
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

        private class Foo
        {
            [Key]
            public int Key { get; set; }
            public int OtherKey { get; set; }
            [ForeignKey("OtherKey")]
            public IEnumerable<Bar> Bars { get; set; }
        }

        private class Bar
        {
            [Key]
            public int Key { get; set; }
            public int OtherKey { get; set; }
        }

        public class UnmappedKeyParent
        {
            public string Id { get { return Name + Age; } }
            public string Name { get; set; }
            public int Age { get; set; }
            public IEnumerable<UnmappedKeyChild> Children { get; set; }
        }

        public class UnmappedKeyChild
        {
            public string UnmappedKeyParentId { get { return ParentName + ParentAge; } }
            public string ParentName { get; set; }
            public int ParentAge { get; set; }
            public string Name { get; set; }
            public int Age { get; set; }
        }

        public interface ILevel1
        {
            string Name { get; }
            IEnumerable<ILevel2> Level2s { get; }
        }

        public interface ILevel2
        {
            int Id { get; }
            string Name { get; }
            IEnumerable<ILevel3> Level3s { get; }
        }

        public interface ILevel3
        {
            [Key]
            int Name { get; }
            [ForeignKey("ParentId")]
            IEnumerable<ILevel4> Level4s { get; }
        }

        public interface ILevel4
        {
            int ParentId { get; }
            string Value { get; }
        }

        public class Level1 : ILevel1
        {
            [Key]
            public string Name { get; set; }
            [ForeignKey("Name")]
            public IEnumerable<ILevel2> Level2s { get; set; }
        }

        public class Level2 : ILevel2
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public IEnumerable<ILevel3> Level3s { get; set; }
        }

        public class Level3 : ILevel3
        {
            public int Name { get; set; }
            public int Level2Id { get; set; }
            public IEnumerable<ILevel4> Level4s { get; set; }
        }

        public class Level4 : ILevel4
        {
            public int ParentId { get; set; }
            public string Value { get; set; }
        }

        public class OptionalChildren
        {
            [Key]
            public int Id { get; set; }
            [OptionalResult, ForeignKey("ParentId")]
            public IEnumerable<Level4> Children { get; set; }
        }

        public class Relative
        {
            [OptionalResult]
            public int CousinId { get; set; }
            [OptionalResult]
            public int ParentId { get; set; }
            public string Value { get; set; }
        }

        public class OptionalCousins
        {
            [Key]
            public int Id { get; set; }
            [OptionalResult, ForeignKey("CousinId")]
            public IEnumerable<Relative> Cousins { get; set; }
            [ForeignKey("ParentId")]
            public IEnumerable<Relative> Children { get; set; }
        }

        public class OptionalRequired
        {
            [Key]
            public int Id { get; set; }
            [OptionalResult, ForeignKey("Level2Id")]
            public List<Level3> Children { get; set; }
        }
    }
}
