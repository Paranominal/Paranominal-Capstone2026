// Summary: Drives the Dissolve shader's _DissolveAmount from 0 to 1 on every configured renderer.
// If no renderers are assigned, all renderers on this object and its children are used.
// If an object uses a non-dissolve shader, its material is replaced at play time while preserving its texture and colour.

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class DissolveEffect : MonoBehaviour
{
    [Tooltip("How long the dissolve takes in seconds.")]
    [SerializeField] private float dissolveDuration = 1.5f;
    [Tooltip("Destroy this GameObject when the dissolve finishes.")]
    [SerializeField] private bool destroyOnComplete = true;
    [Tooltip("Dissolve material to swap to if an object uses a different shader. Leave empty if every material already uses a dissolve shader.")]
    [SerializeField] private Material dissolveMaterial;
    [Tooltip("Renderers affected by this dissolve. If empty, all renderers on this object and its children are gathered automatically.")]
    [SerializeField] private Renderer[] targetRenderers;

    public event Action OnDissolveComplete;

    private readonly List<Material> runtimeMaterials = new List<Material>();
    private bool isPlaying;

    private static readonly int DissolveAmountID = Shader.PropertyToID("_DissolveAmount");
    private static readonly int MainTexID = Shader.PropertyToID("_MainTex");
    private static readonly int ColorID = Shader.PropertyToID("_Color");
    private static readonly int BaseMapID = Shader.PropertyToID("_BaseMap");
    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

    private void Awake()
    {
        GatherRenderersIfNeeded();
    }

    public void Play()
    {
        if (isPlaying)
            return;

        GatherRenderersIfNeeded();

        if (!PrepareMaterials())
        {
            Debug.LogWarning(
                $"[{name}] DissolveEffect could not find a renderer/material with a _DissolveAmount property.",
                this);
            CompleteDissolve();
            return;
        }

        isPlaying = true;
        StartCoroutine(DissolveCoroutine());
    }

    private void GatherRenderersIfNeeded()
    {
        if (targetRenderers == null || targetRenderers.Length == 0)
            targetRenderers = GetComponentsInChildren<Renderer>(true);
    }

    private bool PrepareMaterials()
    {
        runtimeMaterials.Clear();

        if (targetRenderers == null)
            return false;

        foreach (Renderer targetRenderer in targetRenderers)
        {
            if (targetRenderer == null)
                continue;

            Material[] originalMaterials = targetRenderer.sharedMaterials;
            Material[] replacementMaterials = new Material[originalMaterials.Length];

            for (int i = 0; i < originalMaterials.Length; i++)
            {
                Material original = originalMaterials[i];

                if (original == null)
                    continue;

                Material replacement = CreateDissolveMaterial(original);

                if (replacement == null)
                {
                    replacementMaterials[i] = original;
                    continue;
                }

                replacement.SetFloat(DissolveAmountID, 0f);
                replacementMaterials[i] = replacement;
                runtimeMaterials.Add(replacement);
            }

            targetRenderer.materials = replacementMaterials;
        }

        return runtimeMaterials.Count > 0;
    }

    private Material CreateDissolveMaterial(Material original)
    {
        if (original.HasProperty(DissolveAmountID))
            return new Material(original);

        if (dissolveMaterial == null || !dissolveMaterial.HasProperty(DissolveAmountID))
            return null;

        Material replacement = new Material(dissolveMaterial);
        CopyTextureAndColor(original, replacement);
        return replacement;
    }

    private void CopyTextureAndColor(Material from, Material to)
    {
        Texture texture = null;
        Color colour = Color.white;

        if (from.HasProperty(BaseMapID))
            texture = from.GetTexture(BaseMapID);
        else if (from.HasProperty(MainTexID))
            texture = from.GetTexture(MainTexID);

        if (from.HasProperty(BaseColorID))
            colour = from.GetColor(BaseColorID);
        else if (from.HasProperty(ColorID))
            colour = from.GetColor(ColorID);

        if (texture != null)
        {
            if (to.HasProperty(BaseMapID))
                to.SetTexture(BaseMapID, texture);

            if (to.HasProperty(MainTexID))
                to.SetTexture(MainTexID, texture);
        }

        if (to.HasProperty(BaseColorID))
            to.SetColor(BaseColorID, colour);

        if (to.HasProperty(ColorID))
            to.SetColor(ColorID, colour);
    }

    private IEnumerator DissolveCoroutine()
    {
        float duration = Mathf.Max(dissolveDuration, 0.01f);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetDissolveAmount(Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        SetDissolveAmount(1f);
        CompleteDissolve();
    }

    private void SetDissolveAmount(float amount)
    {
        foreach (Material runtimeMaterial in runtimeMaterials)
        {
            if (runtimeMaterial != null)
                runtimeMaterial.SetFloat(DissolveAmountID, amount);
        }
    }

    private void CompleteDissolve()
    {
        isPlaying = false;
        OnDissolveComplete?.Invoke();

        if (destroyOnComplete)
            Destroy(gameObject);
    }

    private void OnDestroy()
    {
        foreach (Material runtimeMaterial in runtimeMaterials)
        {
            if (runtimeMaterial != null)
                Destroy(runtimeMaterial);
        }

        runtimeMaterials.Clear();
    }
}
