using System.Threading;
using Cysharp.Threading.Tasks;
using LogicAbstract;

namespace Presenter.DialogueState.Base
{
    public abstract class DialogueStateHandlerBase : IDialogueStateHandler
    {
        protected readonly IDialogueStateHolder StateHolder;
        protected readonly DialogueStateQueue Queue;

        public abstract Identifier.DialogueState State { get; }

        protected DialogueStateHandlerBase(IDialogueStateHolder stateHolder, DialogueStateQueue queue)
        {
            StateHolder = stateHolder;
            Queue = queue;
        }

        public UniTask HandleAsync(CancellationToken token)
        {
            return Queue.EnqueueAsync(async queueToken =>
            {
                StateHolder.Transition(State);
                await OnEnterState(queueToken);
            }, token);
        }

        public virtual UniTask HandleInterruptAsync(Identifier.DialogueInterruptEvent interruptEvent, CancellationToken token)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask OnEnterState(CancellationToken token)
        {
            return UniTask.CompletedTask;
        }
    }

    public abstract class DialogueStateHandlerBase<TContext> : IDialogueStateHandler<TContext>
    {
        protected readonly IDialogueStateHolder StateHolder;
        protected readonly DialogueStateQueue Queue;

        public abstract Identifier.DialogueState State { get; }

        protected DialogueStateHandlerBase(IDialogueStateHolder stateHolder, DialogueStateQueue queue)
        {
            StateHolder = stateHolder;
            Queue = queue;
        }

        public UniTask HandleAsync(CancellationToken token)
        {
            return UniTask.CompletedTask;
        }

        public UniTask HandleAsync(TContext context, CancellationToken token)
        {
            return Queue.EnqueueAsync(async queueToken =>
            {
                StateHolder.Transition(State);
                await OnEnterState(context, queueToken);
            }, token);
        }

        public virtual UniTask HandleInterruptAsync(Identifier.DialogueInterruptEvent interruptEvent, CancellationToken token)
        {
            return UniTask.CompletedTask;
        }

        protected abstract UniTask OnEnterState(TContext context, CancellationToken token);
    }
}
