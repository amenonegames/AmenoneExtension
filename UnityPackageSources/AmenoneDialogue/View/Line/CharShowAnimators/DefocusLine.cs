using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace View.Line
{
    internal class DefocusLine
    {
        private static readonly Color32 HiddenColor = new(255, 255, 255, 0);

        private readonly TMP_Text _source;
        private readonly Material _materialTemplate;
        private readonly Dictionary<int, DefocusGlyphLayer> _layersByMaterialIndex = new();
        private readonly List<DefocusGlyph> _glyphs = new();
        private readonly List<int> _charIndices = new();

        public TMP_Text Source => _source;
        public IReadOnlyList<DefocusGlyph> Glyphs => _glyphs;

        public DefocusLine(TMP_Text source, Material materialTemplate)
        {
            _source = source;
            _materialTemplate = materialTemplate;
        }

        public DefocusGlyph AddGlyph(int charIndex)
        {
            var textInfo = _source.textInfo;
            var charInfo = textInfo.characterInfo[charIndex];
            var materialIndex = charInfo.materialReferenceIndex;

            if (!_layersByMaterialIndex.TryGetValue(materialIndex, out var layer))
            {
                layer = DefocusGlyphLayer.Create(_source, charInfo.material, charInfo.fontAsset, _materialTemplate);
                _layersByMaterialIndex.Add(materialIndex, layer);
            }

            var meshInfo = textInfo.meshInfo[materialIndex];
            var glyph = layer.AddGlyph(meshInfo.vertices, meshInfo.uvs0, charInfo.vertexIndex);
            _glyphs.Add(glyph);
            _charIndices.Add(charIndex);
            return glyph;
        }

        // TMP がメッシュを作り直すと頂点カラーは元に戻る。退場中の行は文字列もレイアウトも変えない前提で直接書く。
        public void HideSourceChars()
        {
            var textInfo = _source.textInfo;
            foreach (var charIndex in _charIndices)
            {
                if (charIndex >= textInfo.characterCount) continue;

                var charInfo = textInfo.characterInfo[charIndex];
                if (!charInfo.isVisible) continue;

                var colors = textInfo.meshInfo[charInfo.materialReferenceIndex].colors32;
                for (var vertexOffset = 0; vertexOffset < 4; vertexOffset++)
                    colors[charInfo.vertexIndex + vertexOffset] = HiddenColor;
            }
            _source.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
        }

        public void DestroyLayers()
        {
            foreach (var layer in _layersByMaterialIndex.Values)
            {
                if (layer != null) Object.Destroy(layer.gameObject);
            }
            _layersByMaterialIndex.Clear();
            _glyphs.Clear();
            _charIndices.Clear();
        }
    }
}
