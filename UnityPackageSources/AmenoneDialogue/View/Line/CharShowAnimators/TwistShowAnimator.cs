using System.Threading;
using amenone.litmotiontext;
using Cysharp.Threading.Tasks;
using Identifier;
using LitMotion;
using TMPro;
using UnityEngine;
using ViewAbstract;

namespace View.Line
{
    internal class TwistShowAnimator : ICharShowAnimator
    {
        public TextVisualizeTag Tag => TextVisualizeTag.Twist;

        public void Register(TMP_Text textComponent, MotionSequenceBuilder sequence, CharData charData, int charIndex, float entranceStartTime)
        {
            var initialScale = Vector3.one * 0.1f;
            sequence.Join(LMotion.Create(Color.clear, Color.white, charData.Duration)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharColorCustom(textComponent, charIndex, Color.clear));
            sequence.Join(LMotion.Create(Vector2.zero, Vector2.one, charData.Duration)
                .WithEase(Ease.InBounce)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharUv3Custom(textComponent, charIndex, Vector2.zero));
            sequence.Join(LMotion.Create(initialScale, Vector3.one, charData.Duration)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharScaleCustom(textComponent, charIndex, initialScale));
        }

        public float GetIntervalToNextCharEntrance(CharData charData) => charData.Duration;

        public UniTask OnTextFeed(CancellationToken token) => UniTask.CompletedTask;
    }
}
