using System;
using System.Collections.Generic;
using Identifier;
using LogicAbstract;

namespace Presenter.DialogueState.Base
{
    public class DialogueEventHandlerRegistry
    {
        private readonly IDialogueStateHolder _stateHolder;
        private readonly Dictionary<Identifier.DialogueState, IDialogueStateHandler> _handlers = new();

        public DialogueEventHandlerRegistry(IDialogueStateHolder stateHolder)
        {
            _stateHolder = stateHolder;
        }

        public void Register(IDialogueStateHandler handler)
        {
            _handlers[handler.State] = handler;
        }

        public bool CanHandleInterrupt(DialogueInterruptEvent interruptEvent)
        {
            return _stateHolder.CanHandleInterrupt(interruptEvent);
        }

        public bool TryGetCurrentHandler(out IDialogueStateHandler handler)
        {
            return _handlers.TryGetValue(_stateHolder.CurrentState, out handler);
        }

        public bool TryGetHandlerForEvent(DialogueEvent ev, out IDialogueStateHandler handler)
        {
            if (_stateHolder.CanTransition(ev, out var nextState))
            {
                return _handlers.TryGetValue(nextState, out handler);
            }

            handler = null;
            return false;
        }

        public bool TryGetHandlerForEvent<TContext>(DialogueEvent ev, out IDialogueStateHandler<TContext> handler)
        {
            var canTransition = _stateHolder.CanTransition(ev, out var nextState);
            var isHandlerExist = _handlers.TryGetValue(nextState, out var baseHandler);
            if (canTransition &&
                isHandlerExist &&
                baseHandler is IDialogueStateHandler<TContext> typedHandler)
            {
                handler = typedHandler;
                return true;
            }

            handler = null;
            return false;
        }
    }
}
