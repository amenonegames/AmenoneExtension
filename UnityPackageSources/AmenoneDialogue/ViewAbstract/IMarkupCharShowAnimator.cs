using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using TMPro;

namespace ViewAbstract
{
    public interface IMarkupCharShowAnimator
    {
        string MarkupName { get; }
        void Register(TMP_Text textComponent, MotionSequenceBuilder sequence, CharData charData, int charIndex, float entranceStartTime);
        float GetIntervalToNextCharEntrance(CharData charData);
        UniTask OnTextFeed(CancellationToken token);
    }
}
