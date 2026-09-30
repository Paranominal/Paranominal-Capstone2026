using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

// Summary: Renderer Feature that applies a CRT post-processing effect.
public class CRTRendererFeature : ScriptableRendererFeature
{
    [SerializeField] private Shader shader;
    [SerializeField] private CRTSettings defaultSettings = new CRTSettings();

    private Material material;
    private CRTRenderPass pass;

    public override void Create()
    {
        if (shader == null) return;

        material = new Material(shader);
        pass = new CRTRenderPass(material, defaultSettings);
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
public class CRTSettings
{
    [Range(0.0f, 3.0f)] public float blurOffset = 1.0f;
    [Range(0.0f, 1.0f)] public float scanlineIntensity = 0.3f;
    [Range(50f, 1000f)] public float scanlineCount = 300f;
    [Range(0.0f, 5.0f)] public float scanlineSpeed = 0.5f;
    [Range(0.0f, 1.0f)] public float vignetteIntensity = 0.3f;
    [Range(0.01f, 1.0f)] public float vignetteSmoothness = 0.3f;
    public bool usePhosphor = false;
    [Range(0.0f, 1.0f)] public float phosphorIntensity = 0.15f;
    [Range(1.0f, 6.0f)] public float phosphorScale = 2.0f;
}

// Summary: Render pass that blits the camera colour through the CRT material.
public class CRTRenderPass : ScriptableRenderPass
{
    private static readonly int BlurOffsetID = Shader.PropertyToID("_BlurOffset");
    private static readonly int ScanlineIntensityID = Shader.PropertyToID("_ScanlineIntensity");
    private static readonly int ScanlineCountID = Shader.PropertyToID("_ScanlineCount");
    private static readonly int ScanlineSpeedID = Shader.PropertyToID("_ScanlineSpeed");
    private static readonly int VignetteIntensityID = Shader.PropertyToID("_VignetteIntensity");
    private static readonly int VignetteSmoothnessID = Shader.PropertyToID("_VignetteSmoothness");
    private static readonly int UsePhosphorID = Shader.PropertyToID("_UsePhosphor");
    private static readonly int PhosphorIntensityID = Shader.PropertyToID("_PhosphorIntensity");
    private static readonly int PhosphorScaleID = Shader.PropertyToID("_PhosphorScale");
    private const string PassName = "CRTRenderPass";

    private CRTSettings defaultSettings;
    private Material material;

    public CRTRenderPass(Material material, CRTSettings defaultSettings)
    {
        this.material = material;
        this.defaultSettings = defaultSettings;
    }

    private void UpdateSettings()
    {
        if (material == null) return;

        var vol = VolumeManager.instance.stack.GetComponent<CRTVolume>();

        float blurOffset = vol.blurOffset.overrideState ? vol.blurOffset.value : defaultSettings.blurOffset;
        float scanlineIntensity = vol.scanlineIntensity.overrideState ? vol.scanlineIntensity.value : defaultSettings.scanlineIntensity;
        float scanlineCount = vol.scanlineCount.overrideState ? vol.scanlineCount.value : defaultSettings.scanlineCount;
        float scanlineSpeed = vol.scanlineSpeed.overrideState ? vol.scanlineSpeed.value : defaultSettings.scanlineSpeed;
        float vignetteIntensity = vol.vignetteIntensity.overrideState ? vol.vignetteIntensity.value : defaultSettings.vignetteIntensity;
        float vignetteSmoothness = vol.vignetteSmoothness.overrideState ? vol.vignetteSmoothness.value : defaultSettings.vignetteSmoothness;
        bool usePhosphor = vol.usePhosphor.overrideState ? vol.usePhosphor.value : defaultSettings.usePhosphor;
        float phosphorIntensity = vol.phosphorIntensity.overrideState ? vol.phosphorIntensity.value : defaultSettings.phosphorIntensity;
        float phosphorScale = vol.phosphorScale.overrideState ? vol.phosphorScale.value : defaultSettings.phosphorScale;

        material.SetFloat(BlurOffsetID, blurOffset);
        material.SetFloat(ScanlineIntensityID, scanlineIntensity);
        material.SetFloat(ScanlineCountID, scanlineCount);
        material.SetFloat(ScanlineSpeedID, scanlineSpeed);
        material.SetFloat(VignetteIntensityID, vignetteIntensity);
        material.SetFloat(VignetteSmoothnessID, vignetteSmoothness);
        material.SetFloat(UsePhosphorID, usePhosphor ? 1.0f : 0.0f);
        material.SetFloat(PhosphorIntensityID, phosphorIntensity);
        material.SetFloat(PhosphorScaleID, phosphorScale);
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

        if (material == null) return;
        if (resourceData.isActiveTargetBackBuffer) return;

        TextureHandle src = resourceData.activeColorTexture;

        var desc = src.GetDescriptor(renderGraph);
        desc.name = "_CRTTexture";
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