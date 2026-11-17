using Unity.Entities;

namespace Amenone.EcsBridge.Tests
{
    public struct TestValue : IComponentData
    {
        public int Value;
    }

    public struct TestElement : IBufferElementData
    {
        public int Value;
    }
}
