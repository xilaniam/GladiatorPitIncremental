using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Full-screen post-process outline: detects depth and normal discontinuities
/// across the whole rendered image and draws a bold, constant-pixel-width ink
/// line over them. Unlike the per-material inverted-hull Outline pass, this
/// works on every object in the scene regardless of shader, gives a uniform
/// line thickness at any distance, and also catches interior creases.
///
/// Requires: on your URP Pipeline Asset (or the Renderer's Universal Renderer
/// Data settings), make sure "Depth Texture" is enabled so _CameraDepthTexture
/// is available to sample.
/// </summary>
public class ScreenSpaceOutlineFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        public Material outlineMaterial;
    }

    public Settings settings = new Settings();
    private OutlinePass outlinePass;

    public override void Create()
    {
        outlinePass = new OutlinePass(settings.outlineMaterial, settings.renderPassEvent);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.outlineMaterial == null)
        {
            Debug.LogWarning("ScreenSpaceOutlineFeature: no outline material assigned, skipping.");
            return;
        }
        renderer.EnqueuePass(outlinePass);
    }

    private class OutlinePass : ScriptableRenderPass
    {
        private readonly Material material;
        private RTHandle tempTarget;
        private readonly ProfilingSampler profilingSampler = new ProfilingSampler("Screen Space Outline");

        public OutlinePass(Material mat, RenderPassEvent evt)
        {
            material = mat;
            renderPassEvent = evt;
            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(ref tempTarget, descriptor, name: "_OutlineTempTarget");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (material == null) return;

            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, profilingSampler))
            {
                RTHandle cameraTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;

                // Blit scene -> temp using the outline material (reads depth,
                // writes outline-composited color), then blit temp back onto
                // the camera target.
                Blitter.BlitCameraTexture(cmd, cameraTarget, tempTarget, material, 0);
                Blitter.BlitCameraTexture(cmd, tempTarget, cameraTarget);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Dispose()
        {
            tempTarget?.Release();
        }
    }

    protected override void Dispose(bool disposing)
    {
        outlinePass?.Dispose();
    }
}
