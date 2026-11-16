using Identifier;
using LogicAbstract;
using MessagePipe;
using VContainer;
using ViewAbstract;

namespace View.Button
{
    public class TextFeedButton : FullScreenButtonBase , ITextFeedButtonActivatable
    {
        private IAsyncPublisher<DialogueEventMessage> _dialogueEventPublisher;
        [Inject]
        public void Construct(IAsyncPublisher<DialogueEventMessage> dialogueEventPublisher)
        {
            _dialogueEventPublisher = dialogueEventPublisher;
        }


        protected override void Select()
        {
            _dialogueEventPublisher.Publish(new DialogueEventMessage(DialogueEvent.TextFeedProvided));
        }
    }
}