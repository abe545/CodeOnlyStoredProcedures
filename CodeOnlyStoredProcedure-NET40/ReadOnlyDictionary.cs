using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace CodeOnlyStoredProcedure
{
    public sealed class ReadOnlyDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private const string mutateException = "Can not modify a ReadOnlyDictionary";
        private readonly IDictionary<TKey, TValue> internalCollection;

        #region Non-Mutating Methods
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

        public ReadOnlyDictionary(IDictionary<TKey, TValue> toWrap)
        {
            Contract.Requires(toWrap != null);

            internalCollection = toWrap;
        }

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
