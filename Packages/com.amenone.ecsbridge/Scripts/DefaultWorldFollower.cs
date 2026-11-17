using System;
using Unity.Entities;

namespace Amenone.EcsBridge
{
    internal sealed class DefaultWorldFollower
    {
        public World Current { get; private set; }

        public bool FollowCurrent()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world is not { IsCreated: true })
            {
                throw new InvalidOperationException(
                    "World.DefaultGameObjectInjectionWorld is null or already disposed. Create the default world before reading or writing it through Amenone.EcsBridge.");
            }

            if (Current == world) return false;

            Current = world;
            return true;
        }
    }
}
