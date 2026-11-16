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
    internal class ClockworkCharShowAnimator : ICharShowAnimator
    {
        public TextVisualizeTag Tag => TextVisualizeTag.Clockwork;

        public void Register(TMP_Text textComponent, MotionSequenceBuilder sequence, CharData charData, int charIndex, float entranceStartTime)
        {
            var quarterDuration = charData.Duration / 4f;
            sequence.Join(LMotion.Create(Color.clear, Color.white, quarterDuration)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharColorTL(textComponent, charIndex, Color.clear));
            sequence.Join(LMotion.Create(Color.clear, Color.white, quarterDuration)
                .WithDelayHoldingStartValue(entranceStartTime + quarterDuration)
                .BindToTMPCharColorTR(textComponent, charIndex, Color.clear));
            sequence.Join(LMotion.Create(Color.clear, Color.white, quarterDuration)
                .WithDelayHoldingStartValue(entranceStartTime + quarterDuration * 2)
                .BindToTMPCharColorBR(textComponent, charIndex, Color.clear));
            sequence.Join(LMotion.Create(Color.clear, Color.white, quarterDuration)
                .WithDelayHoldingStartValue(entranceStartTime + quarterDuration * 3)
                .BindToTMPCharColorBL(textComponent, charIndex, Color.clear));
        }

        public float GetIntervalToNextCharEntrance(CharData charData) => charData.Duration;

        public UniTask OnTextFeed(CancellationToken token) => UniTask.CompletedTask;
    }
}
