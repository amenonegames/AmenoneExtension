using System.Collections.Generic;

namespace Amenone.EcsBridge
{
    public sealed class LastPublished<TKey, TValue>
    {
        private readonly Dictionary<TKey, TValue> _values = new();
        private bool _hasPublishedForEveryKey;
        private TValue _publishedForEveryKey;

        public bool TryUpdate(TKey key, TValue value)
        {
            if (TryGetPublished(key, out var published) && EqualityComparer<TValue>.Default.Equals(published, value)) return false;

            TreatAsPublished(key, value);
            return true;
        }

        public bool TryGetPublished(TKey key, out TValue value)
        {
            if (_values.TryGetValue(key, out value)) return true;

            value = _publishedForEveryKey;
            return _hasPublishedForEveryKey;
        }

        public void TreatAsPublished(TKey key, TValue value) => _values[key] = value;

        // 覚えたキーもここで全部忘れる
        public void TreatEveryKeyAsPublished(TValue value)
        {
            _values.Clear();
            _hasPublishedForEveryKey = true;
            _publishedForEveryKey = value;
        }

        public void Clear()
        {
            _values.Clear();
            _hasPublishedForEveryKey = false;
            _publishedForEveryKey = default;
        }
    }
}
