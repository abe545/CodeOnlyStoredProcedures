using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using Moq;
using CodeOnlyStoredProcedure;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using CodeOnlyStoredProcedure.DataTransformation;

#if NET40
namespace CodeOnlyTests.Net40
#else
namespace CodeOnlyTests
#endif
{
    [TestClass]
    public class DynamicRowFactoryTests
    {
        [TestMethod]
        public void Ctor_SetsUnsetProperties()
        {
            var toTest = new DynamicRowFactory<Row>();

            CollectionAssert.AreEquivalent(new List<string> { "Name", "Price", "Rename" },
                                           toTest.UnfoundPropertyNames.ToList());
        }

        [TestMethod]
        public void CreateRow_ReturnsNewRow()
        {
            var toTest = new DynamicRowFactory<Row>();

            var res = toTest.CreateRow(new[] { "Name", "Price", "Rename" }, 
                                       new object[] { "Foo", 13.3, "Bar" }, 
                                       Enumerable.Empty<IDataTransformer>());

            Assert.IsNotNull(res, "Nothing returned from CreateRow");
            Assert.IsInstanceOfType(res, typeof(Row));

            var row = res as Row;
            Assert.AreEqual("Foo", row.Name,  "String property not set");
            Assert.AreEqual(13.3,  row.Price, "Double column not set");
            Assert.AreEqual("Bar", row.Other, "Renamed column not set");
        }

        [TestMethod]
        public void CreateRow_WithoutPropertyInResultsSpecifiesThemInUnsetProperties()
        {
            var toTest = new DynamicRowFactory<Row>();

            var res = toTest.CreateRow(new[] { "Name" },
                                       new object[] { "Foo" },
                                       Enumerable.Empty<IDataTransformer>());

            CollectionAssert.AreEquivalent(new List<string> { "Price", "Rename" },
                                           toTest.UnfoundPropertyNames.ToList());
        }

        [TestMethod]
        public void CreateRow_DataConvertersUsed()
        {
            var toTest = new DynamicRowFactory<Row>();

            var res = toTest.CreateRow(new[] { "Name", "Price", "Rename" },
                                       new object[] { "Foo       ", 42.0, "             Bar" },
                                       new[] { new TrimAllStringsTransformer() });

            Assert.IsNotNull(res, "Nothing returned from CreateRow");
            Assert.IsInstanceOfType(res, typeof(Row));

            var row = res as Row;
            Assert.AreEqual("Foo", row.Name,  "String property not set");
            Assert.AreEqual(42.0,  row.Price, "Double column not set");
            Assert.AreEqual("Bar", row.Other, "Renamed column not set");
        }

        [TestMethod]
        public void CreateRow_ExceptionsIncludeUsefulInformation()
        {
            var toTest = new DynamicRowFactory<Row>();

            try
            {
                var res = toTest.CreateRow(new[] { "Name", "Price", "Rename" },
                                           new object[] { "Foo", "Blah", "Bar" },
                                           Enumerable.Empty<IDataTransformer>());
            }
            catch (StoredProcedureColumnException ex)
            {
                Assert.AreEqual("Error setting [Double] Price property. Received value: \"Blah\".", ex.Message);
                Assert.IsInstanceOfType(ex.InnerException, typeof(InvalidCastException));
            }
        }

        [TestMethod]
        public void CreateRow_ExceptionIncludesTypeForValueReturned()
        {
            var toTest = new DynamicRowFactory<Row>();

            try
            {
                var res = toTest.CreateRow(new[] { "Name", "Price", "Rename" },
                                           new object[] { "Foo", 42M, "Bar" },
                                           Enumerable.Empty<IDataTransformer>());
            }
            catch (StoredProcedureColumnException ex)
            {
                Assert.AreEqual("Error setting [Double] Price property. Received value: [Decimal] 42.", ex.Message);
                Assert.IsInstanceOfType(ex.InnerException, typeof(InvalidCastException));
            }
        }

        private class Row
        {
            public string Name { get; set; }
            public double Price { get; set; }
            [Column("Rename")]
            public string Other { get; set; }
        }
    }
}
