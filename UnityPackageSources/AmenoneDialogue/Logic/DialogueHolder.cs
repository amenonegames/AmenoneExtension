using Identifier;
using LogicAbstract;
using MessagePipe;
using Root.Abstract;
using Yarn;

namespace Logic
{
    public class DialogueHolder :
        IDialogueStartable , IDialogueOptionSelectedNotifier
        ,IDialogueContinuable, IDialogueNodeLookup
    {
        private const string DefaultStartNodeName = "START";

        private readonly Dialogue _dialogue;
        private readonly IAsyncPublisher<DialogueEventMessage> _eventPublisher;
        private readonly ITokenGettable _tokenGettable;

        public DialogueHolder(IDialogueFactory dialogueFactory,
            IAsyncPublisher<DialogueEventMessage> eventPublisher,
            ITokenGettable TokenGettable)
        {
            _dialogue = dialogueFactory.Create();
            _eventPublisher = eventPublisher;
            _tokenGettable = TokenGettable;
        }

        public void Start() => Start(DefaultStartNodeName);

        public void Start(string nodeName)
        {
            _dialogue.SetNode(nodeName);
            // 最初の Continue は、StartDialogue を受けて VMRunning に入った状態機械が呼ぶ
            _eventPublisher.Publish(new DialogueEventMessage(DialogueEvent.StartDialogue),_tokenGettable.Get());
        }

        public void Stop() => _dialogue.Stop();

        public bool NodeExists(string nodeName) => _dialogue.NodeExists(nodeName);

        public void NotifyOptionSelected(int optionIndex) => _dialogue.SetSelectedOption(optionIndex);
        public void Continue() => _dialogue.Continue();
    }
}