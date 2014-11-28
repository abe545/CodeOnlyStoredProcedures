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

        #region GetRequiredPropertyNames
        [TestMethod]
        public void TestGetRequiredPropertyNames_GetsAllPropertyNames()
        {
            typeof(Model).GetRequiredPropertyNames()
                .Should().ContainInOrder(typeof(Model).GetProperties().Select(pi => pi.Name), "all public properties are required by default");
        }

        [TestMethod]
        public void TestGetRequiredPropertyNames_DoesNotIncludeNotMappedProperties()
        {
            typeof(ModelNotMapped).GetRequiredPropertyNames()
                .Should().ContainSingle(s => s == "Bar", "Bar is the only mapped property");
        }

        [TestMethod]
        public void TestGetRequiredPropertyNames_DoesNotIncludeReadOnlyPropertiesWhenSpecified()
        {
            typeof(ModelReadOnlyProperty).GetRequiredPropertyNames()
                .Should().ContainSingle(s => s == "Foo", "Foo is the only writeable property");
        }

        [TestMethod]
        public void TestGetRequiredPropertyNames_DoesNotIncludePropertiesWithPrivateSettersWhenRequiresWritableSpecified()
        {
            typeof(ModelPrivateSetter).GetRequiredPropertyNames()
                .Should().ContainSingle(s => s == "Foo", "Foo is the only property with a public setter");
        }

        [TestMethod]
        public void TestGetRequiredPropertyNames_DoesNotIncludeWriteOnlyPropertiesWhenSpecified()
        {
            typeof(ModelWriteOnlyProperty).GetRequiredPropertyNames()
                .Should().ContainSingle(s => s == "Foo", "Foo is the only readable property");
        }

        [TestMethod]
        public void TestGetRequiredPropertyNames_DoesNotIncludePropertiesWithPrivateGetterWhenRequireReadableSpecified()
        {
            typeof(ModelPrivateGetter).GetRequiredPropertyNames()
                .Should().ContainSingle(s => s == "Foo", "Foo is the only property with a public getter");
        }

        [TestMethod]
        public void TestGetRequiredPropertyNames_DoesNotIncludeOptionalProperties()
        {
            typeof(WithOptionalProperty).GetRequiredPropertyNames()
                .Should().ContainSingle(s => s == "Bar", "Foo is marked optional, so should not be included");
        }

        [TestMethod]
        public void TestGetRequiredPropertyNames_GetsTheRenamedProperties()
        {
            typeof(RenamedProperties).GetRequiredPropertyNames()
                .Should().Contain(new[] { "One", "Two", "Three" }, because: "the properties have been mapped to other columns in the result set");
        }
        #endregion

        #region GetResultPropertiesBySqlName
        [TestMethod]
        public void TestGetResultPropertiesBySqlName_GetsAllPublicPropertiesByName()
        {
            var props = typeof(Model).GetResultPropertiesBySqlName();

            Assert.AreEqual(2, props.Count, "More than the public properties returned.");
            Assert.IsTrue(props.ContainsKey("Foo"));
            Assert.IsTrue(props.ContainsKey("Bar"));
        }

        [TestMethod]
        public void TestGetResultPropertiesBySqlName_DoesNotIncludeNotMappedProperties()
        {
            var props = typeof(ModelNotMapped).GetResultPropertiesBySqlName();

            Assert.AreEqual(1, props.Count, "Not mapped property returned.");
            Assert.IsFalse(props.ContainsKey("Foo"), "Not mapped property returned.");
            Assert.IsTrue(props.ContainsKey("Bar"), "Mapped property not returned.");
        }

        [TestMethod]
        public void TestGetResultPropertiesBySqlName_DoesNotIncludeReadOnlyProperties()
        {
            var props = typeof(ModelReadOnlyProperty).GetResultPropertiesBySqlName();

            Assert.AreEqual(1, props.Count, "Read-only property returned.");
            Assert.IsFalse(props.ContainsKey("Bar"), "Read-only property returned.");
            Assert.IsTrue(props.ContainsKey("Foo"), "Writeable property not returned.");
        }
        
        [TestMethod]
        public void TestGetResultPropertiesBySqlName_RenamesViaAllAttributes()
        {
            var props = typeof(RenamedProperties).GetResultPropertiesBySqlName();

            PropertyInfo prop;

            Assert.IsTrue(props.TryGetValue("One", out prop), "Property not renamed via ColumnAttribute");
            Assert.AreEqual("Foo", prop.Name, "Wrong property returned for property renamed via ColumnAttribute");

            Assert.IsTrue(props.TryGetValue("Two", out prop), "Property not renamed via TableValuedParameterAttribute");
            Assert.AreEqual("Bar", prop.Name, "Wrong property returned for property renamed via TableValuedParameterAttribute");

            Assert.IsTrue(props.TryGetValue("Three", out prop), "Property not renamed via StoredProcedureParameterAttribute");
            Assert.AreEqual("Baz", prop.Name, "Wrong property returned for property renamed via StoredProcedureParameterAttribute");
        }
        #endregion

        #region IsValidResultType
        [TestMethod]
        public void IsValidResultType_ReturnsTrueForString()
        {
            Assert.IsTrue(typeof(string).IsValidResultType());
        }

        [TestMethod]
        public void IsValidResultType_ReturnsTrueForChar()
        {
            Assert.IsTrue(typeof(char).IsValidResultType());
        }

        [TestMethod]
        public void IsValidResultType_ReturnsTrueForInt32()
        {
            Assert.IsTrue(typeof(Int32).IsValidResultType());
        }

        [TestMethod]
        public void IsValidResultType_ReturnsTrueForInt64()
        {
            Assert.IsTrue(typeof(Int64).IsValidResultType());
        }

        [TestMethod]
        public void IsValidResultType_ReturnsTrueForInt16()
        {
            Assert.IsTrue(typeof(Int16).IsValidResultType());
        }

        [TestMethod]
        public void IsValidResultType_ReturnsTrueForDouble()
        {
            Assert.IsTrue(typeof(Double).IsValidResultType());
        }

        [TestMethod]
        public void IsValidResultType_ReturnsTrueForSingle()
        {
            Assert.IsTrue(typeof(Single).IsValidResultType());
        }

        [TestMethod]
        public void IsValidResultType_ReturnsTrueForDecimal()
        {
            Assert.IsTrue(typeof(Decimal).IsValidResultType());
        }

        [TestMethod]
        public void IsValidResultType_ReturnsTrueForBoolean()
        {
            Assert.IsTrue(typeof(Boolean).IsValidResultType());
        }
        
        [TestMethod]
        public void IsValidResultType_ReturnsTrueForDateTime()
        {
            Assert.IsTrue(typeof(DateTime).IsValidResultType());
        }

        [TestMethod]
        public void IsValidResultType_ReturnsTrueForGuid()
        {
            Assert.IsTrue(typeof(Guid).IsValidResultType());
        }

        [TestMethod]
        public void IsValidResultType_ReturnsFalseForUnmappedInterface()
        {
            lock (CodeOnlyStoredProcedure.TypeExtensions.interfaceMap)
            {
                // make sure that there is nothing in the map
                CodeOnlyStoredProcedure.TypeExtensions.interfaceMap.Clear();
                Assert.IsFalse(typeof(IModel).IsValidResultType());
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

                Assert.IsTrue(typeof(IModel).IsValidResultType());
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
