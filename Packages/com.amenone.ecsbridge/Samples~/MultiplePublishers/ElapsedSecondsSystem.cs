using Unity.Entities;

namespace Amenone.EcsBridge.Samples.MultiplePublishers
{
    public partial struct ElapsedSecondsSystem : ISystem
    {
        public void OnCreate(ref SystemState state) => state.EntityManager.CreateSingleton(new ElapsedSeconds());

        public void OnUpdate(ref SystemState state) => SystemAPI.GetSingletonRW<ElapsedSeconds>().ValueRW.Value = (int)SystemAPI.Time.ElapsedTime;
    }
}
