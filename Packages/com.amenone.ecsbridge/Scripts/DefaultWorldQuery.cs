using Unity.Entities;

namespace Amenone.EcsBridge
{
    // DefaultWorldSingleton と同じく、Play に入るたびに作り直される World に合わせて query を作り直す
    public sealed class DefaultWorldQuery
    {
        private readonly ComponentType[] _componentTypes;
        private readonly DefaultWorldFollower _defaultWorld = new();
        private EntityQuery _query;

        public DefaultWorldQuery(params ComponentType[] componentTypes) => _componentTypes = componentTypes;

        public EntityQuery Current()
        {
            if (_defaultWorld.FollowCurrent()) _query = _defaultWorld.Current.EntityManager.CreateEntityQuery(_componentTypes);

            return _query;
        }
    }
}
