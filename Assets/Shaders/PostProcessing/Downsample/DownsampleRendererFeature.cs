using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

// Summary: Renderer Feature that applies a downsample (pixelate) post-processing effect.
// Uses a custom raster render pass instead of AddBlitPass so the depth-stencil buffer
// is available for stencil-based UI exclusion.
// Also publishes the final block size as _DownsamplePixelSize so QuantisedDither can match its dither cells to it.
public class DownsampleRendererFeature : ScriptableRendererFeature
{
    [SerializeField] private Shader shader;
    [SerializeField] private DownsampleSettings defaultSettings = new DownsampleSettings();

    private Material material;
    private DownsampleRenderPass pass;

    public override void Create()
    {
        if (shader == null) return;

        material = new Material(shader);
        pass = new DownsampleRenderPass(material, defaultSettings);
        pass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // EDIT (Downsample-Dither): Publish the fallback size on every skipped path so dither never uses a stale block size.
        if (pass == null) { DownsampleRenderPass.SetFallbackPixelSize(); return; }
        if (renderingData.cameraData.cameraType != CameraType.Game) { DownsampleRenderPass.SetFallbackPixelSize(); return; }

        // bypasses the UI, Dialogue, and POC Cameras for the Downsampling pass, making the stenciling scripts unnecessary :)
        if (renderingData.cameraData.camera.name == "UICam") { DownsampleRenderPass.SetFallbackPixelSize(); return; }
        //if (renderingData.cameraData.camera.name == "DialogueCam") return;
        //if (renderingData.cameraData.camera.name == "POVCam") return;

        renderer.EnqueuePass(pass);
    }

    protected override void Dispose(bool disposing)
    {
        if (Application.isPlaying)
            Destroy(material);
        else
            DestroyImmediate(material);
    }
}

[Serializable]
public class DownsampleSettings
{
    [Range(1f, 512f)] public float pixelSize = 4f;
}

// Summary: Custom raster render pass that blits through the downsample material
// with the depth-stencil buffer bound, enabling stencil-based UI exclusion.
public class DownsampleRenderPass : ScriptableRenderPass
{
    private static readonly int PixelSizeID = Shader.PropertyToID("_PixelSize");
    private static readonly int GlobalPixelSizeID = Shader.PropertyToID("_DownsamplePixelSize"); // EDIT (Downsample-Dither): Read by QuantisedDither.
    private static readonly int ResolutionScaleID = Shader.PropertyToID("_ResolutionScale"); // EDIT (Downsample-Dither): Set by RenderResolutionManager.
    private const string PassName = "DownsampleRenderPass";

    private DownsampleSettings defaultSettings;
    private Material material;

    // Data passed to the render function.
    private class PassData
    {
        public TextureHandle sourceTexture;
        public Material material;
    }

    public DownsampleRenderPass(Material material, DownsampleSettings defaultSettings)
    {
        this.material = material;
        this.defaultSettings = defaultSettings;
    }

    // EDIT (Downsample-Dither): Global resolution scale, treating an unset value as 1.
    private static float GetResolutionScale()
    {
        float scale = Shader.GetGlobalFloat(ResolutionScaleID);
        return scale > 0f ? scale : 1f;
    }

    // EDIT (Downsample-Dither): Used when Downsample doesn't run. Whole-number scale keeps dither cells even (1x at 1080p/1440p, 2x at 4K).
    public static void SetFallbackPixelSize()
    {
        int fallback = Mathf.Max(1, Mathf.FloorToInt(GetResolutionScale() + 0.001f));
        Shader.SetGlobalFloat(GlobalPixelSizeID, fallback);
    }

    private void UpdateSettings()
    {
        if (material == null) return;

        var vol = VolumeManager.instance.stack.GetComponent<DownsampleVolume>();

        float pixelSize = vol.pixelSize.overrideState ? vol.pixelSize.value : defaultSettings.pixelSize;

        // EDIT (Downsample-Dither): Resolution scaling and rounding done here (moved from the shader) so both shaders get the exact same whole-pixel size.
        int finalPixelSize = Mathf.Max(1, Mathf.RoundToInt(Mathf.Max(pixelSize, 1f) * GetResolutionScale()));

        material.SetFloat(PixelSizeID, finalPixelSize);
        Shader.SetGlobalFloat(GlobalPixelSizeID, finalPixelSize);
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

        // EDIT (Downsample-Dither): Fallback on early-outs too.
        if (material == null) { SetFallbackPixelSize(); return; }
        if (resourceData.isActiveTargetBackBuffer) { SetFallbackPixelSize(); return; }

        TextureHandle src = resourceData.activeColorTexture;

        var desc = src.GetDescriptor(renderGraph);
        desc.name = "_DownsampleTexture";
        desc.depthBufferBits = 0;
        TextureHandle tmp = renderGraph.CreateTexture(desc);

        if (!src.IsValid() || !tmp.IsValid()) { SetFallbackPixelSize(); return; } // EDIT (Downsample-Dither): Moved above UpdateSettings so the fallback isn't overwritten.

        UpdateSettings();

        // Copy source to temp so it can be sampled as _BlitTexture.
        renderGraph.AddCopyPass(src, tmp);

        // Blit from temp back to source through the downsample material.
        // Stencil-excluded pixels are never written, retaining the original image.
        using (var builder = renderGraph.AddRasterRenderPass<PassData>(PassName, out var passData))
        {
            passData.sourceTexture = tmp;
            passData.material = material;

            builder.UseTexture(tmp, AccessFlags.Read);
            builder.SetRenderAttachment(src, 0, AccessFlags.Write);
            builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.ReadWrite);

            builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
            {
                Blitter.BlitTexture(context.cmd, data.sourceTexture, new Vector4(1, 1, 0, 0), data.material, 0);
            });
        }
    }
}
