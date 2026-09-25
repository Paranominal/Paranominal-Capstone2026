using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

// Summary: Renderer Feature that applies a sharpen post-processing effect.
// Reads settings from SharpenVolume if present, otherwise uses defaults from the feature inspector.
public class SharpenRendererFeature : ScriptableRendererFeature
{
    [SerializeField] private Shader shader;
    [SerializeField] private SharpenSettings defaultSettings = new SharpenSettings();

    private Material material;
    private SharpenRenderPass pass;

    public override void Create()
    {
        if (shader == null) return;

        material = new Material(shader);
        pass = new SharpenRenderPass(material, defaultSettings);
        pass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (pass == null) return;

        // Skip Scene View and Preview cameras.
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
public class SharpenSettings
{
    [Range(0f, 5f)] public float sharpness = 0.25f;
    [Range(0.25f, 3f)] public float sampleDistance = 1.0f;
}

// Summary: Render pass that blits the camera colour through the sharpen material.
public class SharpenRenderPass : ScriptableRenderPass
{
    private static readonly int SharpnessID = Shader.PropertyToID("_Sharpness");
    private static readonly int SampleDistanceID = Shader.PropertyToID("_SampleDistance");
    private const string PassName = "SharpenRenderPass";

    private SharpenSettings defaultSettings;
    private Material material;

    public SharpenRenderPass(Material material, SharpenSettings defaultSettings)
    {
        this.material = material;
        this.defaultSettings = defaultSettings;
    }

    private void UpdateSettings()
    {
        if (material == null) return;

        var volume = VolumeManager.instance.stack.GetComponent<SharpenVolume>();

        float sharpness = volume.sharpness.overrideState
            ? volume.sharpness.value : defaultSettings.sharpness;
        float sampleDistance = volume.sampleDistance.overrideState
            ? volume.sampleDistance.value : defaultSettings.sampleDistance;

        material.SetFloat(SharpnessID, sharpness);
        material.SetFloat(SampleDistanceID, sampleDistance);
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

        if (material == null) return;
        if (resourceData.isActiveTargetBackBuffer) return;

        TextureHandle src = resourceData.activeColorTexture;

        var desc = src.GetDescriptor(renderGraph);
        desc.name = "_SharpenTexture";
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
