using System;
using Cysharp.Threading.Tasks;
using Identifier;
using LogicAbstract;
using MessagePipe;
using VContainer.Unity;
using ViewAbstract;

namespace Presenter.InputSwitch
{
    public class InputSwitchPresenter : IInitializable, IDisposable
    {
        private readonly ITextFeedButtonActivatable _textFeedButtonActivatable;
        private readonly ITextFastForwardButtonActivatable _textFastForwardButtonActivatable;
        private readonly IAsyncSubscriber<InputStateMessage> _inputStateSubscriber;
        private IDisposable _subscription;

        public InputSwitchPresenter(ITextFeedButtonActivatable TextFeedButtonActivatable, ITextFastForwardButtonActivatable TextFastForwardButtonActivatable, IAsyncSubscriber<InputStateMessage> InputStateSubscriber)
        {
            _textFeedButtonActivatable = TextFeedButtonActivatable;
            _textFastForwardButtonActivatable = TextFastForwardButtonActivatable;
            _inputStateSubscriber = InputStateSubscriber;
        }

        public void Initialize() => _subscription = _inputStateSubscriber.Subscribe((msg, _) => On(msg));

        public void Dispose() => _subscription?.Dispose();

        UniTask On(InputStateMessage msg)
        {
            switch (msg.inputState)
            {
                case InputState.OnlyFastForward:
                    _textFeedButtonActivatable.Deactivate();
                    _textFastForwardButtonActivatable.Activate();
                    break;
                case InputState.NoAction:
                    _textFeedButtonActivatable.Deactivate();
                    _textFastForwardButtonActivatable.Deactivate();
                    break;
                case InputState.WaitTextFeed:
                    _textFeedButtonActivatable.Activate();
                    _textFastForwardButtonActivatable.Deactivate();
                    break;
                case InputState.WaitOptionSelection:
                    _textFastForwardButtonActivatable.Deactivate();
                    _textFeedButtonActivatable.Deactivate();
                    break;
            }
            return UniTask.CompletedTask;
        }
    }
}