using System.Threading;
using Cysharp.Threading.Tasks;
using LogicAbstract;
using ViewAbstract;

namespace Presenter.Adapters.Abstract
{

    public interface IOptionViewContextFactory
    {
        public UniTask<OptionViewContext> Create(DialogueOptionContext logicContext,CancellationToken token);
    }

    public interface ILineViewContextFactory
    {
        public UniTask<LineViewContext> Create(DialogueLineContext logicContext , CancellationToken token);
    }

}
