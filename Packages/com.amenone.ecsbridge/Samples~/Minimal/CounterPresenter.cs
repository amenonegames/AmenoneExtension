using System;
using MessagePipe;
using VContainer.Unity;

namespace Amenone.EcsBridge.Samples.Minimal
{
    public sealed class CounterPresenter : IStartable, IDisposable
    {
        private readonly CounterView _view;
        private readonly CounterIncrementSender _sender;
        private readonly ISubscriber<CounterChangedMessage> _subscriber;
        private IDisposable _subscription;

        public CounterPresenter(CounterView view, CounterIncrementSender sender, ISubscriber<CounterChangedMessage> subscriber)
        {
            _view = view;
            _sender = sender;
            _subscriber = subscriber;
        }

        public void Start()
        {
            _view.IncrementClicked += SendIncrement;
            _subscription = _subscriber.Subscribe(message => _view.Show(message.Value));
        }

        public void Dispose()
        {
            _view.IncrementClicked -= SendIncrement;
            _subscription?.Dispose();
        }

        private void SendIncrement() => _sender.Send(1);
    }
}
