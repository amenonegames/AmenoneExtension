using System.Threading;
using Cysharp.Threading.Tasks;

namespace ViewAbstract
{
    public interface IOptionDistributable
    {
        public UniTask DistributeOptionAsync(OptionViewContext context, CancellationToken cancellationToken);
        public UniTask ClearOptionsAsync(int selectedIndex ,CancellationToken cancellationToken);
    }
}