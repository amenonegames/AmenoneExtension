using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Amenone.EcsBridge.Samples.MultiplePublishers
{
    public sealed class ElapsedSecondsLifetimeScope : LifetimeScope
    {
        [SerializeField] private ElapsedSecondsView _view;

        protected override void Configure(IContainerBuilder builder)
        {
            var options = builder.RegisterMessagePipe();
            builder.RegisterMessageBroker<ElapsedSecondsChangedMessage>(options);
            builder.RegisterMessageBroker<ParityChangedMessage>(options);
            builder.Register<ElapsedSecondsPublishers>(Lifetime.Singleton);
            builder.RegisterSystemFromDefaultWorld<ElapsedSecondsPublishSystem>();
            builder.RegisterComponent(_view);
            builder.RegisterEntryPoint<ElapsedSecondsPresenter>();
        }
    }
}
