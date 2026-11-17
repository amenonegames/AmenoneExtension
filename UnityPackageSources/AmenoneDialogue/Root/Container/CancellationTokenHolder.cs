using System;
using System.Threading;
using Root.Abstract;

namespace RootContainer
{
    public class CancellationTokenHolder : ITokenGettable, IDisposable
    {
        private readonly CancellationTokenSource _source = new CancellationTokenSource();
        // Dispose の後に Get() されても ObjectDisposedException にならないよう、作った時に取っておく
        private readonly CancellationToken _token;
        private bool _disposed;

        public CancellationTokenHolder() => _token = _source.Token;

        public CancellationToken Get() => _token;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try
            {
                _source.Cancel();
            }
            finally
            {
                _source.Dispose();
            }
        }
    }
}