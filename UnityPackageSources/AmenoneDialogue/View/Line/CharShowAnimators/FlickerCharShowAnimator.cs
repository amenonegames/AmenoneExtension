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
    internal class FlickerCharShowAnimator : ICharShowAnimator
    {
        private const float EntranceDurationScale = 2.4f;
        private const float AppearDurationRatio = 0.005f;
        private const float StreakMarginEm = 6f;
        private const float GlowMarginEm = 0.5f;
        private const float IntervalToNextCharRate = 0.3f;

        public TextVisualizeTag Tag => TextVisualizeTag.Flicker;

        public void Register(TMP_Text textComponent, MotionSequenceBuilder sequence, CharData charData, int charIndex, float entranceStartTime)
        {
            var entranceDuration = charData.Duration * EntranceDurationScale;
            var charInfo = textComponent.textInfo.characterInfo[charIndex];

            sequence.Join(LMotion.Create(Color.clear, Color.white, entranceDuration * AppearDurationRatio)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharColorCustom(textComponent, charIndex, Color.clear));

            // シェーダーは UV3 = (入場の残り, クアッドの縦の拡大率 - 1) から拡大分を打ち消して字形を元の大きさで描く（横の拡大率はクアッドの縦横比から求める）。
            // スケールと UV3 は同じ時間・同じ Linear で動かし、毎フレームの拡大率を一致させる。
            var quadExpand = CalculateQuadExpand(charInfo);
            var unlitUv3 = new Vector2(1f, quadExpand.y - 1f);
            sequence.Join(LMotion.Create(unlitUv3, Vector2.zero, entranceDuration)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharUv3Custom(textComponent, charIndex, unlitUv3));
            sequence.Join(LMotion.Create(quadExpand, Vector3.one, entranceDuration)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharScaleCustom(textComponent, charIndex, quadExpand));
        }

        private static Vector3 CalculateQuadExpand(TMP_CharacterInfo charInfo)
        {
            var em = charInfo.pointSize;
            var quadWidth = Mathf.Max(charInfo.topRight.x - charInfo.bottomLeft.x, Mathf.Epsilon);
            var quadHeight = Mathf.Max(charInfo.topRight.y - charInfo.bottomLeft.y, Mathf.Epsilon);
            return new Vector3(
                1f + 2f * GlowMarginEm * em / quadWidth,
                1f + 2f * StreakMarginEm * em / quadHeight,
                1f);
        }

        public float GetIntervalToNextCharEntrance(CharData charData) => charData.Duration * IntervalToNextCharRate;

        public UniTask OnTextFeed(CancellationToken token) => UniTask.CompletedTask;
    }
}
