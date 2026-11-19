using System;
using Unity.Core;
using Unity.Entities;
using VContainer.Unity;

namespace Amenone.EcsBridge
{
    public sealed class WorldClockDriver : IStartable, IPostTickable, IDisposable
    {
        private readonly IWorldDeltaTimeSource _deltaTimeSource;
        private World _world;

        public WorldClockDriver(IWorldDeltaTimeSource deltaTimeSource) => _deltaTimeSource = deltaTimeSource;

        public void Start()
        {
            _world = World.DefaultGameObjectInjectionWorld;
            ChangeUnityTimeDriving(false);
        }

        // VContainer の PostTick が、同じフレームの ECS の SimulationSystemGroup の更新より前に呼ばれる前提に頼っている
        public void PostTick()
        {
            if (_world is not { IsCreated: true }) return;

            var deltaSeconds = Math.Min(_deltaTimeSource.DeltaSeconds, _world.MaximumDeltaTime);
            _world.SetTime(new TimeData(_world.Time.ElapsedTime + deltaSeconds, deltaSeconds));
        }

        public void Dispose()
        {
            ChangeUnityTimeDriving(true);
            _world = null;
        }

        private void ChangeUnityTimeDriving(bool isEnabled)
        {
            if (_world is not { IsCreated: true }) return;

            var updateWorldTimeSystem = _world.GetExistingSystemManaged<UpdateWorldTimeSystem>();
            if (updateWorldTimeSystem != null) updateWorldTimeSystem.Enabled = isEnabled;
        }
    }
}
