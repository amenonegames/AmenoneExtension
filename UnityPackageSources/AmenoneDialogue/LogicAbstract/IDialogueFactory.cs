using Yarn;

namespace LogicAbstract
{
    public interface IDialogueFactory
    {
        Dialogue Create();
    }
    public interface IDialogueStartable
    {
        void Start();
        void Start(string nodeName);
        void Stop();
    }
    public interface IDialogueNodeLookup
    {
        bool NodeExists(string nodeName);
    }
    public interface IDialogueContinuable
    {
        void Continue();
    }
    public interface IDialogueOptionSelectedNotifier
    {
        void NotifyOptionSelected(int optionIndex);
    }
}