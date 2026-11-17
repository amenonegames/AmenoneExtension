using Unity.Entities;

namespace Amenone.EcsBridge
{
    // Entity を消すだけでは、System が持つ状態と World の時刻の Entity が残るので、World ごと作り直す
    public sealed class DefaultWorldReset : IDefaultWorldReset
    {
        private const string DefaultWorldName = "Default World";

        public void Reset()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world is { IsCreated: true })
            {
                ScriptBehaviourUpdateOrder.RemoveWorldFromCurrentPlayerLoop(world);
                world.Dispose();
            }

            DefaultWorldInitialization.Initialize(DefaultWorldName);
        }
    }
}
