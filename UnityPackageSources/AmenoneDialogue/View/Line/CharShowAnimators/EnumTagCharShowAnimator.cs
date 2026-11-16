using System.Threading;
using Cysharp.Threading.Tasks;
using FastEnumUtility;
using LitMotion;
using TMPro;
using ViewAbstract;

namespace View.Line
{
    internal sealed class EnumTagCharShowAnimator : IMarkupCharShowAnimator
    {
        private readonly ICharShowAnimator _animator;

        public EnumTagCharShowAnimator(ICharShowAnimator animator)
        {
            _animator = animator;
            MarkupName = FastEnum.GetName(animator.Tag);
        }

        public string MarkupName { get; }

        public void Register(TMP_Text textComponent, MotionSequenceBuilder sequence, CharData charData, int charIndex, float entranceStartTime)
        {
            _animator.Register(textComponent, sequence, charData, charIndex, entranceStartTime);
        }

        public float GetIntervalToNextCharEntrance(CharData charData) => _animator.GetIntervalToNextCharEntrance(charData);

        public UniTask OnTextFeed(CancellationToken token) => _animator.OnTextFeed(token);
    }
}
