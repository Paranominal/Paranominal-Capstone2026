using UnityEngine;
using UnityEngine.UI;

// Summary: Sets stencil write properties on a UI Graphic material so it can be excluded from fullscreen post-processing effects like downsampling.
[RequireComponent(typeof(Graphic))]
public class UIStencilWriter : MonoBehaviour
{
    private void Awake()
    {
        Graphic graphic = GetComponent<Graphic>();
        Material mat = new Material(graphic.material);
        mat.SetFloat("_Stencil", 1);
        mat.SetFloat("_StencilComp", 8); // Always
        mat.SetFloat("_StencilOp", 2);   // Replace
        graphic.material = mat;
    }
}