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
    internal class RippleCharShowAnimator : ICharShowAnimator
    {
        private const float EntranceDurationScale = 2.6f;
        private const float FadeInDurationRatio = 0.04f;
        private const float EntranceIntervalRate = 0.35f;
        private const float RiseDurationRatio = 0.9f;
        private const float RiseDistanceEm = 0.05f;
        private const float HorizontalMarginEm = 0.4f;
        private const float VerticalMarginEm = 0.7f;
        private const float VerticalMarginGlyphHeights = 1.1f;

        public TextVisualizeTag Tag => TextVisualizeTag.Ripple;

        public void Register(TMP_Text textComponent, MotionSequenceBuilder sequence, CharData charData, int charIndex, float entranceStartTime)
        {
            var entranceDuration = charData.Duration * EntranceDurationScale;
            var charInfo = textComponent.textInfo.characterInfo[charIndex];

            sequence.Join(LMotion.Create(Color.clear, Color.white, entranceDuration * FadeInDurationRatio)
                .WithEase(Ease.OutQuad)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharColorCustom(textComponent, charIndex, Color.clear));

            // シェーダーは UV3 = (入場の残り, クアッド拡大率 - 1) から拡大分を打ち消して字形を元の大きさで描く。
            // スケールと UV3 は同じ時間・同じ Linear で動かし、毎フレームの拡大率を一致させる。
            var quadExpand = CalculateQuadExpand(charInfo);
            var submergedUv3 = new Vector2(1f, quadExpand - 1f);
            var expandedScale = new Vector3(quadExpand, quadExpand, 1f);
            sequence.Join(LMotion.Create(submergedUv3, Vector2.zero, entranceDuration)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharUv3Custom(textComponent, charIndex, submergedUv3));
            sequence.Join(LMotion.Create(expandedScale, Vector3.one, entranceDuration)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharScaleCustom(textComponent, charIndex, expandedScale));

            var sunkenPosition = Vector3.down * (RiseDistanceEm * charInfo.pointSize);
            sequence.Join(LMotion.Create(sunkenPosition, Vector3.zero, entranceDuration * RiseDurationRatio)
                .WithEase(Ease.OutCubic)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharPositionCustom(textComponent, charIndex, sunkenPosition));
        }

        private static float CalculateQuadExpand(TMP_CharacterInfo charInfo)
        {
            var em = charInfo.pointSize;
            var quadWidth = Mathf.Max(charInfo.topRight.x - charInfo.bottomLeft.x, Mathf.Epsilon);
            var quadHeight = Mathf.Max(charInfo.topRight.y - charInfo.bottomLeft.y, Mathf.Epsilon);
            var horizontalExpand = 1f + 2f * HorizontalMarginEm * em / quadWidth;
            var verticalExpand = 1f + 2f * (VerticalMarginGlyphHeights + VerticalMarginEm * em / quadHeight);
            return Mathf.Max(horizontalExpand, verticalExpand);
        }

        public float GetIntervalToNextCharEntrance(CharData charData) => charData.Duration * EntranceIntervalRate;

        public UniTask OnTextFeed(CancellationToken token) => UniTask.CompletedTask;
    }
}
