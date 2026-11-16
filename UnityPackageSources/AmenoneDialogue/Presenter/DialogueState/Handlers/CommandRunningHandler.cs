using System.Threading;
using Cysharp.Threading.Tasks;
using Identifier;
using LogicAbstract;
using MessagePipe;
using Presenter.DialogueState.Base;

namespace Presenter.DialogueState.Handlers
{
    public class CommandRunningHandler : DialogueStateHandlerBase<DialogueCommandContext>
    {
        public override Identifier.DialogueState State => Identifier.DialogueState.CommandRunning;
        private readonly ICommandDispatcher _commandDispatcher;
        private readonly IAsyncPublisher<DialogueEventMessage> _dialogueEventPublisher;

        public CommandRunningHandler(IDialogueStateHolder stateHolder, DialogueStateQueue queue, ICommandDispatcher CommandDispatcher, IAsyncPublisher<DialogueEventMessage> DialogueEventPublisher) : base(stateHolder, queue)
        {
            _commandDispatcher = CommandDispatcher;
            _dialogueEventPublisher = DialogueEventPublisher;
        }

        protected override async UniTask OnEnterState(DialogueCommandContext context, CancellationToken token)
        {
            await _commandDispatcher.DispatchCommandAsync(context.CommandText, token);
            _dialogueEventPublisher.Publish(new DialogueEventMessage(DialogueEvent.CommandExecuteFinish), token);
        }
    }
}
