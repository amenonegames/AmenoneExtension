using Unity.Entities;

namespace Amenone.EcsBridge.Samples.Minimal
{
    public struct Counter : IComponentData
    {
        public int Value;
    }

    public struct CounterIncrementRequest : IComponentData
    {
        public int Amount;
    }
}
