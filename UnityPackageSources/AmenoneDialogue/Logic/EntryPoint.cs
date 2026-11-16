using LogicAbstract;
using VContainer.Unity;

namespace Logic
{
    public class EntryPoint : IStartable
    {
        private readonly IDialogueStartable _dialogueStartable;

        public EntryPoint(IDialogueStartable DialogueStartable) => _dialogueStartable = DialogueStartable;

        public void Start()
        {
            _dialogueStartable.Start();
        }
    }
}