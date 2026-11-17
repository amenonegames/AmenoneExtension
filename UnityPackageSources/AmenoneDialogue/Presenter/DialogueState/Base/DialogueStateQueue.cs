using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Root.Abstract;
using VContainer;

namespace Presenter.DialogueState.Base
{
    public class DialogueStateQueue : IDisposable
    {
        // 破棄の時に実行中の action が残っていても Release できるよう、セマフォ自体は Dispose しない
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly CancellationTokenSource _lifetimeSource = new CancellationTokenSource();
        private readonly CancellationToken _lifetimeToken;
        private readonly ITokenGettable _rootTokenGettable;
        private bool _disposed;

        public DialogueStateQueue() => _lifetimeToken = _lifetimeSource.Token;

        [Inject]
        public DialogueStateQueue(ITokenGettable rootTokenGettable) : this() => _rootTokenGettable = rootTokenGettable;

        private CancellationToken RootToken => _rootTokenGettable?.Get() ?? default;

        public UniTask EnqueueAsync(Func<UniTask> action, CancellationToken cancellationToken = default)
            => EnqueueAsync(_ => action(), cancellationToken);

        public UniTask<T> EnqueueAsync<T>(Func<UniTask<T>> func, CancellationToken cancellationToken = default)
            => EnqueueAsync<T>(_ => func(), cancellationToken);

        public async UniTask EnqueueAsync(Func<CancellationToken, UniTask> action, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                using var actionTokenSource = LinkActionTokenSource(cancellationToken);
                if (actionTokenSource.IsCancellationRequested) return;
                await action(actionTokenSource.Token);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async UniTask<T> EnqueueAsync<T>(Func<CancellationToken, UniTask<T>> func, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                using var actionTokenSource = LinkActionTokenSource(cancellationToken);
                actionTokenSource.Token.ThrowIfCancellationRequested();
                return await func(actionTokenSource.Token);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private CancellationTokenSource LinkActionTokenSource(CancellationToken callerToken)
            => CancellationTokenSource.CreateLinkedTokenSource(_lifetimeToken, RootToken, callerToken);

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(DialogueStateQueue));
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try
            {
                _lifetimeSource.Cancel();
            }
            finally
            {
                _lifetimeSource.Dispose();
            }
        }
    }
}
