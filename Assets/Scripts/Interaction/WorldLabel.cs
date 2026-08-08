using UnityEngine;

// Summary: Floating name label for world objects. Place on a child object to define
// the label position (uses this transform's position). The InteractionFocusController
// finds these via proximity and feeds them to the ScreenSpacePromptPool.
// Completely independent of IInteractable.
public class WorldLabel : MonoBehaviour
{
    [Tooltip("Text shown in the floating label.")]
    public string displayName;
}
