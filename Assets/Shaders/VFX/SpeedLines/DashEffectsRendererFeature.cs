// Summary: URP Renderer Feature that applies the dash post-processing effects.
// Matches the project's established blit pattern (AddBlitPass + AddCopyPass).
// EDIT (ethereal-grade): Adds an Ethereal Grade raster pass (shader pass 1) that runs before the
// existing dash pass (shader pass 0). Requests the depth texture when depth fade is enabled.

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public class DashEffectsRendererFeature : ScriptableRendererFeature
{
    [SerializeField] private Shader shader;

    private Material material;
    private DashEffectsRenderPass pass;

    public override void Create()
    {
        if (shader == null) return;

        material = new Material(shader);
        pass = new DashEffectsRenderPass(material);
        pass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (pass == null) return;
        if (renderingData.cameraData.cameraType != CameraType.Game) return;

        var volume = VolumeManager.instance.stack.GetComponent<DashEffectsVolumeComponent>();
        if (volume == null || !volume.IsActive()) return;

        // EDIT (ethereal-grade): only ask URP for the depth texture when depth fade actually needs it
        bool needsDepth = volume.enableEthereal.value && volume.enableDepthFade.value;
        pass.ConfigureInput(needsDepth ? ScriptableRenderPassInput.Depth : ScriptableRenderPassInput.None);

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

// Summary: Render pass that blits the camera colour through the dash effects material.
public class DashEffectsRenderPass : ScriptableRenderPass
{
    // master
    private static readonly int IntensityID = Shader.PropertyToID("_Intensity");

    // toggles
    private static readonly int EnableBlurID  = Shader.PropertyToID("_EnableBlur");
    private static readonly int EnableWarpID  = Shader.PropertyToID("_EnableWarp");
    private static readonly int EnableLinesID = Shader.PropertyToID("_EnableLines");

    // blur
    private static readonly int BlurStrengthID  = Shader.PropertyToID("_BlurStrength");
    private static readonly int SampleCountID   = Shader.PropertyToID("_SampleCount");
    private static readonly int CenterFalloffID = Shader.PropertyToID("_CenterFalloff");

    // warp
    private static readonly int WarpStrengthID = Shader.PropertyToID("_WarpStrength");

    // action lines
    private static readonly int LinesColourID      = Shader.PropertyToID("_LinesColour");
    private static readonly int LinesTilingID       = Shader.PropertyToID("_LinesTiling");
    private static readonly int LinesRadialScaleID  = Shader.PropertyToID("_LinesRadialScale");
    private static readonly int LinesPowerID        = Shader.PropertyToID("_LinesPower");
    private static readonly int LinesRemapID        = Shader.PropertyToID("_LinesRemap");
    private static readonly int LinesAnimationID    = Shader.PropertyToID("_LinesAnimation");

    // mask
    private static readonly int MaskScaleID    = Shader.PropertyToID("_MaskScale");
    private static readonly int MaskHardnessID = Shader.PropertyToID("_MaskHardness");
    private static readonly int MaskPowerID    = Shader.PropertyToID("_MaskPower");

    // EDIT (ethereal-grade): ethereal grade property IDs
    private static readonly int DesaturationID      = Shader.PropertyToID("_Desaturation");
    private static readonly int ShadowColourID      = Shader.PropertyToID("_ShadowColour");
    private static readonly int HighlightColourID   = Shader.PropertyToID("_HighlightColour");
    private static readonly int SplitToneBalanceID  = Shader.PropertyToID("_SplitToneBalance");
    private static readonly int TintStrengthID      = Shader.PropertyToID("_TintStrength");
    private static readonly int GlowThresholdID     = Shader.PropertyToID("_GlowThreshold");
    private static readonly int GlowIntensityID     = Shader.PropertyToID("_GlowIntensity");
    private static readonly int GlowSpreadID        = Shader.PropertyToID("_GlowSpread");
    private static readonly int EnableDepthFadeID   = Shader.PropertyToID("_EnableDepthFade");
    private static readonly int DepthFadeColourID   = Shader.PropertyToID("_DepthFadeColour");
    private static readonly int DepthFadeStartID    = Shader.PropertyToID("_DepthFadeStart");
    private static readonly int DepthFadeEndID      = Shader.PropertyToID("_DepthFadeEnd");
    private static readonly int DepthFadeStrengthID = Shader.PropertyToID("_DepthFadeStrength");
    private static readonly int EtherealDepthTexID  = Shader.PropertyToID("_EtherealDepthTex");

    // EDIT (ethereal-grade): shader pass indices (dash stays at 0 so existing behaviour is unchanged)
    private const int DashShaderPass     = 0;
    private const int EtherealShaderPass = 1;

    private const string PassName = "DashEffectsRenderPass";
    private const string EtherealPassName = "DashEtherealGradePass"; // EDIT (ethereal-grade)
    private Material material;

    // EDIT (ethereal-grade): data handed to the ethereal grade raster pass
    private class EtherealPassData
    {
        public TextureHandle source;
        public TextureHandle depth;
        public Material material;
        public bool useDepth;
    }

    public DashEffectsRenderPass(Material material)
    {
        this.material = material;
    }

    // EDIT (ethereal-grade): now returns the volume so RecordRenderGraph can read the layer toggles
    private DashEffectsVolumeComponent UpdateSettings()
    {
        if (material == null) return null;

        var vol = VolumeManager.instance.stack.GetComponent<DashEffectsVolumeComponent>();
        if (vol == null) return null;

        material.SetFloat(IntensityID, vol.intensity.value);

        material.SetFloat(EnableBlurID, vol.enableBlur.value ? 1f : 0f);
        material.SetFloat(EnableWarpID, vol.enableWarp.value ? 1f : 0f);
        material.SetFloat(EnableLinesID, vol.enableLines.value ? 1f : 0f);

        material.SetFloat(BlurStrengthID, vol.blurStrength.value);
        material.SetFloat(SampleCountID, vol.sampleCount.value);
        material.SetFloat(CenterFalloffID, vol.centerFalloff.value);

        material.SetFloat(WarpStrengthID, vol.warpStrength.value);

        material.SetColor(LinesColourID, vol.linesColour.value);
        material.SetFloat(LinesTilingID, vol.linesTiling.value);
        material.SetFloat(LinesRadialScaleID, vol.linesRadialScale.value);
        material.SetFloat(LinesPowerID, vol.linesPower.value);
        material.SetFloat(LinesRemapID, vol.linesRemap.value);
        material.SetFloat(LinesAnimationID, vol.linesAnimation.value);

        material.SetFloat(MaskScaleID, vol.maskScale.value);
        material.SetFloat(MaskHardnessID, vol.maskHardness.value);
        material.SetFloat(MaskPowerID, vol.maskPower.value);

        // EDIT (ethereal-grade): push ethereal grade settings
        material.SetFloat(DesaturationID, vol.desaturation.value);
        material.SetColor(ShadowColourID, vol.shadowColour.value);
        material.SetColor(HighlightColourID, vol.highlightColour.value);
        material.SetFloat(SplitToneBalanceID, vol.splitToneBalance.value);
        material.SetFloat(TintStrengthID, vol.tintStrength.value);
        material.SetFloat(GlowThresholdID, vol.glowThreshold.value);
        material.SetFloat(GlowIntensityID, vol.glowIntensity.value);
        material.SetFloat(GlowSpreadID, vol.glowSpread.value);
        material.SetColor(DepthFadeColourID, vol.depthFadeColour.value);
        material.SetFloat(DepthFadeStartID, vol.depthFadeStart.value);
        material.SetFloat(DepthFadeEndID, vol.depthFadeEnd.value);
        material.SetFloat(DepthFadeStrengthID, vol.depthFadeStrength.value);

        return vol;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

        if (material == null) return;
        if (resourceData.isActiveTargetBackBuffer) return;

        TextureHandle src = resourceData.activeColorTexture;

        var desc = src.GetDescriptor(renderGraph);
        desc.name = "_DashEffectsTexture";
        desc.depthBufferBits = 0;
        TextureHandle dst = renderGraph.CreateTexture(desc);

        var vol = UpdateSettings(); // EDIT (ethereal-grade)
        if (vol == null) return;    // EDIT (ethereal-grade)

        if (material.GetFloat(IntensityID) < 0.001f) return;

        if (!src.IsValid() || !dst.IsValid()) return;

        // EDIT (ethereal-grade): grade first into its own texture, then the dash pass reads from that
        TextureHandle dashSource = src;

        if (vol.enableEthereal.value)
        {
            var gradeDesc = desc;
            gradeDesc.name = "_DashEtherealGradeTexture";
            TextureHandle graded = renderGraph.CreateTexture(gradeDesc);

            TextureHandle depth = resourceData.cameraDepthTexture;
            bool useDepth = vol.enableDepthFade.value && depth.IsValid();
            material.SetFloat(EnableDepthFadeID, useDepth ? 1f : 0f);

            using (var builder = renderGraph.AddRasterRenderPass<EtherealPassData>(EtherealPassName, out var passData))
            {
                passData.source = src;
                passData.depth = depth;
                passData.material = material;
                passData.useDepth = useDepth;

                builder.UseTexture(src, AccessFlags.Read);
                if (useDepth) builder.UseTexture(depth, AccessFlags.Read);
                builder.SetRenderAttachment(graded, 0, AccessFlags.Write);

                builder.SetRenderFunc((EtherealPassData data, RasterGraphContext ctx) =>
                {
                    if (data.useDepth)
                        data.material.SetTexture(EtherealDepthTexID, (RTHandle)data.depth);

                    Blitter.BlitTexture(ctx.cmd, data.source, new Vector4(1f, 1f, 0f, 0f), data.material, EtherealShaderPass);
                });
            }

            dashSource = graded;
        }

        RenderGraphUtils.BlitMaterialParameters blitOut = new(dashSource, dst, material, DashShaderPass); // EDIT (ethereal-grade): reads graded texture when enabled
        renderGraph.AddBlitPass(blitOut, PassName);

        renderGraph.AddCopyPass(dst, src);
    }
}
