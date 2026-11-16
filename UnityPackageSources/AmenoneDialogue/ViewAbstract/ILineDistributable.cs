using System.Threading;
using Cysharp.Threading.Tasks;

namespace ViewAbstract;

public interface ILineDistributable
{
    public UniTask PreparedLineAsync(LineViewContext context,CancellationToken token);
    public UniTask DistributeLineAsync(LineViewContext context, CancellationToken token);
    public void FastenLineAsync();
    UniTask OnTextFeedAsync(CancellationToken token);
}