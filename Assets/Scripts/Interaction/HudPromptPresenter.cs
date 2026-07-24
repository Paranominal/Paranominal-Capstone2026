using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

// Summary: Draws the HUD interaction prompt (glyph + label) and fades it in/out.
// Point this at a text element under your existing HUD canvas. The focus controller
// calls SetTarget/Clear every frame; this component owns the easing.
public class HudPromptPresenter : MonoBehaviour
{
    [SerializeField] private CanvasGroup group;    // controls fade; parent of the prompt text
    [SerializeField] private TMP_Text label;       // the prompt text element
    [SerializeField] private float fadeSpeed = 8f;

    private float targetAlpha;
    private float currentAlpha;
    private string pendingText;
    private bool visible = true;   // master gate, driven by PlayerHUD.UIVisible / grimoire toggle

    // Summary: Show this prompt, easing toward the given alpha.
    public void SetTarget(InteractionPrompt prompt, float alpha)
    {
        targetAlpha = Mathf.Clamp01(alpha);
        pendingText = Compose(prompt);
    }

    // Summary: Fade the prompt out (keeps last text while it eases away).
    public void Clear()
    {
        targetAlpha = 0f;
    }

    // Summary: Master on/off gate. Hiding snaps to invisible immediately; unhiding lets the
    // prompt fade back in from zero rather than popping at a stale alpha.
    public void SetVisible(bool state)
    {
        visible = state;
        if (!visible)
        {
            targetAlpha = 0f;
            currentAlpha = 0f;
            if (group != null)
                group.alpha = 0f;
        }
    }

    void Update()
    {
        if (!visible)
            return;

        // Only refresh text while we want it visible, so a fade-out keeps the last string.
        if (targetAlpha > 0f && label != null && !string.IsNullOrEmpty(pendingText))
            label.text = pendingText;

        // Unscaled: the grimoire sets timeScale to 0, and the fade should still resolve.
        currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed * Time.unscaledDeltaTime);
        if (group != null)
            group.alpha = currentAlpha;
    }

    private string Compose(InteractionPrompt prompt)
    {
        string glyph = ResolveGlyph(prompt.actionName);
        if (string.IsNullOrEmpty(glyph))
            return prompt.label;
        return $"{glyph} - {prompt.label}";
    }

    // Summary: Pulls the current binding display string (e.g. "E") from the named action,
    // so the glyph follows rebinds and the active control scheme.
    private string ResolveGlyph(string actionName)
    {
        if (string.IsNullOrEmpty(actionName))
            return null;

        InputAction action = InputSystem.actions?.FindAction(actionName);
        if (action == null)
            return null;

        return action.GetBindingDisplayString();
    }
}
