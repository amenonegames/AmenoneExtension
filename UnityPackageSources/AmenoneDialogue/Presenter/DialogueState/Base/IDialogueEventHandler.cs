using System.Threading;
using Cysharp.Threading.Tasks;
using Identifier;

namespace Presenter.DialogueState.Base
{
    public interface IDialogueEventHandler
    {
        DialogueEvent Event { get; }
        UniTask HandleAsync(CancellationToken token);
    }

    public interface IDialogueEventHandler<in TContext> : IDialogueEventHandler
    {
        UniTask HandleAsync(TContext context, CancellationToken token);
    }
}
