// Summary: URP Renderer Feature that applies the speed lines post-processing effect.

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public class SpeedLinesRendererFeature : ScriptableRendererFeature
{
    [SerializeField] private Shader shader;

    private Material material;
    private SpeedLinesRenderPass pass;

    public override void Create()
    {
        if (shader == null) return;

        material = new Material(shader);
        pass = new SpeedLinesRenderPass(material);
        pass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (pass == null) return;
        if (renderingData.cameraData.cameraType != CameraType.Game) return;

        var volume = VolumeManager.instance.stack.GetComponent<SpeedLinesVolumeComponent>();
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

// Summary: Render pass that blits the camera colour through the speed lines material.
public class SpeedLinesRenderPass : ScriptableRenderPass
{
    // zoom blur
    private static readonly int IntensityID     = Shader.PropertyToID("_Intensity");
    private static readonly int BlurStrengthID  = Shader.PropertyToID("_BlurStrength");
    private static readonly int SampleCountID   = Shader.PropertyToID("_SampleCount");
    private static readonly int CenterFalloffID = Shader.PropertyToID("_CenterFalloff");

    // action lines
    private static readonly int LinesColourID      = Shader.PropertyToID("_LinesColour");
    private static readonly int LinesTilingID       = Shader.PropertyToID("_LinesTiling");
    private static readonly int LinesRadialScaleID  = Shader.PropertyToID("_LinesRadialScale");
    private static readonly int LinesPowerID        = Shader.PropertyToID("_LinesPower");
    private static readonly int LinesRemapID        = Shader.PropertyToID("_LinesRemap");
    private static readonly int LinesAnimationID    = Shader.PropertyToID("_LinesAnimation");

    // center mask
    private static readonly int MaskScaleID    = Shader.PropertyToID("_MaskScale");
    private static readonly int MaskHardnessID = Shader.PropertyToID("_MaskHardness");
    private static readonly int MaskPowerID    = Shader.PropertyToID("_MaskPower");

    private const string PassName = "SpeedLinesRenderPass";
    private Material material;

    public SpeedLinesRenderPass(Material material)
    {
        this.material = material;
    }

    private void UpdateSettings()
    {
        if (material == null) return;

        var vol = VolumeManager.instance.stack.GetComponent<SpeedLinesVolumeComponent>();
        if (vol == null) return;

        material.SetFloat(IntensityID, vol.intensity.value);
        material.SetFloat(BlurStrengthID, vol.blurStrength.value);
        material.SetFloat(SampleCountID, vol.sampleCount.value);
        material.SetFloat(CenterFalloffID, vol.centerFalloff.value);

        material.SetColor(LinesColourID, vol.linesColour.value);
        material.SetFloat(LinesTilingID, vol.linesTiling.value);
        material.SetFloat(LinesRadialScaleID, vol.linesRadialScale.value);
        material.SetFloat(LinesPowerID, vol.linesPower.value);
        material.SetFloat(LinesRemapID, vol.linesRemap.value);
        material.SetFloat(LinesAnimationID, vol.linesAnimation.value);

        material.SetFloat(MaskScaleID, vol.maskScale.value);
        material.SetFloat(MaskHardnessID, vol.maskHardness.value);
        material.SetFloat(MaskPowerID, vol.maskPower.value);
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

        if (material == null) return;
        if (resourceData.isActiveTargetBackBuffer) return;

        TextureHandle src = resourceData.activeColorTexture;

        var desc = src.GetDescriptor(renderGraph);
        desc.name = "_SpeedLinesTexture";
        desc.depthBufferBits = 0;
        TextureHandle dst = renderGraph.CreateTexture(desc);

        UpdateSettings();

        if (material.GetFloat(IntensityID) < 0.001f) return;

        if (!src.IsValid() || !dst.IsValid()) return;

        // apply the effect from source to temp texture
        RenderGraphUtils.BlitMaterialParameters blitOut = new(src, dst, material, 0);
        renderGraph.AddBlitPass(blitOut, PassName);

        // copy back without applying the effect again
        renderGraph.AddCopyPass(dst, src);
    }
}
