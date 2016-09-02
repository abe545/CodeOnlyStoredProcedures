using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeOnlyStoredProcedure;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CodeOnlyTests
{
    [TestClass]
    public class IDbConnectionExtensionsTests
    {
        [TestClass]
        public class CreateCommandTests
        {
            [TestMethod]
            public void Clones_And_Opens_Connection_By_Default()
            {
                using (GlobalSettings.UseTestInstance())
                {
                    var db = new Mock<IDbConnection>();
                    var db2 = new Mock<IDbConnection>();

                    db.As<ICloneable>().Setup(c => c.Clone()).Returns(db2.Object);

                    var cmd = new Mock<IDbCommand>();
                    cmd.SetupAllProperties();

                    db2.Setup(d => d.CreateCommand()).Returns(cmd.Object);

                    IDbConnection outConn;
                    var res = db.Object.CreateCommand("foo", "bar", 20, out outConn);

                    outConn.Should().NotBeNull();
                    res.Should().NotBeNull();
                    res.CommandText.Should().Be("[foo].[bar]");
                    res.CommandTimeout.Should().Be(20);
                    res.CommandType.Should().Be(CommandType.StoredProcedure);

                    db.As<ICloneable>().Verify(c => c.Clone(), Times.Once());
                    db2.Verify(d => d.CreateCommand(), Times.Once());
                    db2.Verify(d => d.Open(), Times.Once());
                }
            }

            [TestMethod]
            public void Does_Not_Clone_When_Connection_Is_Not_Cloneable()
            {
                using (GlobalSettings.UseTestInstance())
                {
                    var db = new Mock<IDbConnection>();
                    var cmd = new Mock<IDbCommand>();
                    cmd.SetupAllProperties();

                    db.Setup(d => d.CreateCommand()).Returns(cmd.Object);

                    IDbConnection outConn;
                    var res = db.Object.CreateCommand("foo", "bar", 20, out outConn);

                    outConn.Should().BeNull();
                    res.Should().NotBeNull();
                    res.CommandText.Should().Be("[foo].[bar]");
                    res.CommandTimeout.Should().Be(20);
                    res.CommandType.Should().Be(CommandType.StoredProcedure);

                    db.Verify(d => d.CreateCommand(), Times.Once());
                    db.Verify(d => d.Open(), Times.Never());
                }
            }

            [TestMethod]
            public void Does_Not_Clone_When_Global_Option_Is_Set_To_Not_Clone_And_Opens_It_When_Closed()
            {
                using (GlobalSettings.UseTestInstance())
                {
                    GlobalSettings.Instance.CloneConnectionForEachCall = false;

                    var db = new Mock<IDbConnection>();
                    var cmd = new Mock<IDbCommand>();
                    cmd.SetupAllProperties();

                    db.SetupGet(d => d.State).Returns(ConnectionState.Closed);
                    db.As<ICloneable>().Setup(c => c.Clone()).Throws(new NotSupportedException());
                    db.Setup(d => d.CreateCommand()).Returns(cmd.Object);

                    IDbConnection outConn;
                    var res = db.Object.CreateCommand("foo", "bar", 20, out outConn);

                    outConn.Should().BeNull();
                    res.Should().NotBeNull();
                    res.CommandText.Should().Be("[foo].[bar]");
                    res.CommandTimeout.Should().Be(20);
                    res.CommandType.Should().Be(CommandType.StoredProcedure);

                    db.Verify(d => d.CreateCommand(), Times.Once());
                    db.Verify(d => d.Open(), Times.Never());
                }
            }
        }
    }
}
