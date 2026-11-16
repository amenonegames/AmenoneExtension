using System.Threading;
using Cysharp.Threading.Tasks;
using Identifier;
using LogicAbstract;
using MessagePipe;
using Presenter.DialogueState.Base;

namespace Presenter.DialogueState.Handlers
{
    public class CommandCompleteHandler : DialogueStateHandlerBase
    {
        public override Identifier.DialogueState State => Identifier.DialogueState.CommandComplete;
        private readonly IAsyncPublisher<DialogueEventMessage> _dialogueEventPublisher;

        public CommandCompleteHandler(IDialogueStateHolder stateHolder, DialogueStateQueue queue, IAsyncPublisher<DialogueEventMessage> DialogueEventPublisher) : base(stateHolder, queue) => _dialogueEventPublisher = DialogueEventPublisher;

        protected override UniTask OnEnterState(CancellationToken token)
        {
            _dialogueEventPublisher.Publish(new DialogueEventMessage(DialogueEvent.ContinueRequested), token);
            return UniTask.CompletedTask;
        }
    }
}
