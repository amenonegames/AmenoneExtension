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
    internal class PixelCharShowAnimator : ICharShowAnimator
    {
        private const float BlockRoomXEm = 0.5f;
        private const float BlockRoomYEm = 0.5f;
        private const float EntranceDurationRate = 3f;
        private const float AppearDurationRate = 0.02f;
        private const float EntranceIntervalRate = 0.25f;

        private const float PackedScaleSteps = 16f;
        private const float LargestQuadScale = 2000f;
        private const float PackedProgressStart = 0.01f;
        private const float PackedProgressEnd = 0.99f;

        public TextVisualizeTag Tag => TextVisualizeTag.Pixel;

        public void Register(TMP_Text textComponent, MotionSequenceBuilder sequence, CharData charData, int charIndex, float entranceStartTime)
        {
            var charInfo = textComponent.textInfo.characterInfo[charIndex];
            var quadSize = charInfo.vertex_TR.position - charInfo.vertex_BL.position;
            var quadScale = new Vector3(
                CalculateScaleToHoldBlocks(quadSize.x, BlockRoomXEm * charInfo.pointSize),
                CalculateScaleToHoldBlocks(quadSize.y, BlockRoomYEm * charInfo.pointSize),
                1f);

            var packedScale = new Vector2(quadScale.x, quadScale.y) * PackedScaleSteps;
            var packedSeed = Random.Range(0.001f, 0.999f);
            var blocksStart = packedScale + new Vector2(PackedProgressStart, packedSeed);
            var settled = packedScale + new Vector2(PackedProgressEnd, packedSeed);
            var duration = charData.Duration * EntranceDurationRate;

            sequence.Join(LMotion.Create(Color.clear, Color.white, charData.Duration * AppearDurationRate)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharColorCustom(textComponent, charIndex, Color.clear));
            sequence.Join(LMotion.Create(quadScale, quadScale, duration)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharScaleCustom(textComponent, charIndex, quadScale));
            sequence.Join(LMotion.Create(blocksStart, settled, duration)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharUv3Custom(textComponent, charIndex, blocksStart));
        }

        // ブロックは文字の矩形より外に出るので矩形を広げたままにする。シェーダーへ渡せるのは UV3 の 2 値だけなので、
        // 倍率を 1/16 刻みに丸めて整数部に置き、小数部に進行度（x）と文字ごとの乱数（y）を詰める。
        private static float CalculateScaleToHoldBlocks(float quadLength, float room)
        {
            var scale = 1f + 2f * room / Mathf.Max(quadLength, 0.0001f);
            return Mathf.Min(Mathf.Ceil(scale * PackedScaleSteps) / PackedScaleSteps, LargestQuadScale);
        }

        public float GetIntervalToNextCharEntrance(CharData charData) => charData.Duration * EntranceIntervalRate;

        public UniTask OnTextFeed(CancellationToken token) => UniTask.CompletedTask;
    }
}
