using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Amenone.CSharpExtension
{
    [Serializable]
    public struct SerializableKeyValuePair<TKey, TValue>
    {
        public TKey key;
        public TValue value;

        public SerializableKeyValuePair(TKey key, TValue value)
        {
            this.key = key;
            this.value = value;
        }
    }

    [Serializable]
    public class SerializableDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField] private List<SerializableKeyValuePair<TKey, TValue>> pairs = new List<SerializableKeyValuePair<TKey, TValue>>();

        private Lookup _lookup;

        public SerializableDictionary()
        {
        }

        public SerializableDictionary(IEnumerable<KeyValuePair<TKey, TValue>> entries)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));
            foreach (var entry in entries) pairs.Add(new SerializableKeyValuePair<TKey, TValue>(entry.Key, entry.Value));
        }

        public int Count => CurrentLookup.ByKey.Count;

        public TValue this[TKey key] => CurrentLookup.ByKey[key];

        public IEnumerable<TKey> Keys => KeysOf(CurrentLookup);

        public IEnumerable<TValue> Values => ValuesOf(CurrentLookup);

        public bool ContainsKey(TKey key) => key is not null && CurrentLookup.ByKey.ContainsKey(key);

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (key is not null) return CurrentLookup.ByKey.TryGetValue(key, out value);
            value = default;
            return false;
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => CurrentLookup.InSerializedOrder.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize() => _lookup = null;

        private Lookup CurrentLookup
        {
            get
            {
                var lookup = _lookup;
                if (lookup != null) return lookup;
                lookup = Lookup.Build(pairs);
                _lookup = lookup;
                return lookup;
            }
        }

        private static IEnumerable<TKey> KeysOf(Lookup lookup)
        {
            foreach (var entry in lookup.InSerializedOrder) yield return entry.Key;
        }

        private static IEnumerable<TValue> ValuesOf(Lookup lookup)
        {
            foreach (var entry in lookup.InSerializedOrder) yield return entry.Value;
        }

        private sealed class Lookup
        {
            public readonly IReadOnlyDictionary<TKey, TValue> ByKey;
            public readonly IReadOnlyList<KeyValuePair<TKey, TValue>> InSerializedOrder;

            private Lookup(IReadOnlyDictionary<TKey, TValue> byKey, IReadOnlyList<KeyValuePair<TKey, TValue>> inSerializedOrder)
            {
                ByKey = byKey;
                InSerializedOrder = inSerializedOrder;
            }

            public static Lookup Build(IEnumerable<SerializableKeyValuePair<TKey, TValue>> pairs)
            {
                var byKey = new Dictionary<TKey, TValue>();
                var inSerializedOrder = new List<KeyValuePair<TKey, TValue>>();
                foreach (var pair in pairs ?? Array.Empty<SerializableKeyValuePair<TKey, TValue>>())
                {
                    if (IsMissingKey(pair.key) || byKey.ContainsKey(pair.key)) continue;
                    byKey.Add(pair.key, pair.value);
                    inSerializedOrder.Add(new KeyValuePair<TKey, TValue>(pair.key, pair.value));
                }
                return new Lookup(byKey, inSerializedOrder);
            }

            private static bool IsMissingKey(TKey key) => key is null || (key is UnityEngine.Object unityObject && unityObject == null);
        }
    }
}
