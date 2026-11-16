using System.Threading;
using Cysharp.Threading.Tasks;
using Identifier;
using LogicAbstract;
using MessagePipe;
using Presenter.DialogueState.Base;
using ViewAbstract;

namespace Presenter.DialogueState.Handlers
{
    public class TextFeedRequestHandler : DialogueStateHandlerBase
    {
        public override Identifier.DialogueState State => Identifier.DialogueState.TextFeedHandling;
        private readonly IAsyncPublisher<InputStateMessage> _inputStatePublisher;
        private readonly IAsyncPublisher<DialogueEventMessage> _dialogueEventPublisher;
        private readonly ILineDistributable _lineDistributable;

        public TextFeedRequestHandler(IDialogueStateHolder stateHolder, DialogueStateQueue queue, IAsyncPublisher<InputStateMessage> InputStatePublisher, IAsyncPublisher<DialogueEventMessage> DialogueEventPublisher, ILineDistributable LineDistributable) : base(stateHolder, queue)
        {
            _inputStatePublisher = InputStatePublisher;
            _dialogueEventPublisher = DialogueEventPublisher;
            _lineDistributable = LineDistributable;
        }

        protected override async UniTask OnEnterState(CancellationToken token)
        {
            await _inputStatePublisher.PublishAsync(new InputStateMessage(InputState.NoAction), token);
            await _lineDistributable.OnTextFeedAsync(token);
            _dialogueEventPublisher.Publish(new DialogueEventMessage(DialogueEvent.TextFeedHandleFinish), token);
        }
    }
}
