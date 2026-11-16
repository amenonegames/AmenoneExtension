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
    internal class WispCharShowAnimator : ICharShowAnimator
    {
        private const float WispRoomXEm = 1.1f;
        private const float WispRoomYEm = 2.6f;
        private const float EntranceDurationRate = 3.2f;
        private const float AppearDurationRate = 0.02f;
        private const float EntranceIntervalRate = 0.3f;

        private const float PackedScaleSteps = 16f;
        private const float LargestQuadScale = 2000f;
        private const float PackedProgressStart = 0.01f;
        private const float PackedProgressEnd = 0.99f;
        private const float PackedLineMiddleRange = 64f;

        public TextVisualizeTag Tag => TextVisualizeTag.Wisp;

        public void Register(TMP_Text textComponent, MotionSequenceBuilder sequence, CharData charData, int charIndex, float entranceStartTime)
        {
            var charInfo = textComponent.textInfo.characterInfo[charIndex];
            var quadSize = charInfo.vertex_TR.position - charInfo.vertex_BL.position;
            var quadScale = new Vector3(
                CalculateScaleToHoldWisp(quadSize.x, WispRoomXEm * charInfo.pointSize),
                CalculateScaleToHoldWisp(quadSize.y, WispRoomYEm * charInfo.pointSize),
                1f);

            var packedScale = new Vector2(quadScale.x, quadScale.y) * PackedScaleSteps;
            var packedLineMiddle = PackLineMiddle(charInfo, quadSize.y);
            var wispStart = packedScale + new Vector2(PackedProgressStart, packedLineMiddle);
            var settled = packedScale + new Vector2(PackedProgressEnd, packedLineMiddle);
            var duration = charData.Duration * EntranceDurationRate;

            sequence.Join(LMotion.Create(Color.clear, Color.white, charData.Duration * AppearDurationRate)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharColorCustom(textComponent, charIndex, Color.clear));
            sequence.Join(LMotion.Create(quadScale, quadScale, duration)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharScaleCustom(textComponent, charIndex, quadScale));
            sequence.Join(LMotion.Create(wispStart, settled, duration)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharUv3Custom(textComponent, charIndex, wispStart));
        }

        // 筋は文字の矩形より外に出るので矩形を広げたままにする。シェーダーへ渡せるのは UV3 の 2 値だけなので、
        // 倍率を 1/16 刻みに丸めて整数部に置き、小数部に進行度（x）と行の中央までの距離（y）を詰める。
        private static float CalculateScaleToHoldWisp(float quadLength, float room)
        {
            var scale = 1f + 2f * room / Mathf.Max(quadLength, 0.0001f);
            return Mathf.Min(Mathf.Ceil(scale * PackedScaleSteps) / PackedScaleSteps, LargestQuadScale);
        }

        private static float PackLineMiddle(TMP_CharacterInfo charInfo, float quadHeight)
        {
            var lineMiddle = (charInfo.ascender + charInfo.descender) * 0.5f;
            var quadMiddle = (charInfo.vertex_TR.position.y + charInfo.vertex_BL.position.y) * 0.5f;
            var offsetInQuadHeights = (lineMiddle - quadMiddle) / Mathf.Max(quadHeight, 0.0001f);
            return Mathf.Clamp(offsetInQuadHeights / PackedLineMiddleRange + 0.5f, 0.001f, 0.999f);
        }

        public float GetIntervalToNextCharEntrance(CharData charData) => charData.Duration * EntranceIntervalRate;

        public UniTask OnTextFeed(CancellationToken token) => UniTask.CompletedTask;
    }
}
