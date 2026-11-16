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
    public class InitializeDialoguePresenter : IInitializable, IDisposable
    {
        private readonly DialogueStateQueue _queue;
        private readonly IAsyncSubscriber<InitializeDialogue> _initializeDialogueSubscriber;
        private IDisposable _subscription;

        public InitializeDialoguePresenter(DialogueStateQueue queue, IAsyncSubscriber<InitializeDialogue> initializeDialogueSubscriber)
        {
            _queue = queue;
            _initializeDialogueSubscriber = initializeDialogueSubscriber;
        }

        public void Initialize() => _subscription = _initializeDialogueSubscriber.Subscribe(OnAsync);

        public void Dispose() => _subscription?.Dispose();

        UniTask OnAsync(InitializeDialogue cmd, CancellationToken token)
        {
            return _queue.EnqueueAsync(async () =>
            {
                Debug.Log("InitializeDialogue command received (async)");
                await UniTask.CompletedTask;
            }, token);
        }
    }
}