using Unity.Entities;

namespace Amenone.EcsBridge
{
    // DefaultWorldSingleton と同じく、World が作り直されたら query とバッファを作り直す
    public sealed class DefaultWorldSingletonBuffer<T> where T : unmanaged, IBufferElementData
    {
        private readonly DefaultWorldFollower _defaultWorld = new();
        private EntityQuery _query;

        public void Prepare() => FollowCurrentWorld();

        public DynamicBuffer<T> Buffer()
        {
            FollowCurrentWorld();
            return _query.GetSingletonBuffer<T>();
        }

        private void FollowCurrentWorld()
        {
            if (!_defaultWorld.FollowCurrent()) return;

            var entityManager = _defaultWorld.Current.EntityManager;
            _query = entityManager.CreateEntityQuery(ComponentType.ReadWrite<T>());
            if (_query.IsEmpty) entityManager.CreateSingletonBuffer<T>();
        }
    }
}
