using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using View.Interface;
using ViewAbstract;

namespace View
{
    public class LineViewHolder : ILineViewHolder
    {
        private readonly ILineView[] _lineViews;
        private ILineView CurrentLineView { get; set; }

        public LineViewHolder(ILineView[] lineViews) => _lineViews = lineViews;

        public void RegisterCurrentLineView(ILineView lineView) => CurrentLineView = lineView;

        public void UnregisterCurrentLineView() => CurrentLineView = null;
        public ImmutableArray<ILineView> GetAllLineViews() => _lineViews.ToImmutableArray();

        public ILineView GetLineView(LineViewKey lineViewKey)
        {
            ILineView defaultLine = null;
            foreach (var line in _lineViews)
            {
                if (line.LineViewKey == lineViewKey) return line;
                if (line.LineViewKey == LineViewKey.Default) defaultLine = line;
            }
            return defaultLine;
        }

        public ILineView GetCurrentLineView() => CurrentLineView;

        public (ILineView match , ImmutableArray<ILineView> unMatch) GetLineViewExcept(LineViewKey lineViewKey)
        {
            var match = _lineViews.FirstOrDefault(x => x.LineViewKey == lineViewKey);
            var unMatch = _lineViews.Where(x => x.LineViewKey != lineViewKey).ToImmutableArray();
            return (match, unMatch);
        }
    }
}