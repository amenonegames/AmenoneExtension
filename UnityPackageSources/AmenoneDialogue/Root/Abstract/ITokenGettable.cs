using System.Threading;

namespace Root.Abstract
{
    public interface ITokenGettable
    {
        CancellationToken Get();
    }
}