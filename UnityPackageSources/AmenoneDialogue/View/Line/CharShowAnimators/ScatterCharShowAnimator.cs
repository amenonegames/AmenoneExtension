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
    internal class ScatterCharShowAnimator : ICharShowAnimator
    {
        private const float NearestStartEm = 1.1f;
        private const float FarthestStartEm = 2.3f;
        private const float TailPerDistance = 1.3f;
        private const float TailRoomEm = 3.2f;
        private const float GatherDurationRate = 2.6f;
        private const float AppearDurationRate = 0.05f;
        private const float EntranceIntervalRate = 0.3f;
        private const Ease GatherEase = Ease.OutCubic;

        private bool _evenCharsStartAbove = true;

        public TextVisualizeTag Tag => TextVisualizeTag.Scatter;

        public void Register(TMP_Text textComponent, MotionSequenceBuilder sequence, CharData charData, int charIndex, float entranceStartTime)
        {
            var charInfo = textComponent.textInfo.characterInfo[charIndex];
            var startsAbove = (charIndex % 2 == 0) == _evenCharsStartAbove;
            var startEm = Random.Range(NearestStartEm, FarthestStartEm) * (startsAbove ? 1f : -1f);
            var startOffset = new Vector3(0f, startEm * charInfo.pointSize, 0f);

            var quadStretch = CalculateVerticalQuadStretchToHoldTail(charInfo);
            var stretchedScale = new Vector3(1f, 1f + quadStretch, 1f);
            var trailing = new Vector2(startEm * TailPerDistance, quadStretch);
            var settled = new Vector2(0f, quadStretch);
            var duration = charData.Duration * GatherDurationRate;

            sequence.Join(LMotion.Create(Color.clear, Color.white, charData.Duration * AppearDurationRate)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharColorCustom(textComponent, charIndex, Color.clear));
            sequence.Join(LMotion.Create(stretchedScale, stretchedScale, duration)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharScaleCustom(textComponent, charIndex, stretchedScale));
            sequence.Join(LMotion.Create(startOffset, Vector3.zero, duration)
                .WithEase(GatherEase)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharPositionCustom(textComponent, charIndex, startOffset));
            sequence.Join(LMotion.Create(trailing, settled, duration)
                .WithEase(GatherEase)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharUv3Custom(textComponent, charIndex, trailing));
        }

        // 尾は文字の矩形より上下へ伸びるので矩形を縦に広げたままにする。シェーダーは同じ倍率を UV3.y で受け取り、字形を元の大きさに保つ。
        private static float CalculateVerticalQuadStretchToHoldTail(TMP_CharacterInfo charInfo)
        {
            var quadHeight = Mathf.Max(charInfo.vertex_TR.position.y - charInfo.vertex_BL.position.y, 0.0001f);
            return 2f * TailRoomEm * charInfo.pointSize / quadHeight;
        }

        public float GetIntervalToNextCharEntrance(CharData charData) => charData.Duration * EntranceIntervalRate;

        public UniTask OnTextFeed(CancellationToken token)
        {
            _evenCharsStartAbove = Random.value < 0.5f;
            return UniTask.CompletedTask;
        }
    }
}
