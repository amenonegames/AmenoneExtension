using System.Threading;
using Root.Abstract;

namespace RootContainer
{
    public class CancellationTokenHolder : ITokenGettable
    {
        private CancellationToken _cancellationToken;
        public CancellationToken Get()
        {
            return _cancellationToken;
        }
    }
}