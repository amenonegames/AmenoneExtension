using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using ViewAbstract;

namespace View.Option
{
    public class OptionDistributor: IOptionDistributable
    {
        private readonly ImmutableArray<IOptionRoot> _optionRoots;
        private readonly IOptionViewFactory _optionViewFactory;
        private readonly List<IOptionView> _activatedOptions = new();

        public OptionDistributor(IReadOnlyList<IOptionRoot> OptionRoots, IOptionViewFactory OptionViewFactory)
        {
            _optionRoots = OptionRoots.ToImmutableArray();
            _optionViewFactory = OptionViewFactory;
        }

        public UniTask DistributeOptionAsync(OptionViewContext context, CancellationToken cancellationToken)
        {
            var optionsData = context.Options;

            foreach (var optionData in optionsData)
            {
                var root = _optionRoots.FirstOrDefault(x => x.Key == optionData.MetaData.Key);
                if (root == null) continue;
                var optionView = _optionViewFactory.Create();
                optionView.OptionViewTransform.SetParent(root.Transform);
                optionView.OptionViewTransform.localPosition = Vector3.zero;
                optionView.Activate(optionData);
                _activatedOptions.Add(optionView);
            }

            return UniTask.CompletedTask;
        }

        public UniTask ClearOptionsAsync(int selectedIndex, CancellationToken cancellationToken)
        {
            foreach (var activatedOption in _activatedOptions)
            {
                activatedOption.Clear();
            }
            _activatedOptions.Clear();
            return UniTask.CompletedTask;
        }
    }
}