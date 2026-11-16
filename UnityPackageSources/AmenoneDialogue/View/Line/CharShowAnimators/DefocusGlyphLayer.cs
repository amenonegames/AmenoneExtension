using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace View.Line
{
    internal class DefocusGlyph
    {
        private static readonly Vector2[] CornerRatios = { new(0f, 0f), new(0f, 1f), new(1f, 1f), new(1f, 0f) };

        private readonly DefocusGlyphLayer _layer;
        private readonly Vector2 _positionMin;
        private readonly Vector2 _positionMax;
        private readonly Vector2 _uvMin;
        private readonly Vector2 _uvMax;
        private readonly Vector4 _glyphRectInAtlas;
        private readonly float _sdfScale;

        private float _blur;
        private float _alpha;
        private float _scale = 1f;

        public bool IsVisible => _alpha > 0f;

        public DefocusGlyph(DefocusGlyphLayer layer, Vector3 positionBL, Vector3 positionTR, Vector4 uvBL, Vector4 uvTR, Vector2 blurMarginUV)
        {
            _layer = layer;
            _sdfScale = uvBL.w;
            _glyphRectInAtlas = new Vector4(
                Mathf.Min(uvBL.x, uvTR.x), Mathf.Min(uvBL.y, uvTR.y),
                Mathf.Max(uvBL.x, uvTR.x), Mathf.Max(uvBL.y, uvTR.y));

            var positionPerUV = new Vector2(
                (positionTR.x - positionBL.x) / (uvTR.x - uvBL.x),
                (positionTR.y - positionBL.y) / (uvTR.y - uvBL.y));
            var blurMarginPosition = blurMarginUV * positionPerUV;

            _uvMin = (Vector2)uvBL - blurMarginUV;
            _uvMax = (Vector2)uvTR + blurMarginUV;
            _positionMin = (Vector2)positionBL - blurMarginPosition;
            _positionMax = (Vector2)positionTR + blurMarginPosition;
        }

        public void Apply(float blur, float alpha, float scale)
        {
            _blur = blur;
            _alpha = alpha;
            _scale = scale;
            _layer.SetVerticesDirty();
        }

        public void AddQuadTo(VertexHelper vertexHelper)
        {
            var firstVertex = vertexHelper.currentVertCount;
            var center = (_positionMin + _positionMax) * 0.5f;
            var color = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(_alpha) * 255f));

            foreach (var ratio in CornerRatios)
            {
                var position = new Vector2(
                    Mathf.LerpUnclamped(_positionMin.x, _positionMax.x, ratio.x),
                    Mathf.LerpUnclamped(_positionMin.y, _positionMax.y, ratio.y));
                var uv = new Vector2(
                    Mathf.LerpUnclamped(_uvMin.x, _uvMax.x, ratio.x),
                    Mathf.LerpUnclamped(_uvMin.y, _uvMax.y, ratio.y));

                vertexHelper.AddVert(new UIVertex
                {
                    position = center + (position - center) * _scale,
                    color = color,
                    uv0 = new Vector4(uv.x, uv.y, _blur, _sdfScale * _scale),
                    uv1 = _glyphRectInAtlas,
                });
            }

            vertexHelper.AddTriangle(firstVertex, firstVertex + 1, firstVertex + 2);
            vertexHelper.AddTriangle(firstVertex + 2, firstVertex + 3, firstVertex);
        }
    }

    internal class DefocusGlyphLayer : MaskableGraphic
    {
        private const float BlurMarginExtraTexels = 1.5f;

        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int MaxBlurRadiusEmId = Shader.PropertyToID("_MaxBlurRadiusEm");
        private static readonly int BlurRadiusUVId = Shader.PropertyToID("_BlurRadiusUV");
        private static readonly int[] PropertyIdsCopiedFromSourceMaterial =
        {
            Shader.PropertyToID("_FaceDilate"),
            Shader.PropertyToID("_WeightNormal"),
            Shader.PropertyToID("_WeightBold"),
            Shader.PropertyToID("_ScaleRatioA"),
            Shader.PropertyToID("_GradientScale"),
            Shader.PropertyToID("_TextureWidth"),
            Shader.PropertyToID("_TextureHeight"),
            Shader.PropertyToID("_Sharpness"),
        };

        private readonly List<DefocusGlyph> _glyphs = new();
        private Material _materialInstance;
        private Texture _atlas;
        private Vector2 _blurMarginUV;

        public override Texture mainTexture => _atlas;

        public static DefocusGlyphLayer Create(TMP_Text source, Material sourceMaterial, TMP_FontAsset fontAsset, Material materialTemplate)
        {
            var layerObject = new GameObject(nameof(DefocusGlyphLayer), typeof(RectTransform), typeof(CanvasRenderer))
            {
                layer = source.gameObject.layer
            };
            var layer = layerObject.AddComponent<DefocusGlyphLayer>();
            layer.raycastTarget = false;
            layer.PlaceOnOriginOf(source.rectTransform);
            layer.SetUpMaterial(sourceMaterial, fontAsset, materialTemplate);
            return layer;
        }

        private void PlaceOnOriginOf(RectTransform source)
        {
            rectTransform.SetParent(source, false);
            rectTransform.anchorMin = source.pivot;
            rectTransform.anchorMax = source.pivot;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
        }

        private void SetUpMaterial(Material sourceMaterial, TMP_FontAsset fontAsset, Material materialTemplate)
        {
            _atlas = sourceMaterial.GetTexture(MainTexId);
            _materialInstance = new Material(materialTemplate) { hideFlags = HideFlags.HideAndDontSave };
            _materialInstance.SetTexture(MainTexId, _atlas);
            foreach (var propertyId in PropertyIdsCopiedFromSourceMaterial)
                _materialInstance.SetFloat(propertyId, sourceMaterial.GetFloat(propertyId));

            var atlasTexelSize = new Vector2(1f / _atlas.width, 1f / _atlas.height);
            var maxBlurRadiusUV = materialTemplate.GetFloat(MaxBlurRadiusEmId) * fontAsset.faceInfo.pointSize * atlasTexelSize;
            _materialInstance.SetVector(BlurRadiusUVId, maxBlurRadiusUV);
            _blurMarginUV = maxBlurRadiusUV + BlurMarginExtraTexels * atlasTexelSize;

            material = _materialInstance;
        }

        public DefocusGlyph AddGlyph(Vector3[] vertices, Vector4[] uvs, int vertexIndex)
        {
            const int topRightOffset = 2;
            var glyph = new DefocusGlyph(this,
                vertices[vertexIndex], vertices[vertexIndex + topRightOffset],
                uvs[vertexIndex], uvs[vertexIndex + topRightOffset],
                _blurMarginUV);
            _glyphs.Add(glyph);
            return glyph;
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            foreach (var glyph in _glyphs)
            {
                if (glyph.IsVisible) glyph.AddQuadTo(vertexHelper);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_materialInstance != null) Destroy(_materialInstance);
        }
    }
}
