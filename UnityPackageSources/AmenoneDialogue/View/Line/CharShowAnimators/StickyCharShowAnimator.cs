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
    internal class StickyCharShowAnimator : ICharShowAnimator, IEveryFrameCharAnimator
    {
        private const int VerticesPerChar = 4;
        private const float MaxStepSeconds = 1f / 30f;
        private const float RestPullSqr = 0.0004f;
        private const float RestSpeedSqr = 0.01f;
        private const float AppearDurationRate = 0.15f;
        private const float EntranceIntervalRate = 0.35f;
        private const float QuadMargin = 4f;
        private const int AtlasRectChannel = 1;
        private const int RestQuadChannel = 2;
        private const int PullChannel = 3;
        private const AdditionalCanvasShaderChannels QuadShapeChannels =
            AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.TexCoord2 | AdditionalCanvasShaderChannels.TexCoord3;

        private sealed class QuadShapeBuffer
        {
            public readonly List<Vector4> AtlasRects = new();
            public readonly List<Vector4> RestQuads = new();
            public readonly List<Vector4> Pulls = new();

            public void ResetTo(int vertexCount)
            {
                Refill(AtlasRects, vertexCount);
                Refill(RestQuads, vertexCount);
                Refill(Pulls, vertexCount);
            }

            private static void Refill(List<Vector4> values, int count)
            {
                values.Clear();
                for (var i = 0; i < count; i++) values.Add(Vector4.zero);
            }
        }

        private sealed class PulledChar
        {
            public TMP_Text Text;
            public int CharIndex;
            public Vector2 Anchor;
            public Vector2 Pull;
            public Vector2 Velocity;
            public bool IsAttached;
            public float EntranceProgress;
            public bool HasEntered;
            public Vector2 EntranceStretch;
        }

        private readonly IPointerPositionReader _pointerPositionReader;
        private readonly StickyPullSettings _settings;
        private readonly List<PulledChar> _pulledChars = new();
        private readonly Dictionary<int, QuadShapeBuffer> _quadShapeBuffers = new();
        private readonly HashSet<int> _changedMaterialIndexes = new();

        public StickyCharShowAnimator(IPointerPositionReader pointerPositionReader, StickyPullSettings settings)
        {
            _pointerPositionReader = pointerPositionReader;
            _settings = settings;
        }

        public TextVisualizeTag Tag => TextVisualizeTag.Sticky;

        public void Register(TMP_Text textComponent, MotionSequenceBuilder sequence, CharData charData, int charIndex, float entranceStartTime)
        {
            var pulledChar = new PulledChar { Text = textComponent, CharIndex = charIndex };

            sequence.Join(LMotion.Create(Color.clear, Color.white, charData.Duration * AppearDurationRate)
                .WithDelayHoldingStartValue(entranceStartTime)
                .BindToTMPCharColorCustom(textComponent, charIndex, Color.clear));
            sequence.Join(LMotion.Create(0f, 1f, charData.Duration)
                .WithEase(Ease.InOutBack)
                .WithDelayHoldingStartValue(entranceStartTime)
                .Bind(progress => pulledChar.EntranceProgress = progress));

            _pulledChars.Add(pulledChar);
            EnableQuadShapeChannels(textComponent);
        }

        private static void EnableQuadShapeChannels(TMP_Text textComponent)
        {
            var canvas = textComponent.canvas;
            if (canvas != null) canvas.additionalShaderChannels |= QuadShapeChannels;
        }

        public float GetIntervalToNextCharEntrance(CharData charData) => charData.Duration * EntranceIntervalRate;

        public UniTask OnTextFeed(CancellationToken token)
        {
            ReleaseAllChars();
            return UniTask.CompletedTask;
        }

        // パッケージと TMP が Update でメッシュを作り直すたびに頂点と UV が元へ戻るので、その後の LateUpdate で毎フレーム書き直す。
        public void LateUpdateChars(TMP_Text textComponent)
        {
            if (_pulledChars.Count == 0) return;

            var hasPointer = TryReadPointerInTextSpace(textComponent, out var pointer);
            var deltaTime = Mathf.Min(Time.unscaledDeltaTime, MaxStepSeconds);

            _changedMaterialIndexes.Clear();
            foreach (var pulledChar in _pulledChars)
            {
                if (pulledChar.Text != textComponent) continue;
                if (!TryReadRestQuad(pulledChar, out var restQuad)) continue;

                StartEntranceStretchOnce(pulledChar, restQuad, hasPointer, pointer);
                UpdateAttachment(pulledChar, restQuad, hasPointer, pointer);
                StepSpring(pulledChar, hasPointer, pointer, deltaTime);
                WriteQuad(pulledChar, restQuad, pulledChar.Pull + CalculateRemainingEntranceStretch(pulledChar));
            }
            UploadChangedMeshes(textComponent);
        }

        private bool TryReadPointerInTextSpace(TMP_Text textComponent, out Vector2 pointer)
        {
            pointer = default;
            if (!_pointerPositionReader.TryReadScreenPosition(out var screenPosition)) return false;

            var canvas = textComponent.canvas;
            var camera = canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(textComponent.rectTransform, screenPosition, camera, out pointer);
        }

        private static bool TryReadRestQuad(PulledChar pulledChar, out Rect restQuad)
        {
            restQuad = default;
            var textInfo = pulledChar.Text.textInfo;
            if (pulledChar.CharIndex >= textInfo.characterCount) return false;

            var charInfo = textInfo.characterInfo[pulledChar.CharIndex];
            if (!charInfo.isVisible) return false;

            restQuad = Rect.MinMaxRect(charInfo.bottomLeft.x, charInfo.bottomLeft.y, charInfo.topRight.x, charInfo.topRight.y);
            return true;
        }

        private void StartEntranceStretchOnce(PulledChar pulledChar, Rect restQuad, bool hasPointer, Vector2 pointer)
        {
            if (pulledChar.HasEntered || pulledChar.EntranceProgress <= 0f) return;

            pulledChar.HasEntered = true;
            if (!hasPointer) return;

            pulledChar.Anchor = FindGripPoint(restQuad, pointer);
            pulledChar.EntranceStretch = Vector2.ClampMagnitude(pointer - pulledChar.Anchor, _settings.MaxEntranceStretch);
        }

        private static Vector2 CalculateRemainingEntranceStretch(PulledChar pulledChar)
        {
            return pulledChar.EntranceStretch * (1f - pulledChar.EntranceProgress);
        }

        private void UpdateAttachment(PulledChar pulledChar, Rect restQuad, bool hasPointer, Vector2 pointer)
        {
            if (!hasPointer || !pulledChar.HasEntered)
            {
                pulledChar.IsAttached = false;
                return;
            }

            var distance = Vector2.Distance(pointer, ClampInto(restQuad, pointer));
            if (distance <= _settings.AttachDistance) pulledChar.IsAttached = true;
            if (distance > _settings.DetachDistance) pulledChar.IsAttached = false;
            if (pulledChar.IsAttached) pulledChar.Anchor = FindGripPoint(restQuad, pointer);
        }

        // 字の矩形の縁は余白なので、少し内側を掴ませて字の線が糸に乗るようにする。
        private Vector2 FindGripPoint(Rect restQuad, Vector2 pointer)
        {
            var inset = restQuad.size * _settings.AnchorInset;
            var gripArea = Rect.MinMaxRect(restQuad.xMin + inset.x, restQuad.yMin + inset.y, restQuad.xMax - inset.x, restQuad.yMax - inset.y);
            return ClampInto(gripArea, pointer);
        }

        private static Vector2 ClampInto(Rect area, Vector2 point)
        {
            return new Vector2(Mathf.Clamp(point.x, area.xMin, area.xMax), Mathf.Clamp(point.y, area.yMin, area.yMax));
        }

        private void StepSpring(PulledChar pulledChar, bool hasPointer, Vector2 pointer, float deltaTime)
        {
            var target = pulledChar.IsAttached && hasPointer
                ? (pointer - pulledChar.Anchor) * _settings.PullStrength
                : Vector2.zero;

            var velocity = pulledChar.Velocity;
            var pull = pulledChar.Pull;
            velocity += ((target - pull) * _settings.SpringStiffness - velocity * _settings.SpringDamping) * deltaTime;
            pull += velocity * deltaTime;

            if (target == Vector2.zero && pull.sqrMagnitude < RestPullSqr && velocity.sqrMagnitude < RestSpeedSqr)
            {
                pull = Vector2.zero;
                velocity = Vector2.zero;
            }

            pulledChar.Velocity = velocity;
            pulledChar.Pull = pull;
        }

        // 糸は字の矩形の外へ伸びるので、矩形を糸の先まで広げる。シェーダーは元の矩形と掴んだ点と引く量を受け取り、字形を元の位置に保ったまま糸だけを伸ばす。
        private void WriteQuad(PulledChar pulledChar, Rect restQuad, Vector2 pull)
        {
            var textInfo = pulledChar.Text.textInfo;
            var charInfo = textInfo.characterInfo[pulledChar.CharIndex];
            var materialIndex = charInfo.materialReferenceIndex;
            var meshInfo = textInfo.meshInfo[materialIndex];

            if (!_quadShapeBuffers.TryGetValue(materialIndex, out var buffer))
                _quadShapeBuffers[materialIndex] = buffer = new QuadShapeBuffer();
            if (_changedMaterialIndexes.Add(materialIndex))
                buffer.ResetTo(meshInfo.vertices.Length);

            var tip = pulledChar.Anchor + pull;
            var hasPull = pull != Vector2.zero;
            var min = hasPull ? Vector2.Min(restQuad.min, tip - Vector2.one * QuadMargin) : restQuad.min;
            var max = hasPull ? Vector2.Max(restQuad.max, tip + Vector2.one * QuadMargin) : restQuad.max;
            var depth = charInfo.bottomLeft.z;

            var firstVertex = charInfo.vertexIndex;
            meshInfo.vertices[firstVertex] = new Vector3(min.x, min.y, depth);
            meshInfo.vertices[firstVertex + 1] = new Vector3(min.x, max.y, depth);
            meshInfo.vertices[firstVertex + 2] = new Vector3(max.x, max.y, depth);
            meshInfo.vertices[firstVertex + 3] = new Vector3(max.x, min.y, depth);

            var atlasBottomLeft = meshInfo.uvs0[firstVertex];
            var atlasTopRight = meshInfo.uvs0[firstVertex + 2];
            var atlasRect = new Vector4(atlasBottomLeft.x, atlasBottomLeft.y, atlasTopRight.x, atlasTopRight.y);
            var restQuadValue = new Vector4(restQuad.xMin, restQuad.yMin, restQuad.width, restQuad.height);
            var pullValue = new Vector4(pulledChar.Anchor.x, pulledChar.Anchor.y, pull.x, pull.y);

            for (var vertex = 0; vertex < VerticesPerChar; vertex++)
            {
                buffer.AtlasRects[firstVertex + vertex] = atlasRect;
                buffer.RestQuads[firstVertex + vertex] = restQuadValue;
                buffer.Pulls[firstVertex + vertex] = pullValue;
            }
        }

        private void UploadChangedMeshes(TMP_Text textComponent)
        {
            var textInfo = textComponent.textInfo;
            foreach (var materialIndex in _changedMaterialIndexes)
            {
                var meshInfo = textInfo.meshInfo[materialIndex];
                if (meshInfo.mesh == null) continue;

                var buffer = _quadShapeBuffers[materialIndex];
                meshInfo.mesh.vertices = meshInfo.vertices;
                meshInfo.mesh.SetUVs(AtlasRectChannel, buffer.AtlasRects);
                meshInfo.mesh.SetUVs(RestQuadChannel, buffer.RestQuads);
                meshInfo.mesh.SetUVs(PullChannel, buffer.Pulls);
                textComponent.UpdateGeometry(meshInfo.mesh, materialIndex);
            }
        }

        private void ReleaseAllChars()
        {
            foreach (var pulledChar in _pulledChars)
            {
                if (pulledChar.Text == null || !TryReadRestQuad(pulledChar, out var restQuad)) continue;

                _changedMaterialIndexes.Clear();
                WriteQuad(pulledChar, restQuad, Vector2.zero);
                UploadChangedMeshes(pulledChar.Text);
            }
            _pulledChars.Clear();
        }
    }
}
