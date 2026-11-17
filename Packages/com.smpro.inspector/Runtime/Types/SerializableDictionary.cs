using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SMPro.Inspector
{
    public interface ISerializableDictionary
    {
        int EntryCount { get; }
        object KeyAt(int index);
        IReadOnlyList<(int first, int duplicate)> FindDuplicateKeys();
        IReadOnlyList<int> FindMissingKeys();
    }

    [Serializable]
    public class SerializableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, ISerializableDictionary, ISerializationCallbackReceiver
    {
        [Serializable]
        public struct Entry
        {
            public TKey key;
            public TValue value;
        }

        [SerializeField] List<Entry> entries = new();
        [NonSerialized] Dictionary<TKey, TValue> map = new();
        [NonSerialized] bool changedInCode;

        public SerializableDictionary()
        {
        }

        public SerializableDictionary(IDictionary<TKey, TValue> source)
        {
            foreach (KeyValuePair<TKey, TValue> pair in source) Add(pair.Key, pair.Value);
        }

        Dictionary<TKey, TValue> Map => map ??= new Dictionary<TKey, TValue>();

        public TValue this[TKey key]
        {
            get => Map[key];
            set
            {
                Map[key] = value;
                changedInCode = true;
            }
        }

        public ICollection<TKey> Keys => Map.Keys;
        public ICollection<TValue> Values => Map.Values;
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Map.Keys;
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Map.Values;
        public int Count => Map.Count;
        public bool IsReadOnly => false;

        public void Add(TKey key, TValue value)
        {
            Map.Add(key, value);
            changedInCode = true;
        }

        public bool Remove(TKey key)
        {
            if (!Map.Remove(key)) return false;
            changedInCode = true;
            return true;
        }

        public void Clear()
        {
            Map.Clear();
            changedInCode = true;
        }

        public bool ContainsKey(TKey key) => Map.ContainsKey(key);
        public bool TryGetValue(TKey key, out TValue value) => Map.TryGetValue(key, out value);
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => Map.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Map.GetEnumerator();

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item) => ((ICollection<KeyValuePair<TKey, TValue>>)Map).Contains(item);
        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => ((ICollection<KeyValuePair<TKey, TValue>>)Map).CopyTo(array, arrayIndex);

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            if (!((ICollection<KeyValuePair<TKey, TValue>>)Map).Remove(item)) return false;
            changedInCode = true;
            return true;
        }

        public void OnBeforeSerialize()
        {
            if (!changedInCode) return;
            entries.Clear();
            foreach (KeyValuePair<TKey, TValue> pair in Map) entries.Add(new Entry { key = pair.Key, value = pair.Value });
            changedInCode = false;
        }

        public void OnAfterDeserialize()
        {
            Map.Clear();
            foreach (Entry entry in entries)
                if (entry.key != null && !Map.ContainsKey(entry.key)) Map.Add(entry.key, entry.value);
            changedInCode = false;
        }

        int ISerializableDictionary.EntryCount => entries.Count;

        object ISerializableDictionary.KeyAt(int index) => entries[index].key;

        IReadOnlyList<(int first, int duplicate)> ISerializableDictionary.FindDuplicateKeys()
        {
            var duplicates = new List<(int first, int duplicate)>();
            var firstIndex = new Dictionary<TKey, int>();
            for (int i = 0; i < entries.Count; i++)
            {
                TKey key = entries[i].key;
                if (key == null) continue;
                if (firstIndex.TryGetValue(key, out int first)) duplicates.Add((first, i));
                else firstIndex.Add(key, i);
            }
            return duplicates;
        }

        IReadOnlyList<int> ISerializableDictionary.FindMissingKeys()
        {
            var missing = new List<int>();
            for (int i = 0; i < entries.Count; i++)
                if (entries[i].key == null || (entries[i].key is UnityEngine.Object unityObject && unityObject == null)) missing.Add(i);
            return missing;
        }
    }
}
