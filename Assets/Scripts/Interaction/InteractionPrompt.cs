// Summary: A single resolved interaction prompt. Interactables return one of these
// from ResolvePrompt. The controller infers presentation from the fields:
//   - Has actionName → actionable → shown on HUD when aimed.
//   - No actionName  → informational → shown as floating label if a PromptAnchor exists.
public struct InteractionPrompt
{
    public string label;        // e.g. "Locked", "Open", "Unlock (Staff Key)"
    public string actionName;   // input action for glyph; null/empty = informational only

    public static InteractionPrompt None => new InteractionPrompt();
    public bool HasPrompt => !string.IsNullOrEmpty(label);
}
