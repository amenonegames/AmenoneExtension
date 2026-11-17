using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Presenter.DialogueState.Base
{
    public class DialogueStateQueue : IDisposable
    {
        // 破棄の時に実行中の action が残っていても Release できるよう、セマフォ自体は Dispose しない
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private bool _disposed;

        public async UniTask EnqueueAsync(Func<UniTask> action, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                if (_disposed) return;
                await action();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async UniTask<T> EnqueueAsync<T>(Func<UniTask<T>> func, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                if (_disposed) throw new OperationCanceledException();
                return await func();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(DialogueStateQueue));
            }
        }

        public void Dispose() => _disposed = true;
    }
}
