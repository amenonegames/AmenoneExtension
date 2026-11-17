using System;
using Identifier;
using LogicAbstract;
using MessagePipe;
using Root.Abstract;
using UnityEngine;
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
        private readonly IAsyncPublisher<DialogueFailed> _failedPublisher;
        private readonly ITokenGettable _tokenGettable;

        public DialogueHolder(IDialogueFactory dialogueFactory,
            IAsyncPublisher<DialogueEventMessage> eventPublisher,
            IAsyncPublisher<DialogueFailed> failedPublisher,
            ITokenGettable TokenGettable)
        {
            _dialogue = dialogueFactory.Create();
            _eventPublisher = eventPublisher;
            _failedPublisher = failedPublisher;
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
        // 台本の誤り（<<jump>> 先の無いノードなど）で VM が投げると、NodeEnd は届かず対話が止まったままになる。
        // 例外をログに出し、止まった事を DialogueFailed で知らせる
        public void Continue()
        {
            try
            {
                _dialogue.Continue();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                _failedPublisher.Publish(new DialogueFailed(exception), _tokenGettable.Get());
            }
        }
    }
}