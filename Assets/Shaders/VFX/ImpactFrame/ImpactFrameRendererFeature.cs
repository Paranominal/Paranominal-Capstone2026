// Summary: URP Renderer Feature that applies the Special Shot impact frame.
// Uses a raster pass instead of AddBlitPass so the camera normals texture can be declared as a read.
// Normals are only requested while the effect is active, since the pass is only enqueued then.

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public class ImpactFrameRendererFeature : ScriptableRendererFeature
{
    [SerializeField] private Shader shader;

    private Material material;
    private ImpactFrameRenderPass pass;

    public override void Create()
    {
        if (shader == null) return;

        material = new Material(shader);
        pass = new ImpactFrameRenderPass(material);

        // same event as CRT, place this feature above CRT in the Renderer Data list
        pass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
        pass.ConfigureInput(ScriptableRenderPassInput.Normal);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (pass == null) return;
        if (renderingData.cameraData.cameraType != CameraType.Game) return;

        var volume = VolumeManager.instance.stack.GetComponent<ImpactFrameVolumeComponent>();
        if (volume == null || !volume.IsActive()) return;

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

// Summary: Render pass that blits the camera colour through the impact frame material.
public class ImpactFrameRenderPass : ScriptableRenderPass
{
    // layer toggles
    private static readonly int FlashOnID  = Shader.PropertyToID("_FlashOn");
    private static readonly int SceneOnID  = Shader.PropertyToID("_SceneOn");
    private static readonly int LinesOnID  = Shader.PropertyToID("_LinesOn");
    private static readonly int JitterOnID = Shader.PropertyToID("_JitterOn");
    private static readonly int FlashUsesLightID = Shader.PropertyToID("_FlashUsesLight");

    // runtime
    private static readonly int FocalPointID = Shader.PropertyToID("_FocalPoint");
    private static readonly int SeedID       = Shader.PropertyToID("_Seed");

    // colours
    private static readonly int DarkColourID  = Shader.PropertyToID("_DarkColour");
    private static readonly int LightColourID = Shader.PropertyToID("_LightColour");

    // scene threshold
    private static readonly int SceneThresholdID = Shader.PropertyToID("_SceneThreshold");
    private static readonly int NormalsWeightID  = Shader.PropertyToID("_NormalsWeight");

    // speed lines
    private static readonly int LinesTilingID      = Shader.PropertyToID("_LinesTiling");
    private static readonly int LinesNoiseScaleID  = Shader.PropertyToID("_LinesNoiseScale");
    private static readonly int LinesThresholdID   = Shader.PropertyToID("_LinesThreshold");
    private static readonly int LinesClearMinID   = Shader.PropertyToID("_LinesClearMin");
    private static readonly int LinesClearMaxID   = Shader.PropertyToID("_LinesClearMax");
    private static readonly int LinesClearPowerID = Shader.PropertyToID("_LinesClearPower");

    // jitter
    private static readonly int JitterScaleID     = Shader.PropertyToID("_JitterScale");
    private static readonly int JitterThresholdID = Shader.PropertyToID("_JitterThreshold");
    private static readonly int JitterStrengthID  = Shader.PropertyToID("_JitterStrength");

    // normals
    private static readonly int CameraNormalsID = Shader.PropertyToID("_CameraNormalsTexture");

    private const string PassName = "ImpactFrameRenderPass";
    private Material material;

    private class PassData
    {
        public TextureHandle source;
        public TextureHandle normals;
        public Material material;
    }

    public ImpactFrameRenderPass(Material material)
    {
        this.material = material;
    }

    // returns false when there's nothing to draw
    private bool UpdateSettings()
    {
        if (material == null) return false;

        var vol = VolumeManager.instance.stack.GetComponent<ImpactFrameVolumeComponent>();
        if (vol == null || !vol.IsActive()) return false;

        material.SetFloat(FlashOnID, vol.flashActive.value ? 1f : 0f);
        material.SetFloat(SceneOnID, vol.sceneActive.value ? 1f : 0f);
        material.SetFloat(LinesOnID, vol.linesActive.value ? 1f : 0f);
        material.SetFloat(JitterOnID, vol.jitterActive.value ? 1f : 0f);
        material.SetFloat(FlashUsesLightID, vol.flashColour.value == ImpactFlashColour.Light ? 1f : 0f);

        material.SetVector(FocalPointID, vol.focalPoint.value);
        material.SetFloat(SeedID, vol.seed.value);

        material.SetColor(DarkColourID, vol.darkColour.value);
        material.SetColor(LightColourID, vol.lightColour.value);

        material.SetFloat(SceneThresholdID, vol.sceneThreshold.value);
        material.SetFloat(NormalsWeightID, vol.normalsWeight.value);

        material.SetFloat(LinesTilingID, vol.linesTiling.value);
        material.SetFloat(LinesNoiseScaleID, vol.linesNoiseScale.value);
        material.SetFloat(LinesThresholdID, vol.linesThreshold.value);

        material.SetFloat(JitterScaleID, vol.jitterScale.value);
        material.SetFloat(JitterThresholdID, vol.jitterThreshold.value);
        material.SetFloat(JitterStrengthID, vol.jitterStrength.value);
        material.SetFloat(LinesClearMinID, vol.linesClearMin.value);
        material.SetFloat(LinesClearMaxID, vol.linesClearMax.value);
        material.SetFloat(LinesClearPowerID, vol.linesClearPower.value);
        return true;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

        if (material == null) return;
        if (resourceData.isActiveTargetBackBuffer) return;

        if (!UpdateSettings()) return;

        TextureHandle src = resourceData.activeColorTexture;
        TextureHandle normals = resourceData.cameraNormalsTexture;

        if (!src.IsValid() || !normals.IsValid()) return;

        var desc = src.GetDescriptor(renderGraph);
        desc.name = "_ImpactFrameTexture";
        desc.depthBufferBits = 0;
        TextureHandle dst = renderGraph.CreateTexture(desc);

        if (!dst.IsValid()) return;

        using (var builder = renderGraph.AddRasterRenderPass<PassData>(PassName, out var passData))
        {
            passData.source = src;
            passData.normals = normals;
            passData.material = material;

            // declare both reads so the graph keeps the normals prepass and binds the texture
            builder.UseTexture(src, AccessFlags.Read);
            builder.UseTexture(normals, AccessFlags.Read);
            builder.SetRenderAttachment(dst, 0, AccessFlags.Write);

            builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
            {
                data.material.SetTexture(CameraNormalsID, data.normals);
                Blitter.BlitTexture(context.cmd, data.source, new Vector4(1f, 1f, 0f, 0f), data.material, 0);
            });
        }

        renderGraph.AddCopyPass(dst, src);
    }
}