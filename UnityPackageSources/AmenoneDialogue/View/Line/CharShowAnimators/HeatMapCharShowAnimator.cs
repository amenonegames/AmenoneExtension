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
    internal class HeatMapCharShowAnimator : ICharShowAnimator
    {
        private const float BleedMarginEm = 2f;
        private const float FadeInDurationRate = 0.12f;
        private const float EntranceDurationRate = 2.4f;
        private const float IntervalToNextCharRate = 0.4f;
        private const Ease CoolingEase = Ease.OutSine;

        public TextVisualizeTag Tag => TextVisualizeTag.HeatMap;

        public void Register(TMP_Text textComponent, MotionSequenceBuilder sequence, CharData charData, int charIndex, float entranceStartTime)
        {
            var quadScale = CalculateQuadScaleToHoldBleed(textComponent, charIndex);
            var expandedScale = new Vector3(quadScale, quadScale, 1f);
            var initialProgressAndQuadScale = new Vector2(0f, quadScale);
            var entranceDuration = charData.Duration * EntranceDurationRate;

            sequence.Join(LMotion.Create(Color.clear, Color.white, charData.Duration * FadeInDurationRate)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharColorCustom(textComponent, charIndex, Color.clear));
            sequence.Join(LMotion.Create(expandedScale, Vector3.one, entranceDuration)
                .WithEase(CoolingEase)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharScaleCustom(textComponent, charIndex, expandedScale));
            sequence.Join(LMotion.Create(initialProgressAndQuadScale, Vector2.one, entranceDuration)
                .WithEase(CoolingEase)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharUv3Custom(textComponent, charIndex, initialProgressAndQuadScale));
        }

        // 滲みは文字の矩形より外へ広がるので、入場中だけ矩形を広げる。シェーダーは同じ倍率を UV3.y で受け取り、字形を元の大きさに保つ。
        private static float CalculateQuadScaleToHoldBleed(TMP_Text textComponent, int charIndex)
        {
            var charInfo = textComponent.textInfo.characterInfo[charIndex];
            var quadSize = charInfo.vertex_TR.position - charInfo.vertex_BL.position;
            var shorterSide = Mathf.Max(Mathf.Min(quadSize.x, quadSize.y), 0.0001f);
            return 1f + 2f * BleedMarginEm * charInfo.pointSize / shorterSide;
        }

        public float GetIntervalToNextCharEntrance(CharData charData) => charData.Duration * IntervalToNextCharRate;

        public UniTask OnTextFeed(CancellationToken token) => UniTask.CompletedTask;
    }
}
