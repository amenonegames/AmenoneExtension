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
    internal class GlitchCharShowAnimator : ICharShowAnimator
    {
        private const float QuadStretchX = 5f;
        private const float EncodedProgressSlotWidth = 2f;
        private const float EncodedProgressOffsetInSlot = 0.5f;

        public TextVisualizeTag Tag => TextVisualizeTag.Glitch;

        public void Register(TMP_Text textComponent, MotionSequenceBuilder sequence, CharData charData, int charIndex, float entranceStartTime)
        {
            sequence.Join(LMotion.Create(Color.white, Color.white, charData.Duration)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharColorCustom(textComponent, charIndex, Color.white));

            // TMP_SDF_Glitch.shader が UV3 の x から文字ごとの乱数の種と進行度を、y から帯が尾を引くためのクアッド横拡大率を取り出す
            var hiddenUv3 = new Vector2(charIndex * EncodedProgressSlotWidth + EncodedProgressOffsetInSlot, QuadStretchX);
            var settledUv3 = hiddenUv3 + Vector2.right;
            sequence.Join(LMotion.Create(hiddenUv3, settledUv3, charData.Duration)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharUv3Custom(textComponent, charIndex, hiddenUv3));

            var stretchedScale = new Vector3(QuadStretchX, 1f, 1f);
            sequence.Join(LMotion.Create(stretchedScale, stretchedScale, charData.Duration)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharScaleCustom(textComponent, charIndex, stretchedScale));
        }

        public float GetIntervalToNextCharEntrance(CharData charData) => charData.Duration;

        public UniTask OnTextFeed(CancellationToken token) => UniTask.CompletedTask;
    }
}
