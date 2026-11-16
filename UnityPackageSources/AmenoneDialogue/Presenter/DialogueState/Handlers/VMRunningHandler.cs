using System.Threading;
using Cysharp.Threading.Tasks;
using Identifier;
using LogicAbstract;
using Presenter.DialogueState.Base;

namespace Presenter.DialogueState.Handlers
{
    public class VMRunningHandler : DialogueStateHandlerBase
    {
        public override Identifier.DialogueState State => Identifier.DialogueState.VMRunning;
        private readonly IDialogueContinuable _commandPublisher;

        public VMRunningHandler(IDialogueStateHolder stateHolder, DialogueStateQueue queue, IDialogueContinuable CommandPublisher) : base(stateHolder, queue) => _commandPublisher = CommandPublisher;
        protected override UniTask OnEnterState(CancellationToken token)
        {
            _commandPublisher.Continue();
            return UniTask.CompletedTask;
        }
    }
}
