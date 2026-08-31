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
        if (renderingData.cameraData.camera.name == "UICam" || renderingData.cameraData.camera.name == "DialogueCam" || renderingData.cameraData.camera.name == "POVCam") return; // bypasses the UI, Dialogue, and POC Cameras for the Downsampling pass, making the stenciling scripts unnecessary :)

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
        TextureHandle tmp = renderGraph.CreateTexture(desc);

        UpdateSettings();

        if (!src.IsValid() || !tmp.IsValid()) return;

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