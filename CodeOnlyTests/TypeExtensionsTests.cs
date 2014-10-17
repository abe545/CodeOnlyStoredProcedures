using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using CodeOnlyStoredProcedure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
            var props = typeof(Model).GetMappedProperties().ToArray();

            CollectionAssert.AreEquivalent(typeof(Model).GetProperties(), props, "Not all public properties returned.");
        }

        [TestMethod]
        public void TestGetMappedProperties_DoesNotIncludeNotMappedProperties()
        {
            var prop = typeof(ModelNotMapped).GetMappedProperties().Single();

            Assert.AreEqual("Bar", prop.Name, "Wrong property returned.");
        }

        [TestMethod]
        public void TestGetMappedProperties_DoesNotIncludeReadOnlyPropertiesWhenSpecified()
        {
            var prop = typeof(ModelReadOnlyProperty).GetMappedProperties(requireWritable: true).Single();

            Assert.AreEqual("Foo", prop.Name, "Wrong property returned.");
        }

        [TestMethod]
        public void TestGetMappedProperties_DoesNotIncludeWriteOnlyPropertiesWhenSpecified()
        {
            var prop = typeof(ModelWriteOnlyProperty).GetMappedProperties(requireReadable: true).Single();

            Assert.AreEqual("Foo", prop.Name, "Wrong property returned.");
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
            lock (TypeExtensions.interfaceMap)
            {
                // make sure that there is nothing in the map
                TypeExtensions.interfaceMap.Clear();
                Assert.IsFalse(typeof(IModel).IsValidResultType());
            }
        }

        [TestMethod]
        public void IsValidResultType_ReturnsTrueForMappedInterface()
        {
            lock (TypeExtensions.interfaceMap)
            {
                // make sure that there is nothing in the map
                TypeExtensions.interfaceMap.Clear();
                TypeExtensions.interfaceMap.TryAdd(typeof(IModel), typeof(Model));

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

        private class ModelWriteOnlyProperty
        {
            public string Foo { get; set; }
            public double Bar { set { } }
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
        #endregion
    }
}
