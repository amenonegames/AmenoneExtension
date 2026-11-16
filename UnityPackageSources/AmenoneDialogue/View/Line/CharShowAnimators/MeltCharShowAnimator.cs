using System.Collections.Generic;
using System.Linq;
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
    internal class MeltCharShowAnimator : ICharShowAnimator
    {
        private const float StreakRoomEm = 3.2f;
        private const float AppearDurationRate = 0.05f;
        private const float ExitDuration = 1.8f;
        private const float ExitStaggerPerChar = 0.22f;
        private const bool MeltsFromLineEnd = true;
        private const float EntranceIntervalRate = 0.35f;
        private const float EnteringPhase = -1f;
        private const float SettledPhase = 0f;
        private const float MeltedPhase = 1f;

        private readonly List<(TMP_Text text, int charIndex)> _shownChars = new();

        public TextVisualizeTag Tag => TextVisualizeTag.Melt;

        public void Register(TMP_Text textComponent, MotionSequenceBuilder sequence, CharData charData, int charIndex, float entranceStartTime)
        {
            var quadStretch = CalculateVerticalQuadStretchToHoldStreaks(textComponent, charIndex);
            var stretchedScale = new Vector3(1f, 1f + quadStretch, 1f);
            var enteringPhaseAndStretch = new Vector2(EnteringPhase, quadStretch);
            var settledPhaseAndStretch = new Vector2(SettledPhase, quadStretch);

            sequence.Join(LMotion.Create(Color.clear, Color.white, charData.Duration * AppearDurationRate)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharColorCustom(textComponent, charIndex, Color.clear));
            sequence.Join(LMotion.Create(stretchedScale, stretchedScale, charData.Duration)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharScaleCustom(textComponent, charIndex, stretchedScale));
            sequence.Join(LMotion.Create(enteringPhaseAndStretch, settledPhaseAndStretch, charData.Duration)
                .WithEase(Ease.OutSine)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharUv3Custom(textComponent, charIndex, enteringPhaseAndStretch));

            _shownChars.Add((textComponent, charIndex));
        }

        // 筋は文字の矩形より上下へ伸びるので矩形を縦に広げたままにする。シェーダーは同じ倍率を UV3.y で受け取り、字形を元の大きさに保つ。
        private static float CalculateVerticalQuadStretchToHoldStreaks(TMP_Text textComponent, int charIndex)
        {
            var charInfo = textComponent.textInfo.characterInfo[charIndex];
            var quadHeight = Mathf.Max(charInfo.vertex_TR.position.y - charInfo.vertex_BL.position.y, 0.0001f);
            return 2f * StreakRoomEm * charInfo.pointSize / quadHeight;
        }

        public float GetIntervalToNextCharEntrance(CharData charData) => charData.Duration * EntranceIntervalRate;

        public async UniTask OnTextFeed(CancellationToken token)
        {
            if (_shownChars.Count == 0) return;

            var meltingChars = MeltsFromLineEnd
                ? _shownChars.AsEnumerable().Reverse().ToArray()
                : _shownChars.ToArray();
            _shownChars.Clear();

            var melts = meltingChars.Select((shown, order) =>
                LMotion.Create(SettledPhase, MeltedPhase, ExitDuration)
                    .WithEase(Ease.InSine)
                    .WithDelay(order * ExitStaggerPerChar)
                    .Bind(phase => WritePhaseToMesh(shown.text, shown.charIndex, phase))
                    .ToUniTask(token));

            await UniTask.WhenAll(melts);
        }

        // BindToTMPChar* は同じ TMP_Text の全文字の UV3 を既定値へ戻してしまい、同じ行の他タグの文字の見た目を壊す。退場ではメッシュへ直接書く。
        private static void WritePhaseToMesh(TMP_Text textComponent, int charIndex, float phase)
        {
            if (textComponent == null) return;
            var textInfo = textComponent.textInfo;
            if (charIndex >= textInfo.characterCount) return;

            var charInfo = textInfo.characterInfo[charIndex];
            var materialIndex = charInfo.materialReferenceIndex;
            var meshInfo = textInfo.meshInfo[materialIndex];
            if (meshInfo.mesh == null) return;

            for (var vertexOffset = 0; vertexOffset < 4; vertexOffset++)
                meshInfo.uvs2[charInfo.vertexIndex + vertexOffset].x = phase;

            meshInfo.mesh.uv4 = meshInfo.uvs2;
            textComponent.UpdateGeometry(meshInfo.mesh, materialIndex);
        }
    }
}
