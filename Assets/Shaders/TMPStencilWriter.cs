using UnityEngine;
using TMPro;

// Summary: Sets stencil write properties on a TMP material so it can be excluded from fullscreen post-processing effects like downsampling.
[RequireComponent(typeof(TMP_Text))]
public class TMPStencilWriter : MonoBehaviour
{
    private void Awake()
    {
        TMP_Text tmp = GetComponent<TMP_Text>();
        Material mat = tmp.fontMaterial; // creates a unique instance automatically
        mat.SetFloat("_Stencil", 1);
        mat.SetFloat("_StencilComp", 8); // Always
        mat.SetFloat("_StencilOp", 2);   // Replace
    }
}