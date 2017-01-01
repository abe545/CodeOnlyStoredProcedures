using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using CodeOnlyStoredProcedure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using System.Collections.Generic;
using System.Data;

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
        public void IsValidResultType_ReturnsTrueForDateTimeOffset()
        {
            typeof(DateTimeOffset).IsValidResultType().Should().BeTrue("because DateTimeOffset is an integral type");
        }

        [TestMethod]
        public void IsValidResultType_ReturnsTrueForTimespan()
        {
            typeof(TimeSpan).IsValidResultType().Should().BeTrue("because TimeSpan is an integral type");
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
        public void IsValidResultType_ReturnsTrueForNullableDateTimeOffset()
        {
            typeof(DateTimeOffset?).IsValidResultType().Should().BeTrue("because nullable DateTimeOffset is an integral type");
        }

        [TestMethod]
        public void IsValidResultType_ReturnsTrueForNullableTimespan()
        {
            typeof(TimeSpan?).IsValidResultType().Should().BeTrue("because nullable TimeSpan is an integral type");
        }

        [TestMethod]
        public void IsValidResultType_ReturnsTrueForNullableGuid()
        {
            typeof(Guid?).IsValidResultType().Should().BeTrue("because nullable Guid is an integral type");
        }

        [TestMethod]
        public void IsValidResultType_ReturnsFalseForUnmappedInterface()
        {
            using (GlobalSettings.UseTestInstance())
            {
                typeof(IModel).IsValidResultType().Should().BeFalse("because unmapped interfaces can not be constructed");
            }
        }

        [TestMethod]
        public void IsValidResultType_ReturnsTrueForMappedInterface()
        {
            using (GlobalSettings.UseTestInstance())
            {
                GlobalSettings.Instance.InterfaceMap.TryAdd(typeof(IModel), typeof(Model));

                typeof(IModel).IsValidResultType().Should().BeTrue("because mapped interfaces can be constructed");
            }
        }
        #endregion

        #region IsEnumeratedType
        [TestClass]
        public class IsEnumeratedType
        {
            [TestMethod]
            public void ReturnsTrueForValueArray()
            {
                typeof(int[]).IsEnumeratedType().Should().BeTrue("because arrays are enumerated types");
            }

            [TestMethod]
            public void ReturnsTrueForStringArray()
            {
                typeof(string[]).IsEnumeratedType().Should().BeTrue("because arrays are enumerated types");
            }

            [TestMethod]
            public void ReturnsTrueForInterfaceArray()
            {
                typeof(IModel[]).IsEnumeratedType().Should().BeTrue("because arrays are enumerated types");
            }

            [TestMethod]
            public void ReturnsTrueForClassArray()
            {
                typeof(Model[]).IsEnumeratedType().Should().BeTrue("because arrays are enumerated types");
            }

            [TestMethod]
            public void ReturnsTrueForValueEnumerable()
            {
                typeof(IEnumerable<int>).IsEnumeratedType().Should().BeTrue("because IEnumerable<T> is an enumerated type");
            }

            [TestMethod]
            public void ReturnsTrueForStringEnumerable()
            {
                typeof(IEnumerable<string>).IsEnumeratedType().Should().BeTrue("because IEnumerable<T> is an enumerated type");
            }

            [TestMethod]
            public void ReturnsTrueForInterfaceEnumerable()
            {
                typeof(IEnumerable<IModel>).IsEnumeratedType().Should().BeTrue("because IEnumerable<T> is an enumerated type");
            }

            [TestMethod]
            public void ReturnsTrueForClassEnumerable()
            {
                typeof(IEnumerable<Model>).IsEnumeratedType().Should().BeTrue("because IEnumerable<T> is an enumerated type");
            }

            [TestMethod]
            public void ReturnsTrueForValueList()
            {
                typeof(List<int>).IsEnumeratedType().Should().BeTrue("because List<T> is an enumerated type");
            }

            [TestMethod]
            public void ReturnsTrueForStringList()
            {
                typeof(List<string>).IsEnumeratedType().Should().BeTrue("because List<T> is an enumerated type");
            }

            [TestMethod]
            public void ReturnsTrueForInterfaceList()
            {
                typeof(List<IModel>).IsEnumeratedType().Should().BeTrue("because List<T> is an enumerated type");
            }

            [TestMethod]
            public void ReturnsTrueForClassList()
            {
                typeof(List<Model>).IsEnumeratedType().Should().BeTrue("because List<T> is an enumerated type");
            }

            [TestMethod]
            public void ReturnsFalseForString()
            {
                typeof(string).IsEnumeratedType().Should().BeFalse("because a string should not be considered an enumerated type");
            }

            [TestMethod]
            public void ReturnsFalseForValueType()
            {
                typeof(int).IsEnumeratedType().Should().BeFalse("because an int should not be considered an enumerated type");
            }
        }
        #endregion

        #region CreateTableValuedParameter
        [TestMethod]
        public void ThrowsWhenTypeIsString()
        {
            this.Invoking(_ =>
            {
                typeof(string).CreateTableValuedParameter("Foo", new[] { "Bar" });
            }).ShouldThrow<NotSupportedException>("because the string type should not be allowed to be used as TVPs")
                  .WithMessage("You can not use a string as a Table-Valued Parameter, since you really need to use a class with properties.",
                               "because the message should be helpful");
        }

        [TestMethod]
        public void ThrowsWhenTypeIsAnonymous()
        {
            this.Invoking(_ =>
            {
                var p1 = new { FirstName = "Bar" };
                p1.GetType().CreateTableValuedParameter("Foo", new[] { p1 });
            }).ShouldThrow<NotSupportedException>("because anonymous types should not be allowed to be used as TVPs")
                  .WithMessage("You can not use an anonymous type as a Table-Valued Parameter, since you really need to match the type name with something in the database.",
                               "because the message should be helpful");
        }

        [TestMethod]
        public void ReturnsATableValuedParameterForValidType()
        {
            this.Invoking(_ =>
            {
                var p1 = new Model
                {
                    Foo = "Foo",
                    Bar = 1
                };

                var res = p1.GetType().CreateTableValuedParameter("Bar", new[] { p1 });
                res.Should().BeOfType<TableValuedParameter>("because it should create a TableValuedParameter");
                res.ParameterName.Should().Be("Bar", "because it was passed in");

                ((TableValuedParameter)res).Value.As<IEnumerable<Model>>().Should().ContainInOrder(new[] { p1 }, "because it was passed in");
                ((TableValuedParameter)res).TypeName.Should().Be("[dbo].[Model]", "because it should be inferred from the Type's name");

            }).ShouldNotThrow("because it should be successful");
        }
        #endregion

        #region InferDbType
        [TestMethod]
        public void InferDbType_ReturnsString_ForEnum()
        {
            var result = typeof(EnumToTest).InferDbType();
            result.Should().Be(DbType.String, "because an enum should be passed as a string");
        }

        [TestMethod]
        public void InferDbType_ReturnsDateTimeOffset()
        {
            typeof(DateTimeOffset).InferDbType().Should().Be(DbType.DateTimeOffset);
        }

        [TestMethod]
        public void InferDbType_ReturnsBinary_ForByteArray()
        {
            typeof(byte[]).InferDbType().Should().Be(DbType.Binary);
        }
        #endregion

        #region TryInferSqlDbType
        [TestMethod]
        public void TryInferSqlDbType_ReturnsNVarChar_ForEnum()
        {
            var result = typeof(EnumToTest).TryInferSqlDbType();
            result.Should().Be(SqlDbType.NVarChar, "because an enum should be passed as a varchar");
        }

        [TestMethod]
        public void TryInferSqlDbType_ReturnsDateTimeOffset()
        {
            typeof(DateTimeOffset).TryInferSqlDbType().Should().Be(SqlDbType.DateTimeOffset);
        }

        [TestMethod]
        public void TryInferSqlDbType_ReturnsBinary_ForByteArray()
        {
            typeof(byte[]).TryInferSqlDbType().Should().Be(SqlDbType.VarBinary);
        }
        #endregion

        #region CreateSqlMetaData
        [TestMethod]
        public void CreateSqlMetaData_ReturnsSqlMetaData_ThatRepresents_DateTimeOffset()
        {
            var result = typeof(DateTimeOffset).CreateSqlMetaData("Foo", null, null, null, null);
            result.SqlDbType.Should().Be(SqlDbType.DateTimeOffset);
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
            public double Bar { get; }
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

        private enum EnumToTest
        {
            One, Two
        }
        #endregion
    }
}
