using LogicAbstract;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;
using VContainer.Unity;
using Yarn;
using Yarn.Unity;
using IActionRegistration = LogicAbstract.IActionRegistration;

namespace Logic
{
    public sealed class LogicContainerBuilder : LifetimeScope
    {
        [SerializeField] private YarnProject yarnProject;
        [Tooltip("外から IDialogueStartable.Start を呼んで始める時は外す")]
        [SerializeField] private bool startsAutomatically = true;

        protected override void Configure(IContainerBuilder builder)
        {
            if (startsAutomatically) builder.RegisterEntryPoint<EntryPoint>();

            builder.RegisterComponent(yarnProject);
            builder.Register<DialogueFactory>(Lifetime.Singleton).As<IDialogueFactory>();

            builder.Register<DialogueHolder>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<VariableStorage>(Lifetime.Scoped).As<IVariableStorage>();
            // 子のスコープから解決しても、このスコープの入れ物（対話が読むもの）に書くよう Singleton にする
            builder.Register<DialogueVariableWriter>(Lifetime.Singleton).As<IDialogueVariableWriter>();

            builder.Register<ActionRegistration>(Lifetime.Scoped).As<IActionRegistration>();

            builder.Register<TextSpeedHandler>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<DialogueStateHolder>(Lifetime.Singleton).AsImplementedInterfaces();

            builder.Register<CommandDispatcher>(resolver =>
            {
                var dispatcher = new CommandDispatcher();
                var defaultCommands = new DefaultYarnCommands();
                dispatcher.RegisterCommandsFromInstance(defaultCommands);
                return dispatcher;
            }, Lifetime.Singleton).AsImplementedInterfaces().AsSelf();

        }

    }
}