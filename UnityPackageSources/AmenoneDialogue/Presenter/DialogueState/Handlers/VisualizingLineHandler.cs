using System.Threading;
using Cysharp.Threading.Tasks;
using Identifier;
using LogicAbstract;
using MessagePipe;
using Presenter.Adapters.Abstract;
using Presenter.DialogueState.Base;
using ViewAbstract;

namespace Presenter.DialogueState.Handlers
{
    public class VisualizingLineHandler : DialogueStateHandlerBase<DialogueLineContext>
    {
        public override Identifier.DialogueState State => Identifier.DialogueState.VisualizingLine;
        private readonly ILineDistributable _lineDistributable;
        private readonly ILineViewContextFactory _lineViewContextFactory;
        private readonly IAsyncPublisher<InputStateMessage> _inputStatePublisher;
        private readonly IAsyncPublisher<DialogueEventMessage> _dialogueEventPublisher;

        public VisualizingLineHandler(IDialogueStateHolder stateHolder, DialogueStateQueue queue, ILineDistributable LineDistributable, ILineViewContextFactory LineViewContextFactory, IAsyncPublisher<InputStateMessage> InputStatePublisher, IAsyncPublisher<DialogueEventMessage> DialogueEventPublisher) : base(stateHolder, queue)
        {
            _lineDistributable = LineDistributable;
            _lineViewContextFactory = LineViewContextFactory;
            _inputStatePublisher = InputStatePublisher;
            _dialogueEventPublisher = DialogueEventPublisher;
        }

        public override async UniTask HandleInterruptAsync(DialogueInterruptEvent interruptEvent, CancellationToken token)
        {
            if (interruptEvent == DialogueInterruptEvent.FastForwardRequested)
            {
                await _inputStatePublisher.PublishAsync(new InputStateMessage(InputState.NoAction), token);
                _lineDistributable.FastenLineAsync();
            }
        }

        protected override async UniTask OnEnterState(DialogueLineContext context, CancellationToken token)
        {
            var result = await _lineViewContextFactory.Create(context,token);
            await _lineDistributable.PreparedLineAsync(result, token);
            await _inputStatePublisher.PublishAsync(new InputStateMessage(InputState.OnlyFastForward), token);
            await _lineDistributable.DistributeLineAsync(result, token);
            _dialogueEventPublisher.Publish(new DialogueEventMessage(DialogueEvent.LineVisualizeFinish), token);
        }

    }
}
