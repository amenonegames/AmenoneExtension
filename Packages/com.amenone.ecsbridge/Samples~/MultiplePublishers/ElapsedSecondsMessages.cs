namespace Amenone.EcsBridge.Samples.MultiplePublishers
{
    public readonly struct ElapsedSecondsChangedMessage
    {
        public ElapsedSecondsChangedMessage(int seconds) => Seconds = seconds;

        public int Seconds { get; }
    }

    // IBufferedSubscriber は、struct のメッセージだとまだ何も発行していなくても default を購読した時に渡すので、class にする
    public sealed class ParityChangedMessage
    {
        public ParityChangedMessage(bool isEven) => IsEven = isEven;

        public bool IsEven { get; }
    }
}
