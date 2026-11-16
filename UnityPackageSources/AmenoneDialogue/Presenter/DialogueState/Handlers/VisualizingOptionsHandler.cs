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
    public class VisualizingOptionsHandler : DialogueStateHandlerBase<DialogueOptionContext>
    {
        public override Identifier.DialogueState State => Identifier.DialogueState.VisualizingOptions;
        private readonly IOptionViewContextFactory _optionViewContextFactory;
        private readonly IOptionDistributable _optionDistributable;
        private readonly IAsyncPublisher<DialogueOptionEventMessage> _optionEventPublisher;

        public VisualizingOptionsHandler(IDialogueStateHolder stateHolder, DialogueStateQueue queue, IOptionViewContextFactory OptionViewContextFactory, IOptionDistributable OptionDistributable, IAsyncPublisher<DialogueOptionEventMessage> OptionEventPublisher) : base(stateHolder, queue)
        {
            _optionViewContextFactory = OptionViewContextFactory;
            _optionDistributable = OptionDistributable;
            _optionEventPublisher = OptionEventPublisher;
        }

        protected override async UniTask OnEnterState(DialogueOptionContext context, CancellationToken token)
        {
            var optionViewContext = await _optionViewContextFactory.Create(context,token);
            await _optionDistributable.DistributeOptionAsync(optionViewContext,token);
            _optionEventPublisher.Publish(new DialogueOptionEventMessage(DialogueEvent.OptionsVisualizeFinish,context), token);
        }
    }
}
