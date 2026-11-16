using System.Threading;
using Cysharp.Threading.Tasks;
using Identifier;
using LitMotion;
using TMPro;
using ViewAbstract;

namespace View.Line
{
    internal interface ICharShowAnimator
    {
        public TextVisualizeTag Tag { get; }
        public void Register(TMP_Text textComponent, MotionSequenceBuilder sequence, CharData charData, int charIndex, float entranceStartTime);
        public float GetIntervalToNextCharEntrance(CharData charData);
        public UniTask OnTextFeed(CancellationToken token);
    }
}
