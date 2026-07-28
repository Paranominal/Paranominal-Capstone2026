using UnityEngine;

// Summary: Handles scan outline rendering for both Outline-component and mesh-material-swap approaches.
// Attach alongside WorldItem or ScanTarget. Both delegate their outline calls here.
public class ScanOutline : MonoBehaviour
{
    [Header("Outline Component Path")]
    public Outline outline;

    [Header("Mesh Outline Path")]
    public bool usesMeshOutline = false;
    public Renderer outlineRenderer;
    public int outlineMaterialIndex = 1;

    private Material instancedOutlineMaterial;
    private Material sharedOutlineMaterial;

    // EDIT (outline-fix): moved from Start to Awake. The Outline component renders on OnEnable,
    // which fires before Start, leaving a window where outlines are visible with no one hiding them.
    void Awake()
    {
        if (outline == null)
            outline = GetComponent<Outline>();

        if (outline != null)
        {
            outline.enabled = false;
            outline.OutlineWidth = 5f;
            outline.OutlineMode = Outline.Mode.OutlineVisible;
        }

        if (usesMeshOutline && outlineRenderer != null)
        {
            sharedOutlineMaterial = outlineRenderer.sharedMaterials[outlineMaterialIndex];
            instancedOutlineMaterial = new Material(sharedOutlineMaterial);
        }
    }

    public void SetColor(Color color)
    {
        if (usesMeshOutline && instancedOutlineMaterial != null)
        {
            instancedOutlineMaterial.SetColor("_BaseColor", color);
        }
        else if (outline != null)
        {
            outline.OutlineColor = color;
        }
    }

    public void SetVisible(bool visible)
    {
        if (usesMeshOutline && outlineRenderer != null)
        {
            Material[] mats = outlineRenderer.sharedMaterials;
            mats[outlineMaterialIndex] = visible ? instancedOutlineMaterial : sharedOutlineMaterial;
            outlineRenderer.sharedMaterials = mats;
        }
        else if (outline != null)
        {
            outline.enabled = visible;
        }
    }
}