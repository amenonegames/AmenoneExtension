using UnityEngine;
using UnityEngine.Serialization;
using VContainer;
using VContainer.Unity;
using View.Button;
using View.Interface;
using View.Line;
using View.Option;
using View.Utils;
using ViewAbstract;

namespace View.ContainerBuilder
{
    public class ViewContainerBuilder : LifetimeScope
    {
        [SerializeField] private Transform lineViewParent;
        [SerializeField] private Transform optionRootsParent;
        [SerializeField] private TextFeedButton textFeedButton;
        [SerializeField] private FastForwardButton fastForwardButton;
        [SerializeField] private OptionViewBase optionViewBasePrefab;
        [SerializeField] private TextVisualizePrefabIndex textVisualizePrefabIndex;
        [SerializeField] private StickyPullSettings stickyPullSettings = new();
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<LineViewHolder>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<LineDistributor>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<LineInitializer>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.RegisterComponent(textFeedButton).AsImplementedInterfaces();
            builder.RegisterComponent(fastForwardButton).AsImplementedInterfaces();
            builder.RegisterComponent(optionViewBasePrefab).AsImplementedInterfaces();
            builder.RegisterComponent(textVisualizePrefabIndex).AsSelf();

            builder.Register<OptionDistributor>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<OptionViewBase.OptionViewFactory>(Lifetime.Singleton).As<IOptionViewFactory>();

            var lines = lineViewParent.GetComponentsInChildren<ILineView>(true);
            builder.RegisterComponent(lines).AsSelf();
            var optionRoots = optionRootsParent.GetComponentsInChildren<OptionRoot>(true);
            foreach (var optionRoot in optionRoots)
            {
                builder.RegisterComponent(optionRoot).As<IOptionRoot>();
            }

            builder.Register<ClockworkCharShowAnimator>(Lifetime.Transient).AsImplementedInterfaces();
            builder.Register<DefaultCharShowAnimator>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<FallCharShowAnimator>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<FlowerEaseCharShowAnimator>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<SlideL2RCharShowAnimator>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<TwistShowAnimator>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<HalftoneCharShowAnimator>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<EchoCharShowAnimator>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<ScrambleCharShowAnimator>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<ScatterCharShowAnimator>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<PrismCharShowAnimator>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<WispCharShowAnimator>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<TangleCharShowAnimator>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<OversizeCharShowAnimator>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<SquiggleCharShowAnimator>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<PixelCharShowAnimator>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<StretchCharShowAnimator>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<FallSteppedCharShowAnimator>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<FlickerCharShowAnimator>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<GlitchCharShowAnimator>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<DefocusCharShowAnimator>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<HeatMapCharShowAnimator>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<RippleCharShowAnimator>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<MeltCharShowAnimator>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.RegisterInstance(stickyPullSettings);
            builder.Register<PointerPositionReader>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<StickyCharShowAnimator>(Lifetime.Singleton).AsImplementedInterfaces();
        }
    }
}