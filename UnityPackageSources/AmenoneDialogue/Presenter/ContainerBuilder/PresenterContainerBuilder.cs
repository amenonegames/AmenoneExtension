using LogicAbstract;
using Presenter.Adapters;
using Presenter.DialogueState.Base;
using Presenter.DialogueState.Handlers;
using VContainer;
using VContainer.Unity;

namespace Presenter
{
    public class PresenterContainerBuilder : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterSubscribingPresenters();

            builder.Register<LineViewContextFactory>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<OptionViewContextFactory>(Lifetime.Singleton).AsImplementedInterfaces();

            #if FORCE_YARNLOCALIZATION
            builder.Register<YarnLocalizationLineProvider>(Lifetime.Singleton)
                .AsImplementedInterfaces();
            #else
            builder.Register<UniyLocalizationLineProvider>(Lifetime.Singleton)
                .AsImplementedInterfaces();
            #endif

            builder.Register<LineTagConverter>(Lifetime.Singleton).AsSelf();
            builder.Register<OptionTagConverter>(Lifetime.Singleton).AsSelf();
            builder.Register<LineTextParser>(Lifetime.Singleton).AsSelf();

            builder.Register<DialogueStateQueue>(Lifetime.Singleton).AsSelf();

            builder.Register<VMRunningHandler>(Lifetime.Singleton).AsSelf();
            builder.Register<VisualizingLineHandler>(Lifetime.Singleton).AsSelf();
            builder.Register<WaitingForTextFeedHandler>(Lifetime.Singleton).AsSelf();
            builder.Register<VisualizingOptionsHandler>(Lifetime.Singleton).AsSelf();
            builder.Register<WaitingForOptionSelectionHandler>(Lifetime.Singleton).AsSelf();
            builder.Register<SelectedOptionHandler>(Lifetime.Singleton).AsSelf();
            builder.Register<CommandRunningHandler>(Lifetime.Singleton).AsSelf();
            builder.Register<CommandCompleteHandler>(Lifetime.Singleton).AsSelf();
            builder.Register<StopDialogueHandler>(Lifetime.Singleton).AsSelf();
            builder.Register<TextVisualizeTagParseInfoHolder>(Lifetime.Singleton).AsSelf();
            builder.Register<TextFeedRequestHandler>(Lifetime.Singleton).AsSelf();

            builder.Register<DialogueEventHandlerRegistry>(registry =>
            {
                var r = new DialogueEventHandlerRegistry(registry.Resolve<IDialogueStateHolder>());
                r.Register(registry.Resolve<VMRunningHandler>());
                r.Register(registry.Resolve<VisualizingLineHandler>());
                r.Register(registry.Resolve<WaitingForTextFeedHandler>());
                r.Register(registry.Resolve<VisualizingOptionsHandler>());
                r.Register(registry.Resolve<WaitingForOptionSelectionHandler>());
                r.Register(registry.Resolve<SelectedOptionHandler>());
                r.Register(registry.Resolve<CommandRunningHandler>());
                r.Register(registry.Resolve<CommandCompleteHandler>());
                r.Register(registry.Resolve<StopDialogueHandler>());
                r.Register(registry.Resolve<TextFeedRequestHandler>());
                return r;
            }, Lifetime.Singleton);
        }
    }
}