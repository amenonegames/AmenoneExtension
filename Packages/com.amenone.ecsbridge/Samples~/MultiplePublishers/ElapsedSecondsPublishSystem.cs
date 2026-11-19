using Unity.Entities;

namespace Amenone.EcsBridge.Samples.MultiplePublishers
{
    [UpdateAfter(typeof(ElapsedSecondsSystem))]
    public partial class ElapsedSecondsPublishSystem : PublishSystem<ElapsedSecondsPublishers>
    {
        private readonly LastPublishedValue<int> _lastSeconds = new();
        private readonly LastPublishedValue<bool> _lastIsEven = new();

        protected override void OnCreate() => RequireForUpdate<ElapsedSeconds>();

        protected override void OnPublisherInjected()
        {
            _lastSeconds.Clear();
            _lastIsEven.Clear();
        }

        protected override void PublishChanges()
        {
            var seconds = SystemAPI.GetSingleton<ElapsedSeconds>().Value;
            if (_lastSeconds.TryUpdate(seconds)) Publisher.Seconds.Publish(new ElapsedSecondsChangedMessage(seconds));

            var isEven = seconds % 2 == 0;
            if (_lastIsEven.TryUpdate(isEven)) Publisher.Parity.Publish(new ParityChangedMessage(isEven));
        }
    }
}
