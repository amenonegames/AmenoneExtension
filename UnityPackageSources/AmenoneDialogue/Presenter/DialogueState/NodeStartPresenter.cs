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
    public class NodeStartPresenter : IInitializable, IDisposable
    {
        private readonly DialogueStateQueue _queue;
        private readonly IAsyncSubscriber<NodeStart> _nodeStartSubscriber;
        private IDisposable _subscription;

        public NodeStartPresenter(DialogueStateQueue queue, IAsyncSubscriber<NodeStart> nodeStartSubscriber)
        {
            _queue = queue;
            _nodeStartSubscriber = nodeStartSubscriber;
        }

        public void Initialize() => _subscription = _nodeStartSubscriber.Subscribe(OnAsync);

        public void Dispose() => _subscription?.Dispose();

        UniTask OnAsync(NodeStart cmd, CancellationToken token)
        {
            return _queue.EnqueueAsync(async () =>
            {
                Debug.Log("NodeStart command received (async)");
                await UniTask.CompletedTask;
            }, token);
        }
    }
}