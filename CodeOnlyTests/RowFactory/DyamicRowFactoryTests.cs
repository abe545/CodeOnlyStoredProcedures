using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using Moq;
using CodeOnlyStoredProcedure;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using CodeOnlyStoredProcedure.DataTransformation;
using FluentAssertions;

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

            toTest.UnfoundPropertyNames.Should().Contain(new[] { "Name", "Price", "Rename" }, because: "no properties have been returned yet");
        }

        [TestMethod]
        public void CreateRow_ReturnsNewRow()
        {
            var toTest = new DynamicRowFactory<Row>()
                .CreateRow(new[] { "Name", "Price", "Rename" }, 
                           new object[] { "Foo", 13.3, "Bar" }, 
                           Enumerable.Empty<IDataTransformer>());

            toTest.Should().NotBeNull("one row should have been returned")
                  .And.BeOfType<Row>()
                  .And.Subject
                  .Should().Match<Row>(r => r.Name == "Foo", "Name column returned Foo")
                  .And     .Match<Row>(r => r.Price == 13.3, "Price column returned 13.3")
                  .And     .Match<Row>(r => r.Other == "Bar", "Other column returned Bar");
        }

        [TestMethod]
        public void CreateRow_SetsOptionalProperties()
        {
            var toTest = new DynamicRowFactory<Row>()
                .CreateRow(new[] { "Name", "Price", "Rename", "Optional" },
                           new object[] { "Foo", 13.3, "Bar", true },
                           Enumerable.Empty<IDataTransformer>());

            toTest.Should().NotBeNull("one row should have been returned")
                  .And.BeOfType<Row>()
                  .And.Subject
                  .Should().Match<Row>(r => r.Name == "Foo", "Name column returned Foo")
                  .And     .Match<Row>(r => r.Price == 13.3, "Price column returned 13.3")
                  .And     .Match<Row>(r => r.Other == "Bar", "Other column returned Bar")
                  .And     .Match<Row>(r => r.Optional, "Optional column returned true");
        }

        [TestMethod]
        public void CreateRow_WithoutPropertyInResultsSpecifiesThemInUnsetProperties()
        {
            var toTest = new DynamicRowFactory<Row>();

            toTest.CreateRow(new[] { "Name" },
                             new object[] { "Foo" },
                             Enumerable.Empty<IDataTransformer>());

            toTest.UnfoundPropertyNames.Should().Contain(new[] { "Price", "Rename" }, because: "name returned for first row.");
        }

        [TestMethod]
        public void CreateRow_DataConvertersUsed()
        {
            var toTest = new DynamicRowFactory<Row>()
                .CreateRow(new[] { "Name", "Price", "Rename" },
                           new object[] { "Foo       ", 42.0, "             Bar" },
                           new[] { new TrimAllStringsTransformer() });

            toTest.Should().NotBeNull("one row should have been returned")
                  .And.BeOfType<Row>()
                  .And.Subject
                  .Should().Match<Row>(r => r.Name == "Foo", "Name column returned Foo")
                  .And     .Match<Row>(r => r.Price == 42.0, "Price column returned 13.3")
                  .And     .Match<Row>(r => r.Other == "Bar", "Other column returned Bar");
        }

        [TestMethod]
        public void CreateRow_ExceptionsIncludeUsefulInformation()
        {
            var toTest = new DynamicRowFactory<Row>();

            toTest.Invoking(t => t.CreateRow(new[] { "Name", "Price", "Rename" },
                                             new object[] { "Foo", "Blah", "Bar" },
                                             Enumerable.Empty<IDataTransformer>()))
                  .ShouldThrow<StoredProcedureColumnException>("because the property result type does not match")
                  .WithMessage("Error setting [Double] Price property. Received value: \"Blah\".")
                  .WithInnerException<InvalidCastException>();
        }

        [TestMethod]
        public void CreateRow_ExceptionIncludesTypeForValueReturned()
        {
            var toTest = new DynamicRowFactory<Row>();

            toTest.Invoking(t => t.CreateRow(new[] { "Name", "Price", "Rename" },
                                             new object[] { "Foo", 42M, "Bar" },
                                             Enumerable.Empty<IDataTransformer>()))
                  .ShouldThrow<StoredProcedureColumnException>("because the property result type does not match")
                  .WithMessage("Error setting [Double] Price property. Received value: [Decimal] 42.")
                  .WithInnerException<InvalidCastException>();
        }

        private class Row
        {
            public string Name { get; set; }
            public double Price { get; set; }
            [Column("Rename")]
            public string Other { get; set; }
            [OptionalResult]
            public bool Optional { get; set; }
        }
    }
}
