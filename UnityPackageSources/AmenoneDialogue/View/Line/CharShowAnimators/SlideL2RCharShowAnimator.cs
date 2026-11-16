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
    internal class SlideL2RCharShowAnimator : ICharShowAnimator
    {
        public TextVisualizeTag Tag => TextVisualizeTag.SlideL2R;

        public void Register(TMP_Text textComponent, MotionSequenceBuilder sequence, CharData charData, int charIndex, float entranceStartTime)
        {
            var quarterDuration = charData.Duration / 4f;
            var initialCharPos = new Vector3(-10f, 0f, 0f);
            sequence.Join(LMotion.Create(Color.clear, Color.white, quarterDuration)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharColorTR(textComponent, charIndex, Color.clear));
            sequence.Join(LMotion.Create(Color.clear, Color.white, quarterDuration)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharColorBR(textComponent, charIndex, Color.clear));
            sequence.Join(LMotion.Create(Color.clear, Color.white, quarterDuration)
                .WithDelayHoldingStartValue(entranceStartTime + quarterDuration)
                .BindToTMPCharColorTL(textComponent, charIndex, Color.clear));
            sequence.Join(LMotion.Create(Color.clear, Color.white, quarterDuration)
                .WithDelayHoldingStartValue(entranceStartTime + quarterDuration)
                .BindToTMPCharColorBL(textComponent, charIndex, Color.clear));
            sequence.Join(LMotion.Create(initialCharPos, Vector3.zero, charData.Duration)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharPositionCustom(textComponent, charIndex, initialCharPos));
        }

        public float GetIntervalToNextCharEntrance(CharData charData) => charData.Duration;

        public UniTask OnTextFeed(CancellationToken token) => UniTask.CompletedTask;
    }
}
