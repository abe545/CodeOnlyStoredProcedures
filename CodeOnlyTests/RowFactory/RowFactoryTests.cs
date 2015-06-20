using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using CodeOnlyStoredProcedure;
using CodeOnlyStoredProcedure.RowFactory;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#if NET40
namespace CodeOnlyTests.Net40.RowFactory
#else
namespace CodeOnlyTests.RowFactory
#endif
{
    [TestClass]
    public class RowFactory
    {
        [TestClass]
        public class Create
        {
            [TestMethod]
            public void when_returning_single_string()
            {
                Assert<string, SimpleTypeRowFactory<string>>();
            }

            [TestMethod]
            public void when_returning_single_int()
            {
                Assert<int, SimpleTypeRowFactory<int>>();
            }

            [TestMethod]
            public void when_returning_single_nullable_double()
            {
                Assert<double?, SimpleTypeRowFactory<double?>>("double?");
            }

            [TestMethod]
            public void when_returning_single_enum()
            {
                Assert<Primes, EnumRowFactory<Primes>>();
            }

            [TestMethod]
            public void when_returning_single_nullable_enum()
            {
                Assert<Primes?, EnumRowFactory<Primes?>>("Primes?");
            }

            [TestMethod]
            public void when_returning_dynamic()
            {
                Assert<dynamic, ExpandoObjectRowFactory<dynamic>>("dynamic");
            }

            [TestMethod]
            public void when_returning_ComplexType()
            {
                Assert<ComplexType, ComplexTypeRowFactory<ComplexType>>();
            }

            [TestMethod]
            public void when_returning_HierarchicalType()
            {
                Assert<HierarchicalType, HierarchicalTypeRowFactory<HierarchicalType>>();
            }

            [TestMethod]
            public void when_returning_HierarchicalType_by_interface()
            {
                using (GlobalSettings.UseTestInstance())
                {
                    GlobalSettings.Instance.InterfaceMap.TryAdd(typeof(IHierarchy), typeof(HierarchicalType));
                    Assert<IHierarchy, HierarchicalTypeRowFactory<IHierarchy>>();
                }
            }

            [TestMethod]
            public void when_returning_ComplexType_by_interface_that_implements_hierarchy()
            {
                using (GlobalSettings.UseTestInstance())
                {
                    GlobalSettings.Instance.InterfaceMap.TryAdd(typeof(IHierarchy), typeof(ReadOnlyHierarchy));
                    Assert<IHierarchy, ComplexTypeRowFactory<IHierarchy>>();
                }
            }

            private static void Assert<T, TFactory>(string typeName = null)
                where TFactory : IRowFactory<T>
            {
                if (typeName == null)
                    typeName = typeof(T).Name;

                var result = RowFactory<T>.Create();

                var typeF = typeof(TFactory);
                var factoryName = typeF.Name.Substring(0, typeF.Name.Length - 2); // get rid of the `1

                result.Should().NotBeNull("result should not be null");
                result.RowType.Should().BeSameAs(typeof(T), "result should have RowType set to {0}", typeName);
                result.Should().BeOfType<TFactory>("result should be of type {0}<{1}>", factoryName, typeName);
            }
        }

        #region Private Helpers
        private enum Primes { One, Two, Three, Five, Seven }
        private class ComplexType
        {
            public int Id { get; set; }
            public int ParentId { get; set; }
            public string Name { get; set; }
        }
        private class HierarchicalType : IHierarchy
        {
            public int Id { get; set; }
            public string Name { get; set; }
            [ForeignKey("ParentId")]
            public IEnumerable<ComplexType> Children { get; set; }
        }

        private interface IHierarchy
        {
            IEnumerable<ComplexType> Children { get; set; }
        }

        private class ReadOnlyHierarchy : IHierarchy
        {
            public int Id { get; set; }
            public string Name { get; set; }

            IEnumerable<ComplexType> IHierarchy.Children
            {
                get { return null; }
                set { }
            }
        }
	    #endregion
    }
}
