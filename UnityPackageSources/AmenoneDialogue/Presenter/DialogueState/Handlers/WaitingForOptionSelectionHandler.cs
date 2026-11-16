using System.Threading;
using Cysharp.Threading.Tasks;
using Identifier;
using LogicAbstract;
using MessagePipe;
using Presenter.Adapters;
using Presenter.DialogueState.Base;
using ViewAbstract;

namespace Presenter.DialogueState.Handlers
{
    public class WaitingForOptionSelectionHandler : DialogueStateHandlerBase<DialogueOptionContext>
    {
        public override Identifier.DialogueState State => Identifier.DialogueState.WaitingForOptionSelection;
        private readonly IAsyncPublisher<InputStateMessage> _inputStatePublisher;

        public WaitingForOptionSelectionHandler(IDialogueStateHolder stateHolder, DialogueStateQueue queue, IAsyncPublisher<InputStateMessage> InputStatePublisher) : base(stateHolder, queue) => _inputStatePublisher = InputStatePublisher;

        protected override UniTask OnEnterState(DialogueOptionContext context, CancellationToken token)
        {
            _inputStatePublisher.Publish(new InputStateMessage(InputState.WaitOptionSelection), token);
            return UniTask.CompletedTask;
        }
    }
}
