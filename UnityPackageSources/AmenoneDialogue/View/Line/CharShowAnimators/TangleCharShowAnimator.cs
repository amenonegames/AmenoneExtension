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
    internal class TangleCharShowAnimator : ICharShowAnimator
    {
        private const float TangleRoomXEm = 2.1f;
        private const float TangleRoomYEm = 2.9f;
        private const float EntranceDurationRate = 6f;
        private const float AppearDurationRate = 0.02f;
        private const float EntranceIntervalRate = 0.25f;

        private const float PackedScaleSteps = 16f;
        private const float LargestQuadScale = 2000f;
        private const float PackedProgressStart = 0.01f;
        private const float PackedProgressEnd = 0.99f;

        public TextVisualizeTag Tag => TextVisualizeTag.Tangle;

        public void Register(TMP_Text textComponent, MotionSequenceBuilder sequence, CharData charData, int charIndex, float entranceStartTime)
        {
            var charInfo = textComponent.textInfo.characterInfo[charIndex];
            var quadSize = charInfo.vertex_TR.position - charInfo.vertex_BL.position;
            var quadScale = new Vector3(
                CalculateScaleToHoldTangle(quadSize.x, TangleRoomXEm * charInfo.pointSize),
                CalculateScaleToHoldTangle(quadSize.y, TangleRoomYEm * charInfo.pointSize),
                1f);

            var packedScale = new Vector2(quadScale.x, quadScale.y) * PackedScaleSteps;
            var packedSeed = Random.Range(0.001f, 0.999f);
            var tangled = packedScale + new Vector2(PackedProgressStart, packedSeed);
            var settled = packedScale + new Vector2(PackedProgressEnd, packedSeed);
            var duration = charData.Duration * EntranceDurationRate;

            sequence.Join(LMotion.Create(Color.clear, Color.white, charData.Duration * AppearDurationRate)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharColorCustom(textComponent, charIndex, Color.clear));
            sequence.Join(LMotion.Create(quadScale, quadScale, duration)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharScaleCustom(textComponent, charIndex, quadScale));
            sequence.Join(LMotion.Create(tangled, settled, duration)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharUv3Custom(textComponent, charIndex, tangled));
        }

        // 字は定位置から離れた所に描かれるので矩形を広げたままにする。シェーダーへ渡せるのは UV3 の 2 値だけなので、
        // 倍率を 1/16 刻みに丸めて整数部に置き、小数部に進行度（x）と文字ごとの乱数（y）を詰める。
        private static float CalculateScaleToHoldTangle(float quadLength, float room)
        {
            var scale = 1f + 2f * room / Mathf.Max(quadLength, 0.0001f);
            return Mathf.Min(Mathf.Ceil(scale * PackedScaleSteps) / PackedScaleSteps, LargestQuadScale);
        }

        public float GetIntervalToNextCharEntrance(CharData charData) => charData.Duration * EntranceIntervalRate;

        public UniTask OnTextFeed(CancellationToken token) => UniTask.CompletedTask;
    }
}
