namespace Amenone.ArchitectureGuard.Analyzers.Tests
{
    internal static class EcsBridgeStub
    {
        public const string Source = @"
using Unity.Entities;
using VContainer;

namespace Amenone.EcsBridge
{
    public abstract partial class PublishSystem<TPublisher> : SystemBase where TPublisher : class
    {
        protected TPublisher Publisher { get; private set; }

        [Inject]
        public void Construct(TPublisher publisher)
        {
            Publisher = publisher;
            OnPublisherInjected();
        }

        protected virtual void OnPublisherInjected()
        {
        }

        protected sealed override void OnUpdate()
        {
            if (Publisher == null) return;

            PublishChanges();
        }

        protected abstract void PublishChanges();
    }
}
";
    }
}
