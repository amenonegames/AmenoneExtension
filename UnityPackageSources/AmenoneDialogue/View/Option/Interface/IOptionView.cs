using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ViewAbstract
{
    public interface IOptionView
    {
        Transform OptionViewTransform { get; }
        public UniTask Activate(OptionViewData data);
        public UniTask Focus();
        public UniTask UnFocus();
        public UniTask Select();
        public UniTask Clear();
    }
}