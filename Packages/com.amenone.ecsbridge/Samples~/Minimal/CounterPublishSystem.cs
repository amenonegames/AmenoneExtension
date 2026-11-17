using MessagePipe;
using Unity.Entities;

namespace Amenone.EcsBridge.Samples.Minimal
{
    [UpdateAfter(typeof(CounterSystem))]
    public partial class CounterPublishSystem : PublishSystem<IPublisher<CounterChangedMessage>>
    {
        private readonly LastPublishedValue<int> _lastValue = new();

        protected override void OnCreate() => RequireForUpdate<Counter>();

        protected override void OnPublisherInjected() => _lastValue.Clear();

        protected override void PublishChanges()
        {
            var value = SystemAPI.GetSingleton<Counter>().Value;
            if (_lastValue.TryUpdate(value)) Publisher.Publish(new CounterChangedMessage(value));
        }
    }
}
