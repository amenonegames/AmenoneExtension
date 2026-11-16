using VContainer.Unity;
using View.Interface;

namespace View
{
    public class LineInitializer : IInitializable
    {
        private ILineViewHolder _lineViewHolder;

        public LineInitializer(ILineViewHolder LineViewHolder) => _lineViewHolder = LineViewHolder;

        public void Initialize()
        {
            foreach (var lineView in _lineViewHolder.GetAllLineViews())
            {
                lineView.Initialize();
            }
        }
    }
}