using UnityEngine;

public enum PromptSurface
{
    None,
    Hud,
    WorldSpace,
}

// Summary: A single resolved interaction prompt. Interactables return one of these from ResolvePrompt.
public struct InteractionPrompt
{
    public PromptSurface surface;
    public string label;        // e.g. "Locked", "Open", "Use Key"
    public string actionName;   // input action to draw a glyph for (HUD only); null/empty = no glyph
    public Transform anchor;    // world-space point the label floats at (WorldSpace only)

    public static InteractionPrompt None => new InteractionPrompt { surface = PromptSurface.None };
    public bool HasPrompt => surface != PromptSurface.None;
}
