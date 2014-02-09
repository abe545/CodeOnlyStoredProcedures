using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace CodeOnlyStoredProcedure
{
    /// <summary>
    /// A read-only implementation of IDictionary$lt;TKey, TValue$gt;
    /// </summary>
    /// <remarks>This is needed for the 4.0 version of CodeOnlyStoredProcedure because
    /// the Immutable Collection project only works on 4.5 and above. Ironically, .NET 4.5
    /// ships with a ReadOnlyDictionary implementation</remarks>
    /// <typeparam name="TKey">The type to use as the key of the dictionary</typeparam>
    /// <typeparam name="TValue">The type of values stored in the dictionary</typeparam>
    /// <inheritdoc />
    internal sealed class ReadOnlyDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private const string mutateException = "Can not modify a ReadOnlyDictionary";
        private readonly IDictionary<TKey, TValue> internalCollection;

        #region Non-Mutating Methods
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(TKey key)
        {
            return internalCollection.ContainsKey(key);
        }

        public ICollection<TKey> Keys
        {
            get { return internalCollection.Keys; }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return internalCollection.TryGetValue(key, out value);
        }

        public ICollection<TValue> Values
        {
            get { return internalCollection.Values; }
        }

        public TValue this[TKey key]
        {
            get { return internalCollection[key]; }
            set { throw new NotSupportedException(mutateException); }
        }

        public int Count
        {
            get { return internalCollection.Count; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return internalCollection.GetEnumerator();
        } 

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            return internalCollection.Contains(item);
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            internalCollection.CopyTo(array, arrayIndex);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        } 
        #endregion

        #region Mutating Methods
        void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
        {
            throw new NotSupportedException(mutateException);
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            throw new NotSupportedException(mutateException);
        }

        bool IDictionary<TKey, TValue>.Remove(TKey key)
        {
            throw new NotSupportedException(mutateException);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            throw new NotSupportedException(mutateException);
        } 

        void ICollection<KeyValuePair<TKey, TValue>>.Clear()
        {
            throw new NotSupportedException(mutateException);
        }
        #endregion

        /// <summary>
        /// Creates a ReadOnlyDictionary that provides read-only access to the given
        /// IDictionary. If the given dictionary is mutated after constructing this
        /// ReadOnlyDictionary, the ReadOnlyDictionary will reflect those changes.
        /// </summary>
        /// <param name="toWrap">The dictionary to provide read-only access to.</param>
        public ReadOnlyDictionary(IDictionary<TKey, TValue> toWrap)
        {
            Contract.Requires(toWrap != null);

            internalCollection = toWrap;
        }

        /// <summary>
        /// Creates a ReadOnlyDictionary with the given KeyValuePairs.
        /// </summary>
        /// <param name="values">The KeyValuePairs that hold the only data this
        /// ReadOnlyDictionary will ever provide access to.</param>
        public ReadOnlyDictionary(IEnumerable<KeyValuePair<TKey, TValue>> values)
        {
            Contract.Requires(values != null);

            internalCollection = new Dictionary<TKey, TValue>();
            foreach (var kv in values)
                internalCollection.Add(kv);
        }

        [ContractInvariantMethod]
        private void Invariants()
        {
            Contract.Invariant(internalCollection != null);
        }
    }
}
