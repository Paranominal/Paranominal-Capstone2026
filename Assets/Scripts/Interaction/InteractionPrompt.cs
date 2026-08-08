// Summary: A single resolved interaction prompt. Every prompt is actionable (shown on
// the HUD with an input glyph when aimed). Informational floating labels are handled
// separately by WorldLabel.
public struct InteractionPrompt
{
    public string label;        // e.g. "Open", "Pick Up", "Brew"
    public string actionName;   // input action for glyph, e.g. "Collect"

    public static InteractionPrompt None => new InteractionPrompt();
    public bool HasPrompt => !string.IsNullOrEmpty(label);
}