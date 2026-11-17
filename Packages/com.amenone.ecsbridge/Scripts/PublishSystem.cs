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

        // System は World に残ったままスコープだけが作り直されると、同じインスタンスに注入し直される。前回値はここで戻す
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
