// Summary: Drives the Dissolve shader's _DissolveAmount from 0 to 1 over a configurable duration.
// Call Play() on enemy death or object destruction. If the object uses a non-dissolve shader,
// swaps to the provided dissolve material at play time, copying the texture and color from the original.

using UnityEngine;
using System;
using System.Collections;

public class DissolveEffect : MonoBehaviour
{
    [Tooltip("How long the dissolve takes in seconds.")]
    [SerializeField] private float dissolveDuration = 1.5f;
    [Tooltip("Destroy this GameObject when the dissolve finishes.")]
    [SerializeField] private bool destroyOnComplete = true;
    [Tooltip("Dissolve material to swap to if the object uses a different shader. Leave empty if it already uses a dissolve shader.")]
    [SerializeField] private Material dissolveMaterial = null;

    public event Action OnDissolveComplete;

    private Renderer targetRenderer;
    private Material material;
    private static readonly int DissolveAmountID = Shader.PropertyToID("_DissolveAmount");
    private static readonly int MainTexID = Shader.PropertyToID("_MainTex");
    private static readonly int ColorID = Shader.PropertyToID("_Color");
    private static readonly int BaseMapID = Shader.PropertyToID("_BaseMap");
    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

    private void Awake()
    {
        targetRenderer = GetComponent<Renderer>();
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();
    }

    public void Play()
    {
        if (targetRenderer == null) return;

        if (targetRenderer.material.HasProperty(DissolveAmountID))
        {
            // already a dissolve shader, just create an instance
            material = new Material(targetRenderer.material);
        }
        else if (dissolveMaterial != null)
        {
            // swap to dissolve material, copying texture and colour from original
            Material original = targetRenderer.material;
            material = new Material(dissolveMaterial);
            CopyTextureAndColor(original, material);
        }
        else
        {
            return;
        }

        material.SetFloat(DissolveAmountID, 0f);
        targetRenderer.material = material;

        // disable colliders so the dissolving object doesn't block movement
        foreach (Collider col in GetComponentsInChildren<Collider>())
            col.enabled = false;

        StartCoroutine(DissolveCoroutine());
    }

    private void CopyTextureAndColor(Material from, Material to)
    {
        // URP Lit uses _BaseMap/_BaseColor, standard/custom shaders use _MainTex/_Color
        Texture tex = null;
        Color col = Color.white;

        if (from.HasProperty(BaseMapID))
            tex = from.GetTexture(BaseMapID);
        else if (from.HasProperty(MainTexID))
            tex = from.GetTexture(MainTexID);

        if (from.HasProperty(BaseColorID))
            col = from.GetColor(BaseColorID);
        else if (from.HasProperty(ColorID))
            col = from.GetColor(ColorID);

        // no base texture (e.g. procedural shaders), make transparent so only burn edges show
        if (tex == null)
            col.a = 0f;

        if (tex != null && to.HasProperty(MainTexID))
            to.SetTexture(MainTexID, tex);
        if (to.HasProperty(ColorID))
            to.SetColor(ColorID, col);
    }

    private IEnumerator DissolveCoroutine()
    {
        float elapsed = 0f;
        while (elapsed < dissolveDuration)
        {
            elapsed += Time.deltaTime;
            material.SetFloat(DissolveAmountID, Mathf.Clamp01(elapsed / dissolveDuration));
            yield return null;
        }

        material.SetFloat(DissolveAmountID, 1f);
        OnDissolveComplete?.Invoke();

        if (destroyOnComplete)
            Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (material != null) Destroy(material);
    }
}