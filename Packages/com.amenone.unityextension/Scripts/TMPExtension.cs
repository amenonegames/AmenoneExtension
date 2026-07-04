using TMPro;
using UnityEngine;

namespace Amenone.UnityExtension
{
    public static class TMPExtension
    {
        public static void ClearVertexColors(this TMP_Text textComponent , Color initialCol)
        {
            textComponent.ForceMeshUpdate(true);
            var textInfo = textComponent.textInfo;

            var count = Mathf.Min(textInfo.characterCount, textInfo.characterInfo.Length);
            for (int i = 0; i < count; i++)
            {
                var charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible)
                    continue;

                int materialIndex = charInfo.materialReferenceIndex;
                int vertexIndex = charInfo.vertexIndex;

                Color32[] colors = textInfo.meshInfo[materialIndex].colors32;

                colors[vertexIndex + 0] = initialCol;
                colors[vertexIndex + 1] = initialCol;
                colors[vertexIndex + 2] = initialCol;
                colors[vertexIndex + 3] = initialCol;
            }

            for (int i = 0; i < textInfo.materialCount; i++)
            {
                if (textInfo.meshInfo[i].mesh == null) { continue; }

                textInfo.meshInfo[i].mesh.colors32 = textInfo.meshInfo[i].colors32;
                textComponent.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
            }
        }
    }
}