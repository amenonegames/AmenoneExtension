using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Sandbox
{
    public class BlurRendererFeature : ScriptableRendererFeature
    {
        [SerializeField] private Shader shader;
        [SerializeField, Range(1, BlurRenderPass.MaxIterations)] private int iterations = 4;
        [SerializeField, Min(0f)] private float upsampleOffset = 1f;
        [SerializeField] private RenderPassEvent passEvent = RenderPassEvent.BeforeRenderingPostProcessing;

        private Material material;
        private BlurRenderPass pass;

        public override void Create()
        {
            CoreUtils.Destroy(material);
            material = null;
            pass = null;
            if (shader == null)
            {
                Debug.LogWarning($"{name}: assign the BlurSample shader. The blur is skipped.", this);
                return;
            }

            material = CoreUtils.CreateEngineMaterial(shader);
            pass = new BlurRenderPass(material);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (pass == null || !IsBlurredCamera(renderingData.cameraData.cameraType)) return;

            pass.Setup(iterations, upsampleOffset, passEvent);
            renderer.EnqueuePass(pass);
        }

        protected override void Dispose(bool disposing) => CoreUtils.Destroy(material);

        private static bool IsBlurredCamera(CameraType cameraType) => cameraType == CameraType.Game || cameraType == CameraType.SceneView;
    }
}
