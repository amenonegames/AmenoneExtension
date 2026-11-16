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
    internal class FlowerEaseCharShowAnimator : ICharShowAnimator
    {
        public TextVisualizeTag Tag => TextVisualizeTag.FlowerEase;

        public void Register(TMP_Text textComponent, MotionSequenceBuilder sequence, CharData charData, int charIndex, float entranceStartTime)
        {
            var initialCharScale = Vector3.one * 0.5f;
            sequence.Join(LMotion.Create(Color.clear, Color.white, charData.Duration)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharColorTR(textComponent, charIndex, Color.clear));
            sequence.Join(LMotion.Create(Color.clear, Color.white, charData.Duration)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharColorBR(textComponent, charIndex, Color.clear));
            sequence.Join(LMotion.Create(Color.clear, Color.white, charData.Duration)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharColorTL(textComponent, charIndex, Color.clear));
            sequence.Join(LMotion.Create(Color.clear, Color.white, charData.Duration)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharColorBL(textComponent, charIndex, Color.clear));
            sequence.Join(LMotion.Create(initialCharScale, Vector3.one, charData.Duration)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharScaleCustom(textComponent, charIndex, initialCharScale));

            var initialRotate = new Vector3(60f, 10f, 180f);
            sequence.Join(LMotion.Create(initialRotate, Vector3.zero, charData.Duration)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharEulerAnglesCustom(textComponent, charIndex, initialCharScale));

            var quarterDuration = charData.Duration / 4f;
            var fromUv3 = Vector2.zero;
            var toUv3 = Vector2.one;
            sequence.Join(LMotion.Create(fromUv3, toUv3, quarterDuration)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharUv3CustomBL(textComponent, charIndex, fromUv3));
            sequence.Join(LMotion.Create(fromUv3, toUv3, quarterDuration)
                .WithDelayHoldingStartValue(entranceStartTime + quarterDuration)
                .BindToTMPCharUv3CustomTL(textComponent, charIndex, fromUv3));
            sequence.Join(LMotion.Create(fromUv3, toUv3, quarterDuration)
                .WithDelayHoldingStartValue(entranceStartTime + quarterDuration * 2)
                .BindToTMPCharUv3CustomTR(textComponent, charIndex, fromUv3));
            sequence.Join(LMotion.Create(fromUv3, toUv3, quarterDuration)
                .WithDelayHoldingStartValue(entranceStartTime + quarterDuration * 3)
                .BindToTMPCharUv3CustomBR(textComponent, charIndex, fromUv3));

            var fromPos = new Vector3(-50f, -1f, 0f);
            sequence.Join(LMotion.Create(fromPos, Vector3.zero, quarterDuration)
                .WithDelayHoldingStartValue(entranceStartTime + quarterDuration)
                .BindToTMPCharPositionCustom(textComponent, charIndex, fromPos));
        }

        public float GetIntervalToNextCharEntrance(CharData charData) => charData.Duration;

        public UniTask OnTextFeed(CancellationToken token) => UniTask.CompletedTask;
    }
}
