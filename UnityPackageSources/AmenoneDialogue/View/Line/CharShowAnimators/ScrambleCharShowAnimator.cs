using System.Collections.Generic;
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
    internal class ScrambleCharShowAnimator : ICharShowAnimator, IEveryFrameCharAnimator
    {
        private const float ScrambleDurationRate = 2.6f;
        private const float AppearDurationRate = 0.02f;
        private const float EntranceIntervalRate = 0.25f;
        private const int StepCount = 28;
        private const int CorrectGlyphSteps = 2;

        private static readonly Color32[] ScrambleColors =
        {
            new(0, 240, 255, 255),
            new(255, 232, 0, 255),
            new(255, 44, 60, 255),
            new(60, 255, 90, 255),
            new(255, 60, 230, 255),
            new(70, 110, 255, 255),
        };

        private sealed class ScramblingChar
        {
            public TMP_Text Text;
            public int CharIndex;
            public float Progress;
            public bool IsSettled;
        }

        private readonly List<ScramblingChar> _scramblingChars = new();
        private readonly List<int> _substituteCandidates = new();
        private readonly HashSet<int> _changedMaterialIndexes = new();

        public TextVisualizeTag Tag => TextVisualizeTag.Scramble;

        public void Register(TMP_Text textComponent, MotionSequenceBuilder sequence, CharData charData, int charIndex, float entranceStartTime)
        {
            var scramblingChar = new ScramblingChar { Text = textComponent, CharIndex = charIndex };
            _scramblingChars.Add(scramblingChar);

            sequence.Join(LMotion.Create(Color.clear, Color.white, charData.Duration * AppearDurationRate)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharColorCustom(textComponent, charIndex, Color.clear));
            sequence.Join(LMotion.Create(0f, 1f, charData.Duration * ScrambleDurationRate)
                .WithDelayHoldingStartValue(entranceStartTime)
                .Bind(progress => scramblingChar.Progress = progress));
        }

        public float GetIntervalToNextCharEntrance(CharData charData) => charData.Duration * EntranceIntervalRate;

        public UniTask OnTextFeed(CancellationToken token)
        {
            _scramblingChars.Clear();
            return UniTask.CompletedTask;
        }

        // パッケージと TMP が Update でメッシュを作り直すたびに字形と色が元へ戻るので、その後の LateUpdate で毎フレーム書き直す。
        public void LateUpdateChars(TMP_Text textComponent)
        {
            if (_scramblingChars.Count == 0) return;

            _changedMaterialIndexes.Clear();
            foreach (var scramblingChar in _scramblingChars)
            {
                if (scramblingChar.Text != textComponent || scramblingChar.IsSettled) continue;
                if (scramblingChar.Progress <= 0f || !IsDrawn(scramblingChar)) continue;

                var step = Mathf.Min((int)(scramblingChar.Progress * StepCount), StepCount);
                if (step >= StepCount)
                {
                    WriteGlyph(scramblingChar, scramblingChar.CharIndex, Color.white);
                    scramblingChar.IsSettled = true;
                    continue;
                }

                var shownCharIndex = step < StepCount - CorrectGlyphSteps
                    ? PickSubstitute(scramblingChar, step)
                    : scramblingChar.CharIndex;
                WriteGlyph(scramblingChar, shownCharIndex, PickColor(scramblingChar.CharIndex, step));
            }
            UploadChangedMeshes(textComponent);
        }

        private static bool IsDrawn(ScramblingChar scramblingChar)
        {
            var textInfo = scramblingChar.Text.textInfo;
            return scramblingChar.CharIndex < textInfo.characterCount && textInfo.characterInfo[scramblingChar.CharIndex].isVisible;
        }

        // 行に無い文字を出すとフォントに字形が無く豆腐になることがあるので、同じ行の同じフォントの文字から選ぶ。
        private int PickSubstitute(ScramblingChar scramblingChar, int step)
        {
            var textInfo = scramblingChar.Text.textInfo;
            var own = textInfo.characterInfo[scramblingChar.CharIndex];

            _substituteCandidates.Clear();
            for (var i = 0; i < textInfo.characterCount; i++)
            {
                var candidate = textInfo.characterInfo[i];
                if (candidate.isVisible && candidate.fontAsset == own.fontAsset && candidate.character != own.character)
                    _substituteCandidates.Add(i);
            }
            if (_substituteCandidates.Count == 0) return scramblingChar.CharIndex;

            var pick = Hash(scramblingChar.CharIndex, step) % _substituteCandidates.Count;
            var previousPick = Hash(scramblingChar.CharIndex, step - 1) % _substituteCandidates.Count;
            if (step > 0 && pick == previousPick) pick = (pick + 1) % _substituteCandidates.Count;
            return _substituteCandidates[pick];
        }

        private static Color32 PickColor(int charIndex, int step)
        {
            return ScrambleColors[Hash(charIndex * 31 + 7, step) % ScrambleColors.Length];
        }

        private static int Hash(int charIndex, int step)
        {
            unchecked
            {
                var hash = (uint)(charIndex * 73856093) ^ (uint)(step * 19349663);
                hash ^= hash >> 13;
                hash *= 0x5bd1e995;
                hash ^= hash >> 15;
                return (int)(hash & 0x7fffffff);
            }
        }

        private void WriteGlyph(ScramblingChar scramblingChar, int shownCharIndex, Color32 color)
        {
            var textInfo = scramblingChar.Text.textInfo;
            var own = textInfo.characterInfo[scramblingChar.CharIndex];
            var shown = textInfo.characterInfo[shownCharIndex];
            var meshInfo = textInfo.meshInfo[own.materialReferenceIndex];

            var sizeRate = shown.scale > 0f ? own.scale / shown.scale : 1f;
            var ownAnchor = new Vector3((own.bottomLeft.x + own.topRight.x) * 0.5f, own.baseLine, 0f);
            var shownAnchor = new Vector3((shown.bottomLeft.x + shown.topRight.x) * 0.5f, shown.baseLine, 0f);

            WriteVertex(meshInfo, own.vertexIndex, shown.vertex_BL, ownAnchor, shownAnchor, sizeRate, color);
            WriteVertex(meshInfo, own.vertexIndex + 1, shown.vertex_TL, ownAnchor, shownAnchor, sizeRate, color);
            WriteVertex(meshInfo, own.vertexIndex + 2, shown.vertex_TR, ownAnchor, shownAnchor, sizeRate, color);
            WriteVertex(meshInfo, own.vertexIndex + 3, shown.vertex_BR, ownAnchor, shownAnchor, sizeRate, color);
            _changedMaterialIndexes.Add(own.materialReferenceIndex);
        }

        private static void WriteVertex(TMP_MeshInfo meshInfo, int index, TMP_Vertex shown,
            Vector3 ownAnchor, Vector3 shownAnchor, float sizeRate, Color32 color)
        {
            var atlasUV = meshInfo.uvs0[index];
            atlasUV.x = shown.uv.x;
            atlasUV.y = shown.uv.y;
            meshInfo.vertices[index] = ownAnchor + (shown.position - shownAnchor) * sizeRate;
            meshInfo.uvs0[index] = atlasUV;
            meshInfo.colors32[index] = color;
        }

        private void UploadChangedMeshes(TMP_Text textComponent)
        {
            var textInfo = textComponent.textInfo;
            foreach (var materialIndex in _changedMaterialIndexes)
            {
                var meshInfo = textInfo.meshInfo[materialIndex];
                if (meshInfo.mesh == null) continue;

                meshInfo.mesh.vertices = meshInfo.vertices;
                meshInfo.mesh.SetUVs(0, meshInfo.uvs0);
                meshInfo.mesh.colors32 = meshInfo.colors32;
                textComponent.UpdateGeometry(meshInfo.mesh, materialIndex);
            }
        }
    }
}
