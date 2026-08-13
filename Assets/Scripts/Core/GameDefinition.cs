using UnityEngine;

// Summary: Shared base for player-facing definitions (items, spells).
// Carries identity fields common to anything that appears in the UI or quickslots.
// EnemyDefinition is intentionally separate and does not extend this.
public abstract class GameDefinition : ScriptableObject
{
    [Tooltip("Stable unique identifier. Used for save files and debugging. Never change once shipped.")]
    public string id;

    [Tooltip("Name shown to the player in the UI.")]
    public string displayName;

    [TextArea(2, 4)]
    public string description;

    [TextArea(2, 4)]
    public string flavourText;

    [TextArea(2, 4)]
    public string hintText;

    public Sprite icon;
}
