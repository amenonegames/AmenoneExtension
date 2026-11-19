using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Amenone.EcsBridge.Samples.Minimal
{
    public sealed class CounterLifetimeScope : LifetimeScope
    {
        [SerializeField] private CounterView _view;

        protected override void Configure(IContainerBuilder builder)
        {
            var options = builder.RegisterMessagePipe();
            builder.RegisterMessageBroker<CounterChangedMessage>(options);
            builder.Register<CounterIncrementSender>(Lifetime.Singleton);
            builder.RegisterSystemFromDefaultWorld<CounterPublishSystem>();
            builder.RegisterComponent(_view);
            builder.RegisterEntryPoint<CounterPresenter>();
        }
    }
}
