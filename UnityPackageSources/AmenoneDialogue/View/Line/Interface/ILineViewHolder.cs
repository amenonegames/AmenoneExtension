using System.Collections.Immutable;
using ViewAbstract;

namespace View.Interface
{
    public interface ILineViewHolder
    {
        public void RegisterCurrentLineView(ILineView lineView);
        public void UnregisterCurrentLineView();
        public ImmutableArray<ILineView> GetAllLineViews();
        public ILineView GetLineView(LineViewKey lineViewKey);
        public ILineView GetCurrentLineView();
        public (ILineView match, ImmutableArray<ILineView> unMatch) GetLineViewExcept(LineViewKey lineViewKey);
    }
}