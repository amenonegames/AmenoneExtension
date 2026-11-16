using UnityEngine;
using VContainer;
using VContainer.Unity;
using ViewAbstract;

namespace RootContainer
{
    public class RootContainerBuilder : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterDialogueMessaging();

            builder.Register<CancellationTokenHolder>(Lifetime.Singleton)
                .AsImplementedInterfaces();
            builder.Register<LineDisplayExtensions>(Lifetime.Singleton);
        }
    }
}