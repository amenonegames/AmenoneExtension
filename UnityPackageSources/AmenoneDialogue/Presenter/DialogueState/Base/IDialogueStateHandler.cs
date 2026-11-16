using System.Threading;
using Cysharp.Threading.Tasks;

namespace Presenter.DialogueState.Base
{
    public interface IDialogueStateHandler
    {
        Identifier.DialogueState State { get; }
        UniTask HandleAsync(CancellationToken token);
        UniTask HandleInterruptAsync(Identifier.DialogueInterruptEvent interruptEvent, CancellationToken token);
    }

    public interface IDialogueStateHandler<in TContext> : IDialogueStateHandler
    {
        UniTask HandleAsync(TContext context, CancellationToken token);
    }
}
