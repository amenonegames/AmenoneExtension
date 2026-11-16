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
    internal class EchoCharShowAnimator : ICharShowAnimator
    {
        private const float EchoRoomEm = 1.2f;
        private const float ConvergeDurationRate = 1.6f;
        private const float AppearDurationRate = 0.05f;
        private const float EntranceIntervalRate = 0.3f;

        public TextVisualizeTag Tag => TextVisualizeTag.Echo;

        public void Register(TMP_Text textComponent, MotionSequenceBuilder sequence, CharData charData, int charIndex, float entranceStartTime)
        {
            var quadScale = CalculateQuadScaleToHoldEchoes(textComponent, charIndex);
            var expandedScale = new Vector3(quadScale, quadScale, 1f);
            var separated = new Vector2(0f, quadScale);
            var converged = new Vector2(1f, quadScale);

            sequence.Join(LMotion.Create(Color.clear, Color.white, charData.Duration * AppearDurationRate)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharColorCustom(textComponent, charIndex, Color.clear));
            sequence.Join(LMotion.Create(expandedScale, expandedScale, charData.Duration * ConvergeDurationRate)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharScaleCustom(textComponent, charIndex, expandedScale));
            sequence.Join(LMotion.Create(separated, converged, charData.Duration * ConvergeDurationRate)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharUv3Custom(textComponent, charIndex, separated));
        }

        // 残像と光は文字の矩形より外に出るので矩形を広げたままにする。シェーダーは同じ倍率を UV3.y で受け取り、字形を元の大きさに保つ。
        private static float CalculateQuadScaleToHoldEchoes(TMP_Text textComponent, int charIndex)
        {
            var charInfo = textComponent.textInfo.characterInfo[charIndex];
            var quadSize = charInfo.vertex_TR.position - charInfo.vertex_BL.position;
            var shorterSide = Mathf.Max(Mathf.Min(quadSize.x, quadSize.y), 0.0001f);
            return 1f + 2f * EchoRoomEm * charInfo.pointSize / shorterSide;
        }

        public float GetIntervalToNextCharEntrance(CharData charData) => charData.Duration * EntranceIntervalRate;

        public UniTask OnTextFeed(CancellationToken token) => UniTask.CompletedTask;
    }
}
