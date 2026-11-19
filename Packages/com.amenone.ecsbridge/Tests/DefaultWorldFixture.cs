using System.Collections.Generic;
using NUnit.Framework;
using Unity.Entities;

namespace Amenone.EcsBridge.Tests
{
    public abstract class DefaultWorldFixture
    {
        private readonly List<World> _worldsToDispose = new();
        private World _originalDefaultWorld;

        protected static World DefaultWorld => World.DefaultGameObjectInjectionWorld;

        [SetUp]
        public void ReplaceDefaultWorldForTest()
        {
            _originalDefaultWorld = World.DefaultGameObjectInjectionWorld;
            ReplaceDefaultWorld();
        }

        [TearDown]
        public void RestoreOriginalDefaultWorld()
        {
            foreach (var world in _worldsToDispose)
            {
                if (!world.IsCreated) continue;

                ScriptBehaviourUpdateOrder.RemoveWorldFromCurrentPlayerLoop(world);
                world.Dispose();
            }

            _worldsToDispose.Clear();
            World.DefaultGameObjectInjectionWorld = _originalDefaultWorld;
        }

        protected World ReplaceDefaultWorld()
        {
            var world = new World(GetType().Name);
            DisposeOnTearDown(world);
            World.DefaultGameObjectInjectionWorld = world;
            return world;
        }

        protected void DisposeOnTearDown(World world)
        {
            if (world != null) _worldsToDispose.Add(world);
        }

        protected static int EntityCountOf<T>(World world) where T : unmanaged
            => world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<T>()).CalculateEntityCount();
    }
}
