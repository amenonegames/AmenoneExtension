namespace Amenone.EcsBridge.Samples.Minimal
{
    public readonly struct CounterChangedMessage
    {
        public CounterChangedMessage(int value) => Value = value;

        public int Value { get; }
    }
}
