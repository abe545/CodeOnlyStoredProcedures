using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using CodeOnlyStoredProcedure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

#if NET40
namespace CodeOnlyTests.Net40
#else
namespace CodeOnlyTests
#endif
{
    [TestClass]
    public class TypeExtensionsTests
    {
        #region GetMappedProperties
        [TestMethod]
        public void TestGetMappedProperties_GetsAllPublicProperties()
        {
            typeof(Model).GetMappedProperties()
                .Should().ContainInOrder(typeof(Model).GetProperties(), "all public properties should be mapped");
        }

        [TestMethod]
        public void TestGetMappedProperties_DoesNotIncludeNotMappedProperties()
        {
            typeof(ModelNotMapped).GetMappedProperties()
                .Should().ContainSingle(pi => pi.Name == "Bar", "Bar is the only mapped property");
        }

        [TestMethod]
        public void TestGetMappedProperties_DoesNotIncludeReadOnlyPropertiesWhenSpecified()
        {
            typeof(ModelReadOnlyProperty).GetMappedProperties()
                .Should().ContainSingle(pi => pi.Name == "Foo", "Foo is the only writeable property");
        }

        [TestMethod]
        public void TestGetMappedProperties_DoesNotIncludePropertiesWithPrivateSettersWhenRequiresWritableSpecified()
        {
            typeof(ModelPrivateSetter).GetMappedProperties()
                .Should().ContainSingle(pi => pi.Name == "Foo", "Foo is the only property with a public setter");
        }

        [TestMethod]
        public void TestGetMappedProperties_DoesNotIncludeWriteOnlyPropertiesWhenSpecified()
        {
            typeof(ModelWriteOnlyProperty).GetMappedProperties()
                .Should().ContainSingle(pi => pi.Name == "Foo", "Foo is the only readable property");
        }

        [TestMethod]
        public void TestGetMappedProperties_DoesNotIncludePropertiesWithPrivateGetterWhenRequireReadableSpecified()
        {
            typeof(ModelPrivateGetter).GetMappedProperties()
                .Should().ContainSingle(pi => pi.Name == "Foo", "Foo is the only property with a public getter");
        }

        [TestMethod]
        public void TestGetMappedProperties_GetsOptionalProperties()
        {
            typeof(WithOptionalProperty).GetMappedProperties()
                .Should().ContainInOrder(typeof(WithOptionalProperty).GetProperties(), "optional properties should have no effect");
        }
        #endregion

        #region GetResultPropertiesBySqlName
        [TestMethod]
        public void TestGetResultPropertiesBySqlName_GetsAllPublicPropertiesByName()
        {
            var props = typeof(Model).GetResultPropertiesBySqlName();

            props.Should().HaveCount(2, "because Model has 2 public properties.").And
                 .ContainKey("Foo", "because it is a public property.").And
                 .ContainKey("Bar", "because it is a public property.");
        }

        [TestMethod]
        public void TestGetResultPropertiesBySqlName_DoesNotIncludeNotMappedProperties()
        {
            var props = typeof(ModelNotMapped).GetResultPropertiesBySqlName();

            props.Should().HaveCount(1, "because ModelNotMapped has 1 mapped public property.").And
                 .NotContainKey("Foo", "because it is a public property marked with the NotMapped attribute.").And
                 .ContainKey("Bar", "because it is a public property.");
        }

        [TestMethod]
        public void TestGetResultPropertiesBySqlName_DoesNotIncludeReadOnlyProperties()
        {
            var props = typeof(ModelReadOnlyProperty).GetResultPropertiesBySqlName();

            props.Should().HaveCount(1, "because ModelReadOnlyProperty has 1 writable public property.").And
                 .ContainKey("Foo", "because it is a public writeable property.").And
                 .NotContainKey("Bar", "because it is a public read-only property.");
        }
        
        [TestMethod]
        public void TestGetResultPropertiesBySqlName_RenamesViaAllAttributes()
        {
            var props = typeof(RenamedProperties).GetResultPropertiesBySqlName();

            props.Should().HaveCount(3, "because Model has 3 renamed public properties.").And
                 .ContainKey("One", "because it is a renamed public property.").And
                 .ContainKey("Two", "because it is a renamed public property.").And
                 .ContainKey("Three", "because it is a renamed public property.");

            props["One"].Name.Should().Be("Foo", "because it is the name of the property that was renamed via ColumnAttribute.");
            props["Two"].Name.Should().Be("Bar", "because it is the name of the property that was renamed via TableValuedParameterAttribute.");
            props["Three"].Name.Should().Be("Baz", "because it is the name of the property that was renamed via StoredProcedureParameterAttribute.");
        }
        #endregion

        #region IsValidResultType
        [TestMethod]
        public void IsValidResultType_ReturnsTrueForString()
        {
            typeof(String).IsValidResultType().Should().BeTrue("because String is an integral type");
        }

        [TestMethod]
        public void IsValidResultType_ReturnsTrueForChar()
        {
            typeof(Char).IsValidResultType().Should().BeTrue("because Char is an integral type");
        }

        [TestMethod]
        public void IsValidResultType_ReturnsTrueForInt32()
        {
            typeof(Int32).IsValidResultType().Should().BeTrue("because Int32 is an integral type");
        }

        [TestMethod]
        public void IsValidResultType_ReturnsTrueForInt64()
        {
            typeof(Int64).IsValidResultType().Should().BeTrue("because Int64 is an integral type");
        }

        [TestMethod]
        public void IsValidResultType_ReturnsTrueForInt16()
        {
            typeof(Int16).IsValidResultType().Should().BeTrue("because Int16 is an integral type");
        }

        [TestMethod]
        public void IsValidResultType_ReturnsTrueForDouble()
        {
            typeof(Double).IsValidResultType().Should().BeTrue("because Double is an integral type");
        }

        [TestMethod]
        public void IsValidResultType_ReturnsTrueForSingle()
        {
            typeof(Single).IsValidResultType().Should().BeTrue("because Single is an integral type");
        }

        [TestMethod]
        public void IsValidResultType_ReturnsTrueForDecimal()
        {
            typeof(Decimal).IsValidResultType().Should().BeTrue("because Decimal is an integral type");
        }

        [TestMethod]
        public void IsValidResultType_ReturnsTrueForBoolean()
        {
            typeof(Boolean).IsValidResultType().Should().BeTrue("because Boolean is an integral type");
        }
        
        [TestMethod]
        public void IsValidResultType_ReturnsTrueForDateTime()
        {
            typeof(DateTime).IsValidResultType().Should().BeTrue("because DateTime is an integral type");
        }

        [TestMethod]
        public void IsValidResultType_ReturnsTrueForGuid()
        {
            typeof(Guid).IsValidResultType().Should().BeTrue("because Guid is an integral type");
        }

        [TestMethod]
        public void IsValidResultType_ReturnsTrueForNullableChar()
        {
            typeof(Char?).IsValidResultType().Should().BeTrue("because nullable Char is an integral type");
        }

        [TestMethod]
        public void IsValidResultType_ReturnsTrueForNullableInt32()
        {
            typeof(Int32?).IsValidResultType().Should().BeTrue("because nullable Int32 is an integral type");
        }

        [TestMethod]
        public void IsValidResultType_ReturnsTrueForNullableInt64()
        {
            typeof(Int64?).IsValidResultType().Should().BeTrue("because nullable Int64 is an integral type");
        }

        [TestMethod]
        public void IsValidResultType_ReturnsTrueForNullableInt16()
        {
            typeof(Int16?).IsValidResultType().Should().BeTrue("because nullable Int16 is an integral type");
        }

        [TestMethod]
        public void IsValidResultType_ReturnsTrueForNullableDouble()
        {
            typeof(Double?).IsValidResultType().Should().BeTrue("because nullable Double is an integral type");
        }

        [TestMethod]
        public void IsValidResultType_ReturnsTrueForNullableSingle()
        {
            typeof(Single?).IsValidResultType().Should().BeTrue("because nullable Single is an integral type");
        }

        [TestMethod]
        public void IsValidResultType_ReturnsTrueForNullableDecimal()
        {
            typeof(Decimal?).IsValidResultType().Should().BeTrue("because nullable Decimal is an integral type");
        }

        [TestMethod]
        public void IsValidResultType_ReturnsTrueForNullableBoolean()
        {
            typeof(Boolean?).IsValidResultType().Should().BeTrue("because nullable Boolean is an integral type");
        }

        [TestMethod]
        public void IsValidResultType_ReturnsTrueForNullableDateTime()
        {
            typeof(DateTime?).IsValidResultType().Should().BeTrue("because nullable DateTime is an integral type");
        }

        [TestMethod]
        public void IsValidResultType_ReturnsTrueForNullableGuid()
        {
            typeof(Guid?).IsValidResultType().Should().BeTrue("because nullable Guid is an integral type");
        }

        [TestMethod]
        public void IsValidResultType_ReturnsFalseForUnmappedInterface()
        {
            lock (CodeOnlyStoredProcedure.TypeExtensions.interfaceMap)
            {
                // make sure that there is nothing in the map
                CodeOnlyStoredProcedure.TypeExtensions.interfaceMap.Clear();
                typeof(IModel).IsValidResultType().Should().BeFalse("because unmapped interfaces can not be constructed");
            }
        }

        [TestMethod]
        public void IsValidResultType_ReturnsTrueForMappedInterface()
        {
            lock (CodeOnlyStoredProcedure.TypeExtensions.interfaceMap)
            {
                // make sure that there is nothing in the map
                CodeOnlyStoredProcedure.TypeExtensions.interfaceMap.Clear();
                CodeOnlyStoredProcedure.TypeExtensions.interfaceMap.TryAdd(typeof(IModel), typeof(Model));

                typeof(IModel).IsValidResultType().Should().BeTrue("because mapped interfaces can be constructed");
            }
        }
        #endregion

        #region Types To Test With
        private interface IModel
        {
            string Foo { get; set; }
            int    Bar { get; set; }
        }

        private class Model : IModel
        {
            public  string Foo { get; set; }
            public  int    Bar { get; set; }
            private double Baz { get; set; }
        }

        private class ModelNotMapped
        {
            [NotMapped]
            public string Foo { get; set; }
            public double Bar { get; set; }
        }

        private class ModelReadOnlyProperty
        {
            public string Foo { get; set; }
            public double Bar { get { return 42.0; } }
        }

        private class ModelPrivateSetter
        {
            public string Foo { get; set; }
            public double Bar { get; private set; }
        }

        private class ModelWriteOnlyProperty
        {
            public string Foo { get; set; }
            public double Bar { set { } }
        }

        private class ModelPrivateGetter
        {
            public string Foo { get; set; }
            public double Bar { private get; set; }
        } 

        private class RenamedProperties
        {
            [Column("One")]
            public string Foo { get; set; }
            [TableValuedParameter(Name = "Two")]
            public string Bar { get; set; }
            [StoredProcedureParameter(Name = "Three")]
            public string Baz { get; set; }
        }

        private class WithOptionalProperty
        {
            [OptionalResult]
            public string Foo { get; set; }
            public string Bar { get; set; }
        }
        #endregion
    }
}
