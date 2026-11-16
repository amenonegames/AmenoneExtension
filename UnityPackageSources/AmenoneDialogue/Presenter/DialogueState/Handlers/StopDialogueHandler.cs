using Identifier;
using LogicAbstract;
using Presenter.DialogueState.Base;

namespace Presenter.DialogueState.Handlers
{
    public class StopDialogueHandler : DialogueStateHandlerBase
    {
        public override Identifier.DialogueState State => Identifier.DialogueState.DialogueStop;

        public StopDialogueHandler(IDialogueStateHolder stateHolder, DialogueStateQueue queue)
            : base(stateHolder, queue)
        {
        }
    }
}
