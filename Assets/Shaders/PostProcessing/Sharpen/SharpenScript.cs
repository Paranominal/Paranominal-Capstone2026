using UnityEngine;

[ExecuteAlways]
public class SharpenScript : MonoBehaviour
{
    [SerializeField] private Material sharpenMaterial;

    [Range(0f, 5f)] public float sharpness = 0.25f;
    [Range(0.25f, 3f)] public float sampleDistance = 1.0f;

    void Update()
    {
        if (sharpenMaterial == null) return;

        sharpenMaterial.SetFloat("_Sharpness", sharpness);
        sharpenMaterial.SetFloat("_SampleDistance", sampleDistance);
    }
}