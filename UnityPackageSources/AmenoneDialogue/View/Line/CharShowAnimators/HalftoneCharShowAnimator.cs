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
    internal class HalftoneCharShowAnimator : ICharShowAnimator
    {
        private const float GrowDurationRate = 2.6f;
        private const float AppearDurationRate = 0.05f;
        private const float IntervalToNextCharRate = 0.35f;

        public TextVisualizeTag Tag => TextVisualizeTag.Halftone;

        public void Register(TMP_Text textComponent, MotionSequenceBuilder sequence, CharData charData, int charIndex, float entranceStartTime)
        {
            sequence.Join(LMotion.Create(Color.clear, Color.white, charData.Duration * AppearDurationRate)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharColorCustom(textComponent, charIndex, Color.clear));
            var emSize = textComponent.textInfo.characterInfo[charIndex].pointSize;
            var noDots = new Vector2(0f, emSize);
            var fusedDots = new Vector2(1f, emSize);
            sequence.Join(LMotion.Create(noDots, fusedDots, charData.Duration * GrowDurationRate)
                .WithEase(Ease.InOutSine)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharUv3Custom(textComponent, charIndex, noDots));
        }

        public float GetIntervalToNextCharEntrance(CharData charData) => charData.Duration * IntervalToNextCharRate;

        public UniTask OnTextFeed(CancellationToken token) => UniTask.CompletedTask;
    }
}
