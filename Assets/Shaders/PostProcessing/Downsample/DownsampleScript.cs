using UnityEngine;

[ExecuteAlways]
public class DownsampleScript : MonoBehaviour
{
    [SerializeField] private Material pixelateMaterial;
    [Range(1f, 512f)] public float pixelSize = 4f;

    void Update()
    {
        if (pixelateMaterial == null) return;

        pixelateMaterial.SetFloat("_PixelSize", pixelSize);
    }
}