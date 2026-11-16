using Identifier;
using LogicAbstract;

namespace Logic
{
    public class DialogueStateHolder : IDialogueStateHolder
    {
        public DialogueState CurrentState { get; private set; } = DialogueState.DialogueStop;

        public bool CanHandleInterrupt(DialogueInterruptEvent interruptEvent)
        {
            return (CurrentState, interruptEvent) switch
            {
                (DialogueState.VisualizingLine, DialogueInterruptEvent.FastForwardRequested) => true,
                _ => false
            };
        }

        public bool CanTransition(DialogueEvent trigger, out DialogueState nextState)
        {
            return (CurrentState, trigger) switch
            {
                (DialogueState.DialogueStop, DialogueEvent.StartDialogue) =>
                    ReturnWithNextState(out nextState, DialogueState.VMRunning),

                (DialogueState.VMRunning, DialogueEvent.LineVisualizeRequested) =>
                    ReturnWithNextState(out nextState, DialogueState.VisualizingLine),

                (DialogueState.VisualizingLine, DialogueEvent.LineVisualizeFinish) =>
                    ReturnWithNextState(out nextState, DialogueState.WaitingForTextFeed),

                (DialogueState.WaitingForTextFeed, DialogueEvent.TextFeedProvided) =>
                    ReturnWithNextState(out nextState, DialogueState.TextFeedHandling),

                (DialogueState.TextFeedHandling, DialogueEvent.TextFeedHandleFinish) =>
                    ReturnWithNextState(out nextState, DialogueState.VMRunning),

                (DialogueState.VMRunning, DialogueEvent.OptionsVisualizeRequested) =>
                    ReturnWithNextState(out nextState, DialogueState.VisualizingOptions),

                (DialogueState.VisualizingOptions, DialogueEvent.OptionsVisualizeFinish) =>
                    ReturnWithNextState(out nextState, DialogueState.WaitingForOptionSelection),

                (DialogueState.WaitingForOptionSelection, DialogueEvent.OptionSelected) =>
                    ReturnWithNextState(out nextState, DialogueState.SelectedOption),

                (DialogueState.SelectedOption, DialogueEvent.ContinueRequested) =>
                    ReturnWithNextState(out nextState, DialogueState.VMRunning),

                (DialogueState.VMRunning, DialogueEvent.CommandRunRequested) =>
                    ReturnWithNextState(out nextState, DialogueState.CommandRunning),

                (DialogueState.CommandRunning, DialogueEvent.CommandExecuteFinish) =>
                    ReturnWithNextState(out nextState, DialogueState.CommandComplete),

                (DialogueState.CommandComplete, DialogueEvent.ContinueRequested) =>
                    ReturnWithNextState(out nextState, DialogueState.VMRunning),

                (DialogueState.VMRunning, DialogueEvent.StopDialogue) =>
                    ReturnWithNextState(out nextState, DialogueState.DialogueStop),

                _ => ReturnWithNextState(out nextState, CurrentState, false)
            };
        }

        public void Transition(DialogueState state)
        {
            CurrentState = state;
        }

        private bool ReturnWithNextState(out DialogueState state, DialogueState value, bool result = true)
        {
            state = value;
            return result;
        }
    }
}
