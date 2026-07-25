using UnityEngine;

public class GrimoireFearStates : MonoBehaviour
{
    [SerializeField] Material grimoireMaterial;
    [SerializeField] Texture fineTexture, fineEmission, medTexture, medEmission, highTexture, highEmission;
    [SerializeField] FearBar fearBar;
    public bool debugMode;

    void Start()
    {
        grimoireMaterial.SetTexture("_BaseMap", fineTexture);
        grimoireMaterial.SetTexture("_EmissionMap", fineEmission);

        if (debugMode)
        {
            foreach (var localKeywordName in grimoireMaterial.shaderKeywords)
            {
                Debug.Log("Local shader keyword " + localKeywordName + " is currently enabled");
            }
        }
    }

    // EDIT (auto-resolve): fallback for cross-prefab references.
    private void Awake()
    {
        if (fearBar == null)
            fearBar = FindAnyObjectByType<FearBar>();

        if (fearBar != null)
            fearBar.OnFearChanged += HandleFearChanged;
    }

    private void OnDestroy()
    {
        if (fearBar != null)
            fearBar.OnFearChanged -= HandleFearChanged;
    }

    private void HandleFearChanged(FearBar.FearRank rank)
    {
        if (debugMode) Debug.Log("fearLevel updated to: " + rank);
        if (debugMode) Debug.Log("Grimoire texture is now set to: " + grimoireMaterial.mainTexture);

        switch (rank) // ek, meet switch statement
        {
            case FearBar.FearRank.Fine:
                grimoireMaterial.SetTexture("_BaseMap", fineTexture);
                grimoireMaterial.SetTexture("_EmissionMap", fineEmission); // this emission map
                break;
            case FearBar.FearRank.Low:
                grimoireMaterial.SetTexture("_BaseMap", medTexture);
                grimoireMaterial.SetTexture("_EmissionMap", medEmission);
                break;
            case FearBar.FearRank.Medium:
                grimoireMaterial.SetTexture("_BaseMap", medTexture);
                grimoireMaterial.SetTexture("_EmissionMap", medEmission);
                break;
            case FearBar.FearRank.High:
                grimoireMaterial.SetTexture("_BaseMap", highTexture);
                grimoireMaterial.SetTexture("_EmissionMap", highEmission);
                break;
        }

        if (debugMode) Debug.Log("fearLevel updated to: " + rank);
        if (debugMode) Debug.Log("Grimoire texture is now set to: " + grimoireMaterial.mainTexture);
    }
}
