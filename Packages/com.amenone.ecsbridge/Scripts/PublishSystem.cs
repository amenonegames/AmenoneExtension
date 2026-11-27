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

        // System は World に残ったままスコープだけが作り直されると、同じインスタンスにもう一度注入されるので、前回値の扱いはここで決める
        protected abstract void OnPublisherInjected();

        protected sealed override void OnUpdate()
        {
            if (Publisher == null) return;

            PublishChanges();
        }

        protected abstract void PublishChanges();
    }
}
