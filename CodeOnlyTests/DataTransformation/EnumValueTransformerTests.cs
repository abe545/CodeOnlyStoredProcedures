using CodeOnlyStoredProcedure.DataTransformation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeOnlyTests.DataTransformation
{
    [TestClass]
    public class EnumValueTransformerTests
    {
        private EnumValueTransformer toTest;
        private IEnumerable<Attribute> attrs;

        [TestInitialize]
        public void TestIntitialize()
        {
            toTest = new EnumValueTransformer();
            attrs = Enumerable.Empty<Attribute>();
        }

        #region CanTransform Tests
        [TestMethod]
        public void TestCanTransformReturnsTrueForValidIntInput()
        {
            Assert.IsTrue(toTest.CanTransform(1, typeof(IntEnum), attrs));
        }

        [TestMethod]
        public void TestCanTransformReturnsTrueForValidUintInput()
        {
            Assert.IsTrue(toTest.CanTransform(2U, typeof(UintEnum), attrs));
        }

        [TestMethod]
        public void TestCanTransformReturnsTrueForValidLongInput()
        {
            Assert.IsTrue(toTest.CanTransform(0L, typeof(LongEnum), attrs));
        }

        [TestMethod]
        public void TestCanTransformReturnsTrueForValidUlongInput()
        {
            Assert.IsTrue(toTest.CanTransform(3UL, typeof(UlongEnum), attrs));
        }

        [TestMethod]
        public void TestCanTransformReturnsTrueForValidShortInput()
        {
            Assert.IsTrue(toTest.CanTransform((short)1, typeof(ShortEnum), attrs));
        }

        [TestMethod]
        public void TestCanTransformReturnsTrueForValidUshortInput()
        {
            Assert.IsTrue(toTest.CanTransform((ushort)1, typeof(UshortEnum), attrs));
        }

        [TestMethod]
        public void TestCanTransformReturnsTrueForValidByteInput()
        {
            Assert.IsTrue(toTest.CanTransform((byte)1, typeof(ByteEnum), attrs));
        }

        [TestMethod]
        public void TestCanTransformReturnsTrueForValidSbyteInput()
        {
            Assert.IsTrue(toTest.CanTransform((sbyte)1, typeof(SbyteEnum), attrs));
        }

        [TestMethod]
        public void TestCanTransformReturnsTrueForStringInput()
        {
            Assert.IsTrue(toTest.CanTransform("Hello", typeof(LongEnum), attrs));
        }

        [TestMethod]
        public void TestCanTransformReturnsFalseWhenValueIsNotValid()
        {
            Assert.IsFalse(toTest.CanTransform(false, typeof(IntEnum), attrs));
        }

        [TestMethod]
        public void TestCanTransformReturnsFalseWhenTargetTypeIsNotEnum()
        {
            Assert.IsFalse(toTest.CanTransform(1, typeof(string), attrs));
        }

        [TestMethod]
        public void TestCanTransformReturnsFalseWhenValueIsNull()
        {
            Assert.IsFalse(toTest.CanTransform(null, typeof(IntEnum), attrs));
        } 
        #endregion

        #region Transform Tests
        [TestMethod]
        public void TestTransformTransformsStringToEnum()
        {
            var res = toTest.Transform("Hello", typeof(LongEnum), attrs);
            Assert.AreEqual(LongEnum.Hello, res);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestTransformThrowsOnNotFoundString()
        {
            toTest.Transform("None", typeof(LongEnum), attrs);
        }

        [TestMethod]
        public void TestTransformIntValue()
        {
            var res = toTest.Transform(-1, typeof(IntEnum), attrs);
            Assert.AreEqual(IntEnum.Negative, res);
        }

        [TestMethod]
        public void TestTransformUintValue()
        {
            var res = toTest.Transform(1U, typeof(UintEnum), attrs);
            Assert.AreEqual(UintEnum.One, res);
        }

        [TestMethod]
        public void TestTransformLongValue()
        {
            var res = toTest.Transform(1L, typeof(LongEnum), attrs);
            Assert.AreEqual(LongEnum.World, res);
        }

        [TestMethod]
        public void TestTransformUlongValue()
        {
            var res = toTest.Transform(1U, typeof(UlongEnum), attrs);
            Assert.AreEqual(UlongEnum.One, res);
        }

        [TestMethod]
        public void TestTransformShortValue()
        {
            var res = toTest.Transform(1, typeof(ShortEnum), attrs);
            Assert.AreEqual(ShortEnum.One, res);
        }

        [TestMethod]
        public void TestTransformUshortValue()
        {
            var res = toTest.Transform(1U, typeof(UshortEnum), attrs);
            Assert.AreEqual(UshortEnum.One, res);
        }

        [TestMethod]
        public void TestTransformByteValue()
        {
            var res = toTest.Transform(1, typeof(ByteEnum), attrs);
            Assert.AreEqual(ByteEnum.One, res);
        }

        [TestMethod]
        public void TestTransformSbyteValue()
        {
            var res = toTest.Transform(1U, typeof(SbyteEnum), attrs);
            Assert.AreEqual(SbyteEnum.One, res);
        }
        #endregion

        #region Enums
        private enum IntEnum : int
        {
            Negative = -1,
            Zero = 0,
            One = 1,
            Two = 2
        }

        private enum LongEnum : long
        {
            Hello,
            World
        }

        private enum UintEnum : uint
        {
            One = 1,
            Two = 2
        }

        private enum UlongEnum : ulong
        {
            One = 1,
            Two = 2
        }

        private enum ShortEnum : short
        {
            One = 1,
            Two = 2
        }

        private enum UshortEnum : ushort
        {
            One = 1,
            Two = 2
        }

        private enum ByteEnum : byte
        {
            One = 1,
            Two = 2
        }

        private enum SbyteEnum : sbyte
        {
            One = 1,
            Two = 2
        } 
        #endregion
    }
}
