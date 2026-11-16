using System;
using LogicAbstract;
using MessagePipe;
using VContainer;

namespace RootContainer
{
    public static class DialogueMessagingExtensions
    {
        private const string BrokersMissingMessage =
            "親のスコープで RegisterMessagePipe は呼ばれていますが、対話ライブラリのメッセージのブローカーが登録されていません。"
            + "RegisterMessagePipe を呼んでいる所で、その戻り値の options を渡して builder.RegisterDialogueMessageBrokers(options); を 1 行足してください。";

        public static void RegisterDialogueMessaging(this IContainerBuilder builder)
        {
            if (IsRegisteredInScopes<IAsyncPublisher<NodeStart>>(builder)) return;
            if (IsRegisteredInScopes<MessagePipeOptions>(builder)) throw new InvalidOperationException(BrokersMissingMessage);

            var options = builder.RegisterMessagePipe();
            builder.RegisterDialogueMessageBrokers(options);
        }

        public static void RegisterDialogueMessageBrokers(this IContainerBuilder builder, MessagePipeOptions options)
        {
            if (options.InstanceLifetime != InstanceLifetime.Singleton)
                throw new InvalidOperationException(NonSingletonLifetimeMessage(options.InstanceLifetime));

            builder.RegisterMessageBroker<InitializeDialogue>(options);
            builder.RegisterMessageBroker<NodeStart>(options);
            builder.RegisterMessageBroker<NodeEnd>(options);
            builder.RegisterMessageBroker<DialogueEventMessage>(options);
            builder.RegisterMessageBroker<DialogueInterruptEventMessage>(options);
            builder.RegisterMessageBroker<DialogueLineEventMessage>(options);
            builder.RegisterMessageBroker<DialogueOptionEventMessage>(options);
            builder.RegisterMessageBroker<DialogueSelectOptionEventMessage>(options);
            builder.RegisterMessageBroker<DialogueCommandEventMessage>(options);
            builder.RegisterMessageBroker<InputStateMessage>(options);
        }

        private static bool IsRegisteredInScopes<T>(IContainerBuilder builder) =>
            builder.Exists(typeof(T), includeInterfaceTypes: true, findParentScopes: true);

        private static string NonSingletonLifetimeMessage(InstanceLifetime lifetime) =>
            $"RegisterDialogueMessageBrokers には InstanceLifetime が Singleton の MessagePipeOptions を渡してください（今は {lifetime}）。"
            + "Singleton 以外だと、対話ライブラリの Logic・View・Presenter のスコープごとに別々のブローカーができ、発行したメッセージが購読側に届きません。";
    }
}
