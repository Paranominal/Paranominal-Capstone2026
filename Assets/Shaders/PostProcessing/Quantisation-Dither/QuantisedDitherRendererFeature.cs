using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

// Summary: Renderer Feature that applies quantised dithering as a post-processing effect.
public class QuantisedDitherRendererFeature : ScriptableRendererFeature
{
    [SerializeField] private Shader shader;
    [SerializeField] private QuantisedDitherSettings defaultSettings = new QuantisedDitherSettings();

    private Material material;
    private QuantisedDitherRenderPass pass;

    public override void Create()
    {
        if (shader == null) return;

        material = new Material(shader);
        pass = new QuantisedDitherRenderPass(material, defaultSettings);
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
public class QuantisedDitherSettings
{
    [Range(2f, 32f)] public float redSteps = 8f;
    [Range(2f, 32f)] public float greenSteps = 8f;
    [Range(2f, 32f)] public float blueSteps = 8f;
    [Range(0f, 1f)] public float effectStrength = 1f;
    [Range(0f, 1f)] public float ditherStrength = 1f;
    public float bayerSize = 16f;
    public bool usePS1Matrix = false;
    public bool usePerceivedBrightness = false;
    [Range(0.2f, 1.0f)] public float perceptualGamma = 0.5f;
}

// Summary: Render pass that blits the camera colour through the quantised dither material.
public class QuantisedDitherRenderPass : ScriptableRenderPass
{
    private static readonly int RedStepsID = Shader.PropertyToID("_RedSteps");
    private static readonly int GreenStepsID = Shader.PropertyToID("_GreenSteps");
    private static readonly int BlueStepsID = Shader.PropertyToID("_BlueSteps");
    private static readonly int EffectStrengthID = Shader.PropertyToID("_EffectStrength");
    private static readonly int DitherStrengthID = Shader.PropertyToID("_DitherStrength");
    private static readonly int BayerSizeID = Shader.PropertyToID("_BayerSize");
    private static readonly int UsePS1MatrixID = Shader.PropertyToID("_UsePS1Matrix");
    private static readonly int UsePerceivedBrightnessID = Shader.PropertyToID("_UsePerceivedBrightness");
    private static readonly int PerceptualGammaID = Shader.PropertyToID("_PerceptualGamma");
    private const string PassName = "QuantisedDitherRenderPass";

    private QuantisedDitherSettings defaultSettings;
    private Material material;

    public QuantisedDitherRenderPass(Material material, QuantisedDitherSettings defaultSettings)
    {
        this.material = material;
        this.defaultSettings = defaultSettings;
    }

    private void UpdateSettings()
    {
        if (material == null) return;

        var vol = VolumeManager.instance.stack.GetComponent<QuantisedDitherVolume>();

        float redSteps = vol.redSteps.overrideState ? vol.redSteps.value : defaultSettings.redSteps;
        float greenSteps = vol.greenSteps.overrideState ? vol.greenSteps.value : defaultSettings.greenSteps;
        float blueSteps = vol.blueSteps.overrideState ? vol.blueSteps.value : defaultSettings.blueSteps;
        float effectStrength = vol.effectStrength.overrideState ? vol.effectStrength.value : defaultSettings.effectStrength;
        float ditherStrength = vol.ditherStrength.overrideState ? vol.ditherStrength.value : defaultSettings.ditherStrength;
        float bayerSize = vol.bayerSize.overrideState ? vol.bayerSize.value : defaultSettings.bayerSize;
        bool usePS1 = vol.usePS1Matrix.overrideState ? vol.usePS1Matrix.value : defaultSettings.usePS1Matrix;
        bool usePerceived = vol.usePerceivedBrightness.overrideState ? vol.usePerceivedBrightness.value : defaultSettings.usePerceivedBrightness;
        float gamma = vol.perceptualGamma.overrideState ? vol.perceptualGamma.value : defaultSettings.perceptualGamma;

        material.SetFloat(RedStepsID, redSteps);
        material.SetFloat(GreenStepsID, greenSteps);
        material.SetFloat(BlueStepsID, blueSteps);
        material.SetFloat(EffectStrengthID, effectStrength);
        material.SetFloat(DitherStrengthID, ditherStrength);
        material.SetFloat(BayerSizeID, bayerSize);
        material.SetFloat(UsePS1MatrixID, usePS1 ? 1f : 0f);
        material.SetFloat(UsePerceivedBrightnessID, usePerceived ? 1f : 0f);
        material.SetFloat(PerceptualGammaID, gamma);
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

        if (material == null) return;
        if (resourceData.isActiveTargetBackBuffer) return;

        TextureHandle src = resourceData.activeColorTexture;

        var desc = src.GetDescriptor(renderGraph);
        desc.name = "_QuantisedDitherTexture";
        desc.depthBufferBits = 0;
        TextureHandle dst = renderGraph.CreateTexture(desc);

        UpdateSettings();

        if (!src.IsValid() || !dst.IsValid()) return;

        // Apply the effect from source to temp texture.
        RenderGraphUtils.BlitMaterialParameters blitOut = new(src, dst, material, 0);
        renderGraph.AddBlitPass(blitOut, PassName);

        // Copy back without applying the effect again.
        renderGraph.AddCopyPass(dst, src);
    }
}
