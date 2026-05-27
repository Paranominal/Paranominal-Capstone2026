using UnityEngine;

public class GrimoireFearStates : MonoBehaviour
{
    [SerializeField] Material grimoireMaterial;
    [SerializeField] Texture fineTexture, fineEmission, medTexture, medEmission, woundedTexture, woundedEmission;
    [SerializeField] FearBar fearBar;
    public bool debugMode;

    void Start()
    {
        grimoireMaterial.SetTexture("_BaseMap", fineTexture);
        grimoireMaterial.SetTexture("_EmissionMap", fineEmission);

        foreach (var localKeywordName in grimoireMaterial.shaderKeywords)
        {
            Debug.Log("Local shader keyword " + localKeywordName + " is currently enabled");
        }
    }

    private void Awake()
    {
        fearBar.OnFearChanged += HandleFearChanged;
    }

    private void HandleFearChanged(FearBar.FearRank rank)
    {
        if (debugMode) Debug.Log("fearLevel updated to: " + rank);
        if (debugMode) Debug.Log("Grimoire texture is now set to: " + grimoireMaterial.mainTexture);

        switch (rank) // ek, meet switch statement
        {
            case FearBar.FearRank.Fine:
            case FearBar.FearRank.High:
                grimoireMaterial.SetTexture("_BaseMap", fineTexture);
                grimoireMaterial.SetTexture("_EmissionMap", fineEmission); // this emission map
                break;
            case FearBar.FearRank.Medium:
                grimoireMaterial.SetTexture("_BaseMap", medTexture);
                grimoireMaterial.SetTexture("_EmissionMap", medEmission); // and this one were swapped initally - i assume it was a mistake?
                break;
            case FearBar.FearRank.Low:
                grimoireMaterial.SetTexture("_BaseMap", woundedTexture);
                grimoireMaterial.SetTexture("_EmissionMap", woundedEmission);
                break;
        }

        if (debugMode) Debug.Log("fearLevel updated to: " + rank);
        if (debugMode) Debug.Log("Grimoire texture is now set to: " + grimoireMaterial.mainTexture);
    }
}