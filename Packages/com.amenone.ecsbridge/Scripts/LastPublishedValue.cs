using System.Collections.Generic;

namespace Amenone.EcsBridge
{
    public sealed class LastPublishedValue<T>
    {
        private bool _hasPublished;
        private T _published;

        public bool TryUpdate(T value)
        {
            if (_hasPublished && EqualityComparer<T>.Default.Equals(_published, value)) return false;

            _hasPublished = true;
            _published = value;
            return true;
        }

        public void Clear()
        {
            _hasPublished = false;
            _published = default;
        }
    }
}
