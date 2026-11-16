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
    public class NodeEndPresenter : IInitializable, IDisposable
    {
        private readonly DialogueStateQueue _queue;
        private readonly IAsyncSubscriber<NodeEnd> _nodeEndSubscriber;
        private IDisposable _subscription;

        public NodeEndPresenter(DialogueStateQueue queue, IAsyncSubscriber<NodeEnd> nodeEndSubscriber)
        {
            _queue = queue;
            _nodeEndSubscriber = nodeEndSubscriber;
        }

        public void Initialize() => _subscription = _nodeEndSubscriber.Subscribe(OnAsync);

        public void Dispose() => _subscription?.Dispose();

        UniTask OnAsync(NodeEnd cmd, CancellationToken token)
        {
            return _queue.EnqueueAsync(async () =>
            {
                Debug.Log("NodeEnd command received (async)");
                await UniTask.CompletedTask;
            }, token);
        }
    }
}