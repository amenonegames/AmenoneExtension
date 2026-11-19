using MessagePipe;

namespace Amenone.EcsBridge.Samples.MultiplePublishers
{
    public sealed class ElapsedSecondsPublishers
    {
        public readonly IPublisher<ElapsedSecondsChangedMessage> Seconds;
        public readonly IBufferedPublisher<ParityChangedMessage> Parity;

        public ElapsedSecondsPublishers(IPublisher<ElapsedSecondsChangedMessage> seconds, IBufferedPublisher<ParityChangedMessage> parity)
        {
            Seconds = seconds;
            Parity = parity;
        }
    }
}
