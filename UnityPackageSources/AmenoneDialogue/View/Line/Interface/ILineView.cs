using System.Threading;
using Cysharp.Threading.Tasks;
using ViewAbstract;

namespace View.Interface
{
    public interface ILineView
    {
        internal LineViewKey LineViewKey { get; }
        public void Initialize();
        public UniTask PreparedLineAsync(LineViewContext context,CancellationToken token);
        public UniTask DistributeLineAsync(LineViewContext context, CancellationToken cancellationToken);
        public void FastenLine();
        public UniTask OnTextFeedAsync(CancellationToken token);
    }
}