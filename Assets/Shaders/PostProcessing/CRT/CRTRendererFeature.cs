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
    [Range(1.0f, 10.0f)] public float curvature = 1.0f;
    [Range(1.0f, 100.0f)] public float vignetteWidth = 30.0f;
    [Range(0f, 1f)] public float scanlineIntensity = 0.3f;
    [Range(50f, 1000f)] public float scanlineCount = 300f;
    [Range(0f, 0.2f)] public float cornerRadius = 0.05f;
    [Range(1f, 100f)] public float cornerSharpness = 20f;
    [Range(0f, 1f)] public float phosphorIntensity = 0.15f;
}

// Summary: Render pass that blits the camera colour through the CRT material.
public class CRTRenderPass : ScriptableRenderPass
{
    private static readonly int CurvatureID = Shader.PropertyToID("_Curvature");
    private static readonly int VignetteWidthID = Shader.PropertyToID("_VignetteWidth");
    private static readonly int ScanlineIntensityID = Shader.PropertyToID("_ScanlineIntensity");
    private static readonly int ScanlineCountID = Shader.PropertyToID("_ScanlineCount");
    private static readonly int CornerRadiusID = Shader.PropertyToID("_CornerRadius");
    private static readonly int CornerSharpnessID = Shader.PropertyToID("_CornerSharpness");
    private static readonly int PhosphorIntensityID = Shader.PropertyToID("_PhosphorIntensity");
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

        float curvature = vol.curvature.overrideState ? vol.curvature.value : defaultSettings.curvature;
        float vignetteWidth = vol.vignetteWidth.overrideState ? vol.vignetteWidth.value : defaultSettings.vignetteWidth;
        float scanlineIntensity = vol.scanlineIntensity.overrideState ? vol.scanlineIntensity.value : defaultSettings.scanlineIntensity;
        float scanlineCount = vol.scanlineCount.overrideState ? vol.scanlineCount.value : defaultSettings.scanlineCount;
        float cornerRadius = vol.cornerRadius.overrideState ? vol.cornerRadius.value : defaultSettings.cornerRadius;
        float cornerSharpness = vol.cornerSharpness.overrideState ? vol.cornerSharpness.value : defaultSettings.cornerSharpness;
        float phosphorIntensity = vol.phosphorIntensity.overrideState ? vol.phosphorIntensity.value : defaultSettings.phosphorIntensity;

        material.SetFloat(CurvatureID, curvature);
        material.SetFloat(VignetteWidthID, vignetteWidth);
        material.SetFloat(ScanlineIntensityID, scanlineIntensity);
        material.SetFloat(ScanlineCountID, scanlineCount);
        material.SetFloat(CornerRadiusID, cornerRadius);
        material.SetFloat(CornerSharpnessID, cornerSharpness);
        material.SetFloat(PhosphorIntensityID, phosphorIntensity);
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

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