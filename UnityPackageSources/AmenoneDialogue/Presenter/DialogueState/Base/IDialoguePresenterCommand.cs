using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LogicAbstract;

namespace Presenter.DialogueState.Base
{
    public interface IDialoguePresenterCommand<TContext>
    where TContext : IDialogueContext
    {
        UniTask ExecuteAsync(CancellationToken token, TContext context = default);
    }
}