using Unity.Entities;

namespace Amenone.EcsBridge.Samples.Minimal
{
    public partial struct CounterSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.EntityManager.CreateSingleton(new Counter());
            state.RequireForUpdate<CounterIncrementRequest>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var request = SystemAPI.GetSingletonRW<CounterIncrementRequest>();
            if (request.ValueRO.Amount == 0) return;

            SystemAPI.GetSingletonRW<Counter>().ValueRW.Value += request.ValueRO.Amount;
            request.ValueRW.Amount = 0;
        }
    }
}
