using System.Threading;
using Cysharp.Threading.Tasks;
using View.Interface;
using ViewAbstract;

namespace View
{
    public class LineDistributor : ILineDistributable
    {
        private readonly ILineViewHolder _lineViewHolder;

        public LineDistributor(ILineViewHolder LineViewHolder) => _lineViewHolder = LineViewHolder;

        public async UniTask PreparedLineAsync(LineViewContext context, CancellationToken token)
        {
            var line = _lineViewHolder.GetLineView(context.MetaData.Key);
            _lineViewHolder.RegisterCurrentLineView(line);
            await line.PreparedLineAsync(context,token);
        }

        public UniTask DistributeLineAsync(LineViewContext Context, CancellationToken cancellationToken)
        {
            var line = _lineViewHolder.GetLineView(Context.MetaData.Key);
            return line.DistributeLineAsync(Context, cancellationToken);
        }

        public async UniTask OnTextFeedAsync(CancellationToken token)
        {
            var line = _lineViewHolder.GetCurrentLineView();
            await line.OnTextFeedAsync(token);
            _lineViewHolder.UnregisterCurrentLineView();
        }

        public void FastenLineAsync()
        {
            var line = _lineViewHolder.GetCurrentLineView();
            line.FastenLine();
        }
    }
}