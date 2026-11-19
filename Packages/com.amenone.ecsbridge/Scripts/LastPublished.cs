using System.Collections.Generic;

namespace Amenone.EcsBridge
{
    public sealed class LastPublished<TKey, TValue>
    {
        private readonly Dictionary<TKey, TValue> _values = new();

        public bool TryUpdate(TKey key, TValue value)
        {
            if (_values.TryGetValue(key, out var published) && EqualityComparer<TValue>.Default.Equals(published, value)) return false;

            _values[key] = value;
            return true;
        }

        public void Clear() => _values.Clear();
    }
}
