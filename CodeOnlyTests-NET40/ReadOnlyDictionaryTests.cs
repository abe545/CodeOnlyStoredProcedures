using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CodeOnlyStoredProcedure;
using System.Collections.Generic;

namespace CodeOnlyTests.Net40
{
    [TestClass]
    public class ReadOnlyDictionaryTests
    {
        [TestMethod]
        public void TestConstructorWrapsDictionary()
        {
            var dict = new Dictionary<int, string>()
            {
                { 1, "One"   },
                { 2, "Two"   },
                { 3, "Three" }
            };

            var toTest = new ReadOnlyDictionary<int, string>(dict);

            Assert.IsTrue(toTest.IsReadOnly);
            Assert.AreEqual(3, toTest.Count);
            Assert.IsTrue(toTest.ContainsKey(1));
            Assert.IsTrue(toTest.ContainsKey(2));
            Assert.IsTrue(toTest.ContainsKey(3));
            Assert.AreEqual("One", toTest[1]);
            Assert.AreEqual("Two", toTest[2]);
            Assert.AreEqual("Three", toTest[3]);
            Assert.IsTrue(dict.OrderBy(kv => kv.Key).SequenceEqual(toTest.OrderBy(kv => kv.Key)));
            Assert.IsTrue(dict.Keys.OrderBy(k => k).SequenceEqual(toTest.Keys.OrderBy(k => k)));
            Assert.IsTrue(dict.Values.OrderBy(v => v).SequenceEqual(toTest.Values.OrderBy(v => v)));
        }

        [TestMethod]
        public void TestConstructorWrapsKeyValuePairs()
        {
            var pairs = new List<KeyValuePair<int, bool>>
            {
                new KeyValuePair<int, bool>(0, true),
                new KeyValuePair<int, bool>(1, false),
                new KeyValuePair<int, bool>(2, true),
                new KeyValuePair<int, bool>(3, false)
            };

            var toTest = new ReadOnlyDictionary<int, bool>(pairs);

            Assert.IsTrue(toTest.IsReadOnly);
            Assert.AreEqual(4, toTest.Count);
            Assert.IsTrue(toTest.ContainsKey(0));
            Assert.IsTrue(toTest.ContainsKey(1));
            Assert.IsTrue(toTest.ContainsKey(2));
            Assert.IsTrue(toTest.ContainsKey(3));
            Assert.AreEqual(true, toTest[0]);
            Assert.AreEqual(false, toTest[1]);
            Assert.AreEqual(true, toTest[2]);
            Assert.AreEqual(false, toTest[3]);

            Assert.IsTrue(pairs.OrderBy(kv => kv.Key).SequenceEqual(toTest.OrderBy(kv => kv.Key)));
            Assert.IsTrue(pairs.Select(kv => kv.Key).OrderBy(k => k).SequenceEqual(toTest.Keys.OrderBy(k => k)));
            Assert.IsTrue(pairs.Select(kv => kv.Value).OrderBy(v => v).SequenceEqual(toTest.Values.OrderBy(v => v)));
        }

        [TestMethod]
        public void TestTryGetValueGetsValueInBaseDictionary()
        {
            var dict = new Dictionary<string, int>
            {
                { "Hello", 5 },
                { ", "   , 2 },
                { "World", 5 },
                { "!"    , 1 }
            };

            var toTest = new ReadOnlyDictionary<string, int>(dict);
            Assert.IsTrue(toTest.IsReadOnly);
            
            int value;
            Assert.IsTrue(toTest.TryGetValue("Hello", out value));
            Assert.AreEqual(5, value);
            Assert.IsTrue(toTest.TryGetValue(", ", out value));
            Assert.AreEqual(2, value);
            Assert.IsTrue(toTest.TryGetValue("World", out value));
            Assert.AreEqual(5, value);
            Assert.IsTrue(toTest.TryGetValue("!", out value));
            Assert.AreEqual(1, value);
        }

        [TestMethod]
        public void TestTryGetValueReturnsFalseWhenValueIsNotInBaseDictionary()
        {
            var dict = new Dictionary<int, string>()
            {
                { 1, "One"   },
                { 2, "Two"   },
                { 3, "Three" }
            };

            var toTest = new ReadOnlyDictionary<int, string>(dict);
            Assert.IsTrue(toTest.IsReadOnly);
            
            string value;
            Assert.IsFalse(toTest.TryGetValue(4, out value));
            Assert.IsNull(value);
        }

        [TestMethod]
        public void TestICollctionContainsReturnsTrueWhenAppropriate()
        {
            var dict = new Dictionary<int, string>()
            {
                { 1, "One"   },
                { 2, "Two"   },
                { 3, "Three" }
            };

            IDictionary<int, string> toTest = new ReadOnlyDictionary<int, string>(dict);

            Assert.IsTrue(toTest.IsReadOnly);
            Assert.IsTrue(toTest.Contains(new KeyValuePair<int, string>(1, "One")));
            Assert.IsTrue(toTest.Contains(new KeyValuePair<int, string>(2, "Two")));
            Assert.IsTrue(toTest.Contains(new KeyValuePair<int, string>(3, "Three")));

            Assert.IsFalse(toTest.Contains(new KeyValuePair<int, string>(1, "Two")));
            Assert.IsFalse(toTest.Contains(new KeyValuePair<int, string>(4, "Four")));
        }

        [TestMethod]
        public void TestCopyToCopiesAllValues()
        {
            var dict = new Dictionary<int, string>()
            {
                { 1, "One"   },
                { 2, "Two"   },
                { 3, "Three" }
            };

            IDictionary<int, string> toTest = new ReadOnlyDictionary<int, string>(dict);
            Assert.IsTrue(toTest.IsReadOnly);
            
            var copy = new KeyValuePair<int, string>[3];
            toTest.CopyTo(copy, 0);

            Assert.IsTrue(copy.OrderBy(kv => kv.Key).SequenceEqual(dict.OrderBy(kv => kv.Key)));
        }

        [TestMethod]
        public void TestCopyToCopiesAllValuesIntoMiddleOfArray()
        {
            var dict = new Dictionary<int, string>()
            {
                { 1, "One"   },
                { 2, "Two"   },
                { 3, "Three" }
            };

            IDictionary<int, string> toTest = new ReadOnlyDictionary<int, string>(dict);
            Assert.IsTrue(toTest.IsReadOnly);
            
            var copy = new KeyValuePair<int, string>[7];
            toTest.CopyTo(copy, 2);

            Assert.AreEqual(default(KeyValuePair<int, string>), copy[0]);
            Assert.AreEqual(default(KeyValuePair<int, string>), copy[1]);
            Assert.AreEqual(default(KeyValuePair<int, string>), copy[5]);
            Assert.AreEqual(default(KeyValuePair<int, string>), copy[6]);
            Assert.IsTrue(copy.Skip(2).Take(3).OrderBy(kv => kv.Key).SequenceEqual(dict.OrderBy(kv => kv.Key)));
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void TestSet()
        {
            IDictionary<int, int> toTest = new ReadOnlyDictionary<int, int>(new Dictionary<int, int>());
            Assert.IsTrue(toTest.IsReadOnly);
            toTest[100] = 1;
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void TestAdd()
        {
            IDictionary<int, bool> toTest = new ReadOnlyDictionary<int, bool>(new Dictionary<int, bool>());
            Assert.IsTrue(toTest.IsReadOnly);
            toTest.Add(100, false);
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void TestAddKeyValuePair()
        {
            IDictionary<int, bool> toTest = new ReadOnlyDictionary<int, bool>(new Dictionary<int, bool>());
            Assert.IsTrue(toTest.IsReadOnly);
            toTest.Add(new KeyValuePair<int, bool>(100, false));
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void TestRemoveExistingValue()
        {
            var dict = new Dictionary<int, int> { { 100, 99 } };

            IDictionary<int, int> toTest = new ReadOnlyDictionary<int, int>(dict);
            Assert.IsTrue(toTest.IsReadOnly);
            toTest.Remove(100);
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void TestRemoveNotExistingValue()
        {
            var dict = new Dictionary<int, int> { { 100, 99 } };

            IDictionary<int, int> toTest = new ReadOnlyDictionary<int, int>(dict);
            Assert.IsTrue(toTest.IsReadOnly);
            toTest.Remove(10);
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void TestClear()
        {
            var dict = new Dictionary<int, string>()
            {
                { 1, "One"   },
                { 2, "Two"   },
                { 3, "Three" }
            };

            IDictionary<int, string> toTest = new ReadOnlyDictionary<int, string>(dict);
            Assert.IsTrue(toTest.IsReadOnly);
            toTest.Clear();
        }
    }
}
