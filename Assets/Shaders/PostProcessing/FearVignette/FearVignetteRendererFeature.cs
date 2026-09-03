using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

// Summary: Renderer Feature that applies the Fear Vignette post-processing effect.
public class FearVignetteRendererFeature : ScriptableRendererFeature
{
    [SerializeField] private Shader shader;
    [SerializeField] private FearVignetteSettings defaultSettings = new FearVignetteSettings();

    private Material material;
    private FearVignetteRenderPass pass;

    public override void Create()
    {
        if (shader == null) return;

        material = new Material(shader);
        pass = new FearVignetteRenderPass(material, defaultSettings);
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
public class FearVignetteSettings
{
    [Range(0f, 1f)] public float vignetteIntensity = 0f;
    [Range(0.01f, 1f)] public float vignetteSoftness = 0.3f;
    public Color vignetteColor = Color.black;
    [Range(0f, 1f)] public float noiseIntensity = 0f;
    [Range(1f, 20f)] public float noiseScale = 6f;
    public float noiseSpeed = 0.3f;
    public float cycleDuration = 10f;
    public int blendMode = 0;
}

// Summary: Render pass that blits the camera colour through the fear vignette material.
public class FearVignetteRenderPass : ScriptableRenderPass
{
    private static readonly int VignetteIntensityID = Shader.PropertyToID("_VignetteIntensity");
    private static readonly int VignetteSoftnessID = Shader.PropertyToID("_VignetteSoftness");
    private static readonly int VignetteColorID = Shader.PropertyToID("_VignetteColor");
    private static readonly int NoiseIntensityID = Shader.PropertyToID("_NoiseIntensity");
    private static readonly int NoiseScaleID = Shader.PropertyToID("_NoiseScale");
    private static readonly int NoiseSpeedID = Shader.PropertyToID("_NoiseSpeed");
    private static readonly int CycleDurationID = Shader.PropertyToID("_CycleDuration");
    private static readonly int BlendModeID = Shader.PropertyToID("_BlendMode");
    private const string PassName = "FearVignetteRenderPass";

    private FearVignetteSettings defaultSettings;
    private Material material;

    public FearVignetteRenderPass(Material material, FearVignetteSettings defaultSettings)
    {
        this.material = material;
        this.defaultSettings = defaultSettings;
    }

    private void UpdateSettings()
    {
        if (material == null) return;

        var vol = VolumeManager.instance.stack.GetComponent<FearVignetteVolume>();

        float vignetteIntensity = vol.vignetteIntensity.overrideState ? vol.vignetteIntensity.value : defaultSettings.vignetteIntensity;
        float vignetteSoftness = vol.vignetteSoftness.overrideState ? vol.vignetteSoftness.value : defaultSettings.vignetteSoftness;
        Color vignetteColor = vol.vignetteColor.overrideState ? vol.vignetteColor.value : defaultSettings.vignetteColor;
        float noiseIntensity = vol.noiseIntensity.overrideState ? vol.noiseIntensity.value : defaultSettings.noiseIntensity;
        float noiseScale = vol.noiseScale.overrideState ? vol.noiseScale.value : defaultSettings.noiseScale;
        float noiseSpeed = vol.noiseSpeed.overrideState ? vol.noiseSpeed.value : defaultSettings.noiseSpeed;
        float cycleDuration = vol.cycleDuration.overrideState ? vol.cycleDuration.value : defaultSettings.cycleDuration;
        int blendMode = vol.blendMode.overrideState ? vol.blendMode.value : defaultSettings.blendMode;

        material.SetFloat(VignetteIntensityID, vignetteIntensity);
        material.SetFloat(VignetteSoftnessID, vignetteSoftness);
        material.SetColor(VignetteColorID, vignetteColor);
        material.SetFloat(NoiseIntensityID, noiseIntensity);
        material.SetFloat(NoiseScaleID, noiseScale);
        material.SetFloat(NoiseSpeedID, noiseSpeed);
        material.SetFloat(CycleDurationID, cycleDuration);
        material.SetFloat(BlendModeID, (float)blendMode);
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

        if (resourceData.isActiveTargetBackBuffer) return;

        TextureHandle src = resourceData.activeColorTexture;

        var desc = src.GetDescriptor(renderGraph);
        desc.name = "_FearVignetteTexture";
        desc.depthBufferBits = 0;
        TextureHandle dst = renderGraph.CreateTexture(desc);

        UpdateSettings();

        // Skip the pass entirely when there is no vignette to render. Added so that we can just put vignette intensity to zero on scenes like the title screen :3
        if (material.GetFloat(VignetteIntensityID) < 0.001f) return;

        if (!src.IsValid() || !dst.IsValid()) return;

        // Apply the effect from source to temp texture.
        RenderGraphUtils.BlitMaterialParameters blitOut = new(src, dst, material, 0);
        renderGraph.AddBlitPass(blitOut, PassName);

        // Copy back without applying the effect again.
        renderGraph.AddCopyPass(dst, src);
    }
}
