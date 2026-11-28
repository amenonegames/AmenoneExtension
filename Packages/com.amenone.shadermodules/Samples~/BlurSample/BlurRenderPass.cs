using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace Sandbox
{
    public class BlurRenderPass : ScriptableRenderPass
    {
        public const int MaxIterations = 6;
        private const int DownsampleShaderPass = 0;
        private const int UpsampleShaderPass = 1;
        private static readonly int UpsampleOffsetId = Shader.PropertyToID("_BlurUpsampleOffset");

        private readonly Material material;
        private readonly TextureHandle[] levels = new TextureHandle[MaxIterations];
        private int iterations;

        public BlurRenderPass(Material material)
        {
            this.material = material;
            requiresIntermediateTexture = true;
        }

        public void Setup(int iterations, float upsampleOffset, RenderPassEvent passEvent)
        {
            this.iterations = Mathf.Clamp(iterations, 1, MaxIterations);
            renderPassEvent = passEvent;
            material.SetFloat(UpsampleOffsetId, upsampleOffset);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            if (resourceData.isActiveTargetBackBuffer) return;

            var cameraColor = resourceData.activeColorTexture;
            var cameraColorDesc = renderGraph.GetTextureDesc(cameraColor);

            var source = cameraColor;
            for (var i = 0; i < iterations; i++)
            {
                levels[i] = CreateLevelTexture(renderGraph, cameraColorDesc, i + 1);
                AddBlit(renderGraph, source, levels[i], DownsampleShaderPass, $"Blur Downsample To Level {i + 1}");
                source = levels[i];
            }

            for (var i = iterations - 2; i >= 0; i--)
            {
                AddBlit(renderGraph, source, levels[i], UpsampleShaderPass, $"Blur Upsample To Level {i + 1}");
                source = levels[i];
            }

            AddBlit(renderGraph, source, cameraColor, UpsampleShaderPass, "Blur Upsample To Camera Color");
        }

        private static TextureHandle CreateLevelTexture(RenderGraph renderGraph, TextureDesc cameraColorDesc, int level)
        {
            var desc = new TextureDesc(Mathf.Max(1, cameraColorDesc.width >> level), Mathf.Max(1, cameraColorDesc.height >> level))
            {
                name = $"_BlurLevel{level}",
                format = cameraColorDesc.format,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                clearBuffer = false
            };
            return renderGraph.CreateTexture(desc);
        }

        private void AddBlit(RenderGraph renderGraph, TextureHandle source, TextureHandle destination, int shaderPass, string passName) =>
            renderGraph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(source, destination, material, shaderPass), passName);
    }
}
