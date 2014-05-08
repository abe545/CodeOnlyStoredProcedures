using CodeOnlyStoredProcedure.DataTransformation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

#if NET40
namespace CodeOnlyTests.Net40.DataTransformation
#else
namespace CodeOnlyTests.DataTransformation
#endif
{
    [TestClass]
    public class EnumValueTransformerTests
    {
        private EnumValueTransformer toTest;
        private ParameterExpression  input;

        [TestInitialize]
        public void TestIntitialize()
        {
            toTest = new EnumValueTransformer();
            input  = Expression.Parameter(typeof(object), "input");
        }

        #region CanTransform Tests
        [TestMethod]
        public void TestCanTransformReturnsTrueForValidIntInput()
        {
            Assert.IsTrue(toTest.CanTransform(typeof(IntEnum)));
        }

        [TestMethod]
        public void TestCanTransformReturnsTrueForValidUintInput()
        {
            Assert.IsTrue(toTest.CanTransform(typeof(UintEnum)));
        }

        [TestMethod]
        public void TestCanTransformReturnsTrueForValidLongInput()
        {
            Assert.IsTrue(toTest.CanTransform(typeof(LongEnum)));
        }

        [TestMethod]
        public void TestCanTransformReturnsTrueForValidUlongInput()
        {
            Assert.IsTrue(toTest.CanTransform(typeof(UlongEnum)));
        }

        [TestMethod]
        public void TestCanTransformReturnsTrueForValidShortInput()
        {
            Assert.IsTrue(toTest.CanTransform(typeof(ShortEnum)));
        }

        [TestMethod]
        public void TestCanTransformReturnsTrueForValidUshortInput()
        {
            Assert.IsTrue(toTest.CanTransform(typeof(UshortEnum)));
        }

        [TestMethod]
        public void TestCanTransformReturnsTrueForValidByteInput()
        {
            Assert.IsTrue(toTest.CanTransform(typeof(ByteEnum)));
        }

        [TestMethod]
        public void TestCanTransformReturnsTrueForValidSbyteInput()
        {
            Assert.IsTrue(toTest.CanTransform(typeof(SbyteEnum)));
        }

        [TestMethod]
        public void TestCanTransformReturnsFalseWhenTargetTypeIsNotEnum()
        {
            Assert.IsFalse(toTest.CanTransform(typeof(string)));
        }

        [TestMethod]
        public void TestCanTransformReturnsTrueWhenTargetTypeIsNullableEnum()
        {
            Assert.IsTrue(toTest.CanTransform(typeof(IntEnum)));
        }
        #endregion

        #region Transform Tests
        [TestMethod]
        public void TestTransformTransformsStringToEnum()
        {
            var res  = toTest.CreateTransformation(typeof(LongEnum), input);
            var func = CreateTestFunc(res);
            Assert.AreEqual(LongEnum.Hello, func("Hello"));
        }

        [TestMethod]
        public void TestTransformThrowsOnNotFoundString()
        {
            var res  = toTest.CreateTransformation(typeof(LongEnum), input);
            var func = CreateTestFunc(res);

            try
            {
                // this should throw an ArgumentException
                func("None");
                Assert.Fail("Should throw an ArgumentException when converting an invalid string value.");
            }
            catch (ArgumentException) { }
        }

        [TestMethod]
        public void TestTransformIntValue()
        {
            var res  = toTest.CreateTransformation(typeof(IntEnum), input);
            var func = CreateTestFunc(res);
            Assert.AreEqual(IntEnum.Negative, func(-1));
        }

        [TestMethod]
        public void TestTransformUintValue()
        {
            var res  = toTest.CreateTransformation(typeof(UintEnum), input);
            var func = CreateTestFunc(res);
            Assert.AreEqual(UintEnum.One, func(1U));
        }

        [TestMethod]
        public void TestTransformLongValue()
        {
            var res  = toTest.CreateTransformation(typeof(LongEnum), input);
            var func = CreateTestFunc(res);
            Assert.AreEqual(LongEnum.World, func(1L));
        }

        [TestMethod]
        public void TestTransformUlongValue()
        {
            var res  = toTest.CreateTransformation(typeof(UlongEnum), input);
            var func = CreateTestFunc(res);
            Assert.AreEqual(UlongEnum.One, func(1U));
        }

        [TestMethod]
        public void TestTransformShortValue()
        {
            var res  = toTest.CreateTransformation(typeof(ShortEnum), input);
            var func = CreateTestFunc(res);
            Assert.AreEqual(ShortEnum.One, func(1));
        }

        [TestMethod]
        public void TestTransformUshortValue()
        {
            var res  = toTest.CreateTransformation(typeof(UshortEnum), input);
            var func = CreateTestFunc(res);
            Assert.AreEqual(UshortEnum.One, func(1U));
        }

        [TestMethod]
        public void TestTransformByteValue()
        {
            var res  = toTest.CreateTransformation(typeof(ByteEnum), input);
            var func = CreateTestFunc(res);
            Assert.AreEqual(ByteEnum.One, func(1));
        }

        [TestMethod]
        public void TestTransformSbyteValue()
        {
            var res  = toTest.CreateTransformation(typeof(SbyteEnum), input);
            var func = CreateTestFunc(res);
            Assert.AreEqual(SbyteEnum.One, func(1U));
        }

        [TestMethod]
        public void TestTransformNullValueThrowsException()
        {
            var res  = toTest.CreateTransformation(typeof(IntEnum), input);
            var func = CreateTestFunc(res);

            try
            {
                func(null);
                Assert.Fail("Should have failed with an ArgumentNullException.");
            }
            catch (ArgumentNullException) { }
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

        private Func<object, object> CreateTestFunc(Expression toTest)
        {
            var exit = Expression.Label(typeof(object), "exit");
            var expr = Expression.Block(typeof(object), toTest, Expression.Return(exit, input), Expression.Label(exit, Expression.Constant(null)));
            var lambda = Expression.Lambda<Func<object, object>>(expr, input);

            return lambda.Compile();
        }
    }
}
