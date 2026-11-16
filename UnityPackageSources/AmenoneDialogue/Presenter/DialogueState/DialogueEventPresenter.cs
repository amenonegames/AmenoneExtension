using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LogicAbstract;
using MessagePipe;
using Presenter.DialogueState.Base;
using UnityEngine;
using VContainer.Unity;

namespace Presenter.DialogueState
{
    public class DialogueEventPresenter : IInitializable, IDisposable
    {
        private readonly DialogueEventHandlerRegistry _registry;
        private readonly IAsyncSubscriber<DialogueInterruptEventMessage> _interruptEventSubscriber;
        private readonly IAsyncSubscriber<DialogueEventMessage> _eventSubscriber;
        private readonly IAsyncSubscriber<DialogueLineEventMessage> _lineEventSubscriber;
        private readonly IAsyncSubscriber<DialogueOptionEventMessage> _optionEventSubscriber;
        private readonly IAsyncSubscriber<DialogueSelectOptionEventMessage> _selectOptionEventSubscriber;
        private readonly IAsyncSubscriber<DialogueCommandEventMessage> _commandEventSubscriber;
        private IDisposable _subscriptions;

        public DialogueEventPresenter(
            DialogueEventHandlerRegistry registry,
            IAsyncSubscriber<DialogueInterruptEventMessage> interruptEventSubscriber,
            IAsyncSubscriber<DialogueEventMessage> eventSubscriber,
            IAsyncSubscriber<DialogueLineEventMessage> lineEventSubscriber,
            IAsyncSubscriber<DialogueOptionEventMessage> optionEventSubscriber,
            IAsyncSubscriber<DialogueSelectOptionEventMessage> selectOptionEventSubscriber,
            IAsyncSubscriber<DialogueCommandEventMessage> commandEventSubscriber)
        {
            _registry = registry;
            _interruptEventSubscriber = interruptEventSubscriber;
            _eventSubscriber = eventSubscriber;
            _lineEventSubscriber = lineEventSubscriber;
            _optionEventSubscriber = optionEventSubscriber;
            _selectOptionEventSubscriber = selectOptionEventSubscriber;
            _commandEventSubscriber = commandEventSubscriber;
        }

        public void Initialize() => _subscriptions = DisposableBag.Create(
            _interruptEventSubscriber.Subscribe(OnAsync),
            _eventSubscriber.Subscribe(OnAsync),
            _lineEventSubscriber.Subscribe(OnAsync),
            _optionEventSubscriber.Subscribe(OnAsync),
            _selectOptionEventSubscriber.Subscribe(OnAsync),
            _commandEventSubscriber.Subscribe(OnAsync));

        public void Dispose() => _subscriptions?.Dispose();

        UniTask OnAsync(DialogueInterruptEventMessage cmd, CancellationToken token)
        {
            if (_registry.CanHandleInterrupt(cmd.interruptEvent) &&
                _registry.TryGetCurrentHandler(out var currentHandler))
            {
                return currentHandler.HandleInterruptAsync(cmd.interruptEvent, token);
            }

            Debug.LogWarning($"Cannot handle interrupt event {cmd.interruptEvent} in current state");
            return UniTask.CompletedTask;
        }

        UniTask OnAsync(DialogueEventMessage cmd, CancellationToken token)
        {
            if (_registry.TryGetHandlerForEvent(cmd.ev, out var handler))
            {
                return handler.HandleAsync(token);
            }

            Debug.LogWarning($"No handler registered for event {cmd.ev}");
            return UniTask.CompletedTask;
        }

        UniTask OnAsync(DialogueLineEventMessage cmd, CancellationToken token)
        {
            if (_registry.TryGetHandlerForEvent<DialogueLineContext>(cmd.ev, out var handler))
            {
                return handler.HandleAsync(cmd.Context, token);
            }

            Debug.LogWarning($"No handler registered for event {cmd.ev}");
            return UniTask.CompletedTask;
        }

        UniTask OnAsync(DialogueOptionEventMessage cmd, CancellationToken token)
        {
            if (_registry.TryGetHandlerForEvent<DialogueOptionContext>(cmd.ev, out var handler))
            {
                return handler.HandleAsync(cmd.Context, token);
            }

            Debug.LogWarning($"No handler registered for event {cmd.ev}");
            return UniTask.CompletedTask;
        }

        UniTask OnAsync(DialogueSelectOptionEventMessage cmd, CancellationToken token)
        {
            if (_registry.TryGetHandlerForEvent<SelectOptionContext>(cmd.ev, out var handler))
            {
                return handler.HandleAsync(cmd.Context, token);
            }

            Debug.LogWarning($"No handler registered for event {cmd.ev}");
            return UniTask.CompletedTask;
        }

        UniTask OnAsync(DialogueCommandEventMessage cmd, CancellationToken token)
        {
            if (_registry.TryGetHandlerForEvent<DialogueCommandContext>(cmd.ev, out var handler))
            {
                return handler.HandleAsync(cmd.Context, token);
            }

            Debug.LogWarning($"No handler registered for event {cmd.ev}");
            return UniTask.CompletedTask;
        }
    }
}
