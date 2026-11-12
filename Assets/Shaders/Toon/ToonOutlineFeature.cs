using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Draws the "Outline" LightMode pass from CrossHatchInk / CrossHatchToonColor
/// (or any material with a pass tagged LightMode="Outline") as an explicit
/// URP render pass. URP's default forward path only renders passes tagged
/// UniversalForward, so a custom multi-pass shader's extra passes need to be
/// submitted manually via a ScriptableRendererFeature - this is that feature.
/// </summary>
public class ToonOutlineFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        [Tooltip("When in the frame this pass runs. BeforeRenderingOpaques draws " +
                 "the inflated backface outlines first, so the normal forward pass " +
                 "then draws over the top and only the silhouette peeks out.")]
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;

        [Tooltip("Only objects on these layers get outlined.")]
        public LayerMask layerMask = ~0;

        [Tooltip("Must match the LightMode tag on the shader's outline Pass block.")]
        public string shaderTagId = "Outline";
    }

    public Settings settings = new Settings();

    private OutlineRenderPass outlinePass;

    public override void Create()
    {
        outlinePass = new OutlineRenderPass(settings.shaderTagId, settings.layerMask, settings.renderPassEvent);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // Skip in preview/reflection cameras etc. if you want; not filtered here
        // so it works in Game view and Scene view alike.
        renderer.EnqueuePass(outlinePass);
    }

    private class OutlineRenderPass : ScriptableRenderPass
    {
        private readonly ShaderTagId shaderTagId;
        private FilteringSettings filteringSettings;
        private readonly ProfilingSampler profilingSampler = new ProfilingSampler("Toon Outline Pass");

        public OutlineRenderPass(string tagName, LayerMask layerMask, RenderPassEvent evt)
        {
            shaderTagId = new ShaderTagId(tagName);
            renderPassEvent = evt;
            filteringSettings = new FilteringSettings(RenderQueueRange.opaque, layerMask);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, profilingSampler))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                SortingCriteria sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;
                DrawingSettings drawingSettings = CreateDrawingSettings(
                    shaderTagId, ref renderingData, sortingCriteria);

                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}