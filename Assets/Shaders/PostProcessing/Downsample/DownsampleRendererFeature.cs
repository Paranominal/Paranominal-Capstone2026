using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

// Summary: Renderer Feature that applies a downsample (pixelate) post-processing effect.
// Uses a custom raster render pass instead of AddBlitPass so the depth-stencil buffer
// is available for stencil-based UI exclusion.
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
        if (pass == null) return;
        if (renderingData.cameraData.cameraType != CameraType.Game) return;

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

    private void UpdateSettings()
    {
        if (material == null) return;

        var vol = VolumeManager.instance.stack.GetComponent<DownsampleVolume>();

        float pixelSize = vol.pixelSize.overrideState ? vol.pixelSize.value : defaultSettings.pixelSize;
        material.SetFloat(PixelSizeID, pixelSize);
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

        if (resourceData.isActiveTargetBackBuffer) return;

        TextureHandle src = resourceData.activeColorTexture;

        var desc = src.GetDescriptor(renderGraph);
        desc.name = "_DownsampleTexture";
        desc.depthBufferBits = 0;
        TextureHandle dst = renderGraph.CreateTexture(desc);

        UpdateSettings();

        if (!src.IsValid() || !dst.IsValid()) return;

        // First pass: blit through the downsample material with stencil buffer bound.
        using (var builder = renderGraph.AddRasterRenderPass<PassData>(PassName, out var passData))
        {
            passData.sourceTexture = src;
            passData.material = material;

            // Read the source colour texture.
            builder.UseTexture(src, AccessFlags.Read);

            // Set the temp texture as our render target.
            builder.SetRenderAttachment(dst, 0, AccessFlags.Write);

            // Bind the depth-stencil buffer for stencil testing.
            builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read);

            builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
            {
                Blitter.BlitTexture(context.cmd, data.sourceTexture, new Vector4(1, 1, 0, 0), data.material, 0);
            });
        }

        // Second pass: copy back without re-applying the effect.
        renderGraph.AddCopyPass(dst, src);
    }
}
