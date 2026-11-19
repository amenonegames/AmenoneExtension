using Unity.Entities;

namespace Amenone.EcsBridge
{
    // Play に入るたびに既定の World は作り直される。ドメインもシーンも再読み込みしない設定では、
    // 前の World で作った query を持ち続けてしまうので、使うたびに今の World のものか確かめて作り直す
    public sealed class DefaultWorldSingleton<T> where T : unmanaged, IComponentData
    {
        private readonly T _initialValue;
        private readonly bool _createsWhenMissing;
        private readonly DefaultWorldFollower _defaultWorld = new();
        private EntityQuery _query;

        public DefaultWorldSingleton(T initialValue = default, bool createsWhenMissing = true)
        {
            _initialValue = initialValue;
            _createsWhenMissing = createsWhenMissing;
        }

        public void Prepare() => FollowCurrentWorld();

        public bool Exists()
        {
            FollowCurrentWorld();
            return !_query.IsEmpty;
        }

        public void Remove()
        {
            FollowCurrentWorld();
            if (_query.IsEmpty) return;

            _defaultWorld.Current.EntityManager.DestroyEntity(_query.GetSingletonEntity());
        }

        public T Read()
        {
            FollowCurrentWorld();
            return _query.GetSingleton<T>();
        }

        public void Write(T value)
        {
            FollowCurrentWorld();
            if (_query.IsEmpty)
            {
                _defaultWorld.Current.EntityManager.CreateSingleton(value);
                return;
            }

            _query.SetSingleton(value);
        }

        private void FollowCurrentWorld()
        {
            if (!_defaultWorld.FollowCurrent()) return;

            var entityManager = _defaultWorld.Current.EntityManager;
            _query = entityManager.CreateEntityQuery(ComponentType.ReadWrite<T>());
            if (_createsWhenMissing && _query.IsEmpty) entityManager.CreateSingleton(_initialValue);
        }
    }
}
