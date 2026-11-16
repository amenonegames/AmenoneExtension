using System.Threading;
using Cysharp.Threading.Tasks;
using Identifier;
using LogicAbstract;
using MessagePipe;
using Presenter.DialogueState.Base;
using ViewAbstract;

namespace Presenter.DialogueState.Handlers
{
    public class WaitingForTextFeedHandler : DialogueStateHandlerBase
    {
        public override Identifier.DialogueState State => Identifier.DialogueState.WaitingForTextFeed;
        private readonly IAsyncPublisher<InputStateMessage> _inputStatePublisher;
        private readonly ILineDistributable _lineDistributable;

        public WaitingForTextFeedHandler(IDialogueStateHolder stateHolder, DialogueStateQueue queue, IAsyncPublisher<InputStateMessage> InputStatePublisher, ILineDistributable LineDistributable) : base(stateHolder, queue)
        {
            _inputStatePublisher = InputStatePublisher;
            _lineDistributable = LineDistributable;
        }

        protected override async UniTask OnEnterState(CancellationToken token)
        {
            await _inputStatePublisher.PublishAsync(new InputStateMessage(InputState.WaitTextFeed), token);
        }
    }
}
