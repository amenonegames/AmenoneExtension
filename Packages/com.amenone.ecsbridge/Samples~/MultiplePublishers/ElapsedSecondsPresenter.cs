using System;
using MessagePipe;
using VContainer.Unity;

namespace Amenone.EcsBridge.Samples.MultiplePublishers
{
    public sealed class ElapsedSecondsPresenter : IStartable, IDisposable
    {
        private readonly ElapsedSecondsView _view;
        private readonly ISubscriber<ElapsedSecondsChangedMessage> _secondsSubscriber;
        private readonly IBufferedSubscriber<ParityChangedMessage> _paritySubscriber;
        private IDisposable _subscriptions;

        public ElapsedSecondsPresenter(
            ElapsedSecondsView view,
            ISubscriber<ElapsedSecondsChangedMessage> secondsSubscriber,
            IBufferedSubscriber<ParityChangedMessage> paritySubscriber)
        {
            _view = view;
            _secondsSubscriber = secondsSubscriber;
            _paritySubscriber = paritySubscriber;
        }

        public void Start() => _subscriptions = DisposableBag.Create(
            _secondsSubscriber.Subscribe(message => _view.ShowSeconds(message.Seconds)),
            _paritySubscriber.Subscribe(message => _view.ShowParity(message.IsEven)));

        public void Dispose() => _subscriptions?.Dispose();
    }
}
