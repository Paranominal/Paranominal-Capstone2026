using UnityEngine;

public enum WorldLabelMode
{
    FloatingLabel,
    InteractionPrompt
}

// Summary: Anchor for world-space labels. Place on a child object to define
// the label position. Mode determines presentation behaviour:
// FloatingLabel: billboards toward camera with rotation easing.
// InteractionPrompt: fixed facing from parent transform's forward.
// The InteractionFocusController finds these via proximity and feeds them
// to the WorldLabelPool.
public class WorldLabel : MonoBehaviour
{
    [Tooltip("FloatingLabel: billboards toward camera. InteractionPrompt: faces a fixed direction.")]
    public WorldLabelMode mode = WorldLabelMode.FloatingLabel;

    [Tooltip("Text shown in the floating label. Ignored in InteractionPrompt mode.")]
    public string displayName;
}
