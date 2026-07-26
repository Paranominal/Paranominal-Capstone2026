using UnityEngine;

// EDIT (grimoire migration): the old ALTGrimoireEntry 'entry' field has been removed.
// ItemDefinition is now the sole source of truth. The 'collectable' and 'scanDuration'
// fields remain here as per-instance configuration until the IScannable refactor.
public class ALTScannableObject : MonoBehaviour
{
    public ItemDefinition itemDefinition;
    public Outline outline; // used for objects that DO NOT have a mesh outline
    public bool collectable;
    public float scanDuration = 2f; // how long should this object take to scan?
    [Tooltip("How many of this item the player receives on pickup.")]
    public int pickupQuantity = 1;

    public bool usesMeshOutline = false;
    public Renderer outlineRenderer; // used for objects that DO have a mesh outline
    public int outlineMaterialIndex = 1;
    private Material instancedOutlineMaterial;
    private Material sharedOutlineMaterial; // stored so we can swap back to it when hiding

    void Start()
    {
        outline = GetComponent<Outline>();
        if (outline != null) // prevent nullrefs for mesh obj
        {
            outline.enabled = false;
            outline.OutlineWidth = 5f;
            outline.OutlineMode = Outline.Mode.OutlineVisible;
        }

        if (usesMeshOutline && outlineRenderer != null)
        {
            // store the shared reference BEFORE instancing, so we can restore it when hiding
            sharedOutlineMaterial = outlineRenderer.sharedMaterials[outlineMaterialIndex];
            // create a per-object instance so colour changes don't affect other objects
            instancedOutlineMaterial = new Material(sharedOutlineMaterial);
        }
    }

    public void SetOutlineColor(Color color)
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

    public void SetOutlineVisible(bool visible)
    {
        if (usesMeshOutline && outlineRenderer != null)
        {
            // swapping which material the renderer references, rather than modifying colour
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
