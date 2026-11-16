using Identifier;

namespace LogicAbstract
{
    public interface IDialogueStateHolder
    {
        DialogueState CurrentState { get; }
        bool CanTransition(DialogueEvent trigger, out DialogueState nextState);
        bool CanHandleInterrupt(DialogueInterruptEvent interruptEvent);
        void Transition(DialogueState state);
    }
}
