using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Presenter.DialogueState.Base
{
    public class DialogueStateQueue : IDisposable
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private bool _disposed;

        public async UniTask EnqueueAsync(Func<UniTask> action, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            await _semaphore.WaitAsync(cancellationToken);
            try
            {
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

        public void Dispose()
        {
            if (!_disposed)
            {
                _semaphore?.Dispose();
                _disposed = true;
            }
        }
    }
}