using UnityEngine;

[ExecuteAlways]
public class CRT : MonoBehaviour
{
    [Header("CRT Settings")]
    [SerializeField] private Material crtMat;
    [SerializeField] private Texture image;
    [SerializeField] private bool useImage = true;

    [Range(1.0f, 10.0f)]
    public float curvature = 1.0f;

    [Range(1.0f, 100.0f)]
    public float vignetteWidth = 30.0f;

    private static readonly int CurvatureID = Shader.PropertyToID("_Curvature");
    private static readonly int VignetteWidthID = Shader.PropertyToID("_VignetteWidth");
    private static readonly int OverlayTexID = Shader.PropertyToID("_OverlayTex");
    private static readonly int UseOverlayTexID = Shader.PropertyToID("_UseOverlayTex");

    void Update()
    {
        if (crtMat == null) return;

        crtMat.SetFloat(CurvatureID, curvature);
        crtMat.SetFloat(VignetteWidthID, vignetteWidth);
        crtMat.SetTexture(OverlayTexID, image);
        crtMat.SetFloat(UseOverlayTexID, useImage && image != null ? 1.0f : 0.0f);
    }
}