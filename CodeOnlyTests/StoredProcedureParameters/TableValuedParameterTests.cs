using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using CodeOnlyStoredProcedure;
using FluentAssertions;
using Microsoft.SqlServer.Server;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

#if NET40
namespace CodeOnlyTests.Net40.StoredProcedureParameters
#else
namespace CodeOnlyTests.StoredProcedureParameters
#endif
{
    [TestClass]
    public class TableValuedParameterTests : ParameterTestBase
    {
        [TestMethod]
        public void CanOnlyUseWithSqlServer()
        {
            var toTest = new TableValuedParameter("Foo", new[] { new TVP(42) }, typeof(TVP), "Fake");

            toTest.Invoking(t => t.CreateDbDataParameter(CreateCommand()))
                  .ShouldThrow<NotSupportedException>("it should only work if IDbCommand.CreateParameter() returns a SqlParameter")
                  .And.Message.Should().Be("Can only use Table Valued Parameters with SQL Server");
        }

        [TestMethod]
        public void SetsConstructorValuesOnParameter()
        {
            var cmd = new Mock<IDbCommand>();
            cmd.Setup(c => c.CreateParameter()).Returns(new SqlParameter());

            var toTest = new TableValuedParameter("Foo", new[] { new TVP(42) }, typeof(TVP), "CustomInt", "Schema");

            var res = toTest.CreateDbDataParameter(cmd.Object);

            res.DbType       .Should().Be(DbType.Object,            "table valued parameters pass DbType.Object");
            res.ParameterName.Should().Be("Foo",                    "it was passed in the constructor");
            res.Direction    .Should().Be(ParameterDirection.Input, "it is an input parameter");

            var typed = res.Should().BeOfType<SqlParameter>().Which;

            typed.SqlDbType.Should().Be(SqlDbType.Structured,   "table valued parameters are Structured");
            typed.TypeName .Should().Be("[Schema].[CustomInt]", "it was passed in the constructor");

            var meta = typed.Value.Should().BeAssignableTo<IEnumerable<SqlDataRecord>>().Which.Single();
            meta.FieldCount .Should().Be(1,  "only one row was passed in");
            meta.GetInt32(0).Should().Be(42, "it was passed in the row");
        }

        [TestMethod]
        public void IgnoresSetOnlyProperties()
        {
            var cmd = new Mock<IDbCommand>();
            cmd.Setup(c => c.CreateParameter()).Returns(new SqlParameter());

            var toTest = new TableValuedParameter("Foo", new[] { new TVP2(42) }, typeof(TVP2), "CustomInt", "Schema");

            var res = toTest.CreateDbDataParameter(cmd.Object);

            res.DbType.Should().Be(DbType.Object, "table valued parameters pass DbType.Object");
            res.ParameterName.Should().Be("Foo", "it was passed in the constructor");
            res.Direction.Should().Be(ParameterDirection.Input, "it is an input parameter");

            var typed = res.Should().BeOfType<SqlParameter>().Which;

            typed.SqlDbType.Should().Be(SqlDbType.Structured, "table valued parameters are Structured");
            typed.TypeName.Should().Be("[Schema].[CustomInt]", "it was passed in the constructor");

            var meta = typed.Value.Should().BeAssignableTo<IEnumerable<SqlDataRecord>>().Which.Single();
            meta.FieldCount.Should().Be(1, "only one row was passed in");
            meta.GetInt32(0).Should().Be(42, "it was passed in the row");
        }

        [TestMethod]
        public void ToStringRepresentsTheParameter()
        {
            new TableValuedParameter("Foo", new[] { new TVP(42) }, typeof(TVP), "CustomInt", "Schema").ToString()
                .Should().Be(string.Format("@Foo = IEnumerable<{0}> (1 items)", typeof(TVP)));
        }

        [TestMethod]
        public void ToStringDoesNotDisplayExtraAts()
        {
            new TableValuedParameter("@Foo", new[] { new TVP(42) }, typeof(TVP), "CustomInt", "Schema").ToString()
                .Should().Be(string.Format("@Foo = IEnumerable<{0}> (1 items)", typeof(TVP)));
        }

        [TestMethod]
        public void SetsNullValueWhenEnumerableIsEmpty()
        {
            var cmd = new Mock<IDbCommand>();
            cmd.Setup(c => c.CreateParameter()).Returns(new SqlParameter());

            var toTest = new TableValuedParameter("Foo", new TVP2[0], typeof(TVP2), "CustomInt", "Schema");

            var res = toTest.CreateDbDataParameter(cmd.Object);

            res.DbType.Should().Be(DbType.Object, "table valued parameters pass DbType.Object");
            res.ParameterName.Should().Be("Foo", "it was passed in the constructor");
            res.Direction.Should().Be(ParameterDirection.Input, "it is an input parameter");
            res.Value.Should().BeNull();

            var typed = res.Should().BeOfType<SqlParameter>().Which;

            typed.SqlDbType.Should().Be(SqlDbType.Structured, "table valued parameters are Structured");
            typed.TypeName.Should().Be("[Schema].[CustomInt]", "it was passed in the constructor");
            typed.Value.Should().BeNull();
        }

        private class TVP
        {
            public int Int { get; set; }

            public TVP(int val)
            {
                Int = val;
            }
        }

        private class TVP2
        {
            private string value;
            public int Int { get; set; }
            public string Value { set { this.value = value; } }

            public TVP2(int val)
            {
                Int = val;
            }
        }
    }
}
