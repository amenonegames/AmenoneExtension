using System.Threading;
using Cysharp.Threading.Tasks;
using Identifier;
using LogicAbstract;
using MessagePipe;
using Presenter.DialogueState.Base;
using ViewAbstract;

namespace Presenter.DialogueState.Handlers
{
    public class SelectedOptionHandler : DialogueStateHandlerBase<SelectOptionContext>
    {
        public override Identifier.DialogueState State => Identifier.DialogueState.SelectedOption;
        private readonly IOptionDistributable _optionDistributable;
        private readonly IDialogueOptionSelectedNotifier _optionSelectedNotifier;
        private readonly IAsyncPublisher<DialogueEventMessage> _dialogueEventPublisher;

        public SelectedOptionHandler(IDialogueStateHolder stateHolder, DialogueStateQueue queue, IOptionDistributable OptionDistributable, IDialogueOptionSelectedNotifier OptionSelectedNotifier, IAsyncPublisher<DialogueEventMessage> DialogueEventPublisher) : base(stateHolder, queue)
        {
            _optionDistributable = OptionDistributable;
            _optionSelectedNotifier = OptionSelectedNotifier;
            _dialogueEventPublisher = DialogueEventPublisher;
        }

        protected override async UniTask OnEnterState(SelectOptionContext context, CancellationToken token)
        {
            await _optionDistributable.ClearOptionsAsync(context.SelectdIndex, token);
            _optionSelectedNotifier.NotifyOptionSelected(context.SelectdIndex);
            _dialogueEventPublisher.Publish(new DialogueEventMessage(DialogueEvent.ContinueRequested));
        }
    }
}
