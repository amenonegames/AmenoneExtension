using Identifier;
using LogicAbstract;
using MessagePipe;
using VContainer;
using ViewAbstract;

namespace View.Button
{
    public class FastForwardButton : FullScreenButtonBase, ITextFastForwardButtonActivatable
    {
        private IAsyncPublisher<DialogueInterruptEventMessage> _interruptEventPublisher;

        [Inject]
        public void Construct(IAsyncPublisher<DialogueInterruptEventMessage> interruptEventPublisher)
        {
            _interruptEventPublisher = interruptEventPublisher;
        }


        protected override void Select()
        {
            _interruptEventPublisher.Publish(new DialogueInterruptEventMessage(DialogueInterruptEvent.FastForwardRequested));
        }
    }
}