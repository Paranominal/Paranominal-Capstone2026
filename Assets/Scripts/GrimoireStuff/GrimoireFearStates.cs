using Unity.VisualScripting;
using UnityEngine;

public class GrimoireFearStates : MonoBehaviour
{
    [SerializeField] Material grimMaterial;
    [SerializeField] Texture fineTexture, fineEmission, medTexture, medEmission, woundedTexture, woundedEmission;
    [Range(0, 3)]
    [SerializeField] int fearLevel;
    private Texture startTexture;
    public bool debugMode;
    void Start()
    {
        grimMaterial.SetTexture("_BaseMap", fineTexture);
        grimMaterial.SetTexture("_EmissionMap", fineEmission);

        if(debugMode)foreach (var localKeywordName in grimMaterial.shaderKeywords)
        {
            Debug.Log("Local shader keyword " + localKeywordName + " is currently enabled");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (debugMode) Debug.Log("fearLevel: " + fearLevel);
        if (debugMode) Debug.Log("Grimoire texture is set to: " + grimMaterial.mainTexture);

        if (fearLevel == 0)
        {
            grimMaterial.SetTexture("_BaseMap", fineTexture);
            grimMaterial.SetTexture("_EmissionMap", medEmission);
        }
        else if (fearLevel == 1)
        {
            grimMaterial.SetTexture("_BaseMap", medTexture);
            grimMaterial.SetTexture("_EmissionMap", fineEmission);
        }
        else if (fearLevel == 2)
        {
            grimMaterial.SetTexture("_BaseMap", woundedTexture);
            grimMaterial.SetTexture("_EmissionMap", woundedEmission);
        }
    }
}
