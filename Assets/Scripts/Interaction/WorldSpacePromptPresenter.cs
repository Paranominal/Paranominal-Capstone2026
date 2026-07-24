using UnityEngine;
using TMPro;

// Summary: One shared world-space label that follows the focused interactable's anchor,
// billboards toward the camera, and fades in/out. The focus controller calls SetTarget/Clear
// every frame; this component owns the easing and positioning.
public class WorldSpacePromptPresenter : MonoBehaviour
{
    [SerializeField] private CanvasGroup group;    // controls fade
    [SerializeField] private TMP_Text label;       // the label text element
    [SerializeField] private Transform root;       // object moved to the anchor (defaults to this transform)
    [SerializeField] private float fadeSpeed = 8f;

    private float targetAlpha;
    private float currentAlpha;
    private string pendingText;
    private Transform anchor;
    private bool visible = true;   // master gate, driven by the grimoire toggle

    void Awake()
    {
        if (root == null)
            root = transform;
    }

    // Summary: Show this prompt at its anchor, easing toward the given alpha.
    public void SetTarget(InteractionPrompt prompt, float alpha)
    {
        targetAlpha = Mathf.Clamp01(alpha);
        pendingText = prompt.label;
        anchor = prompt.anchor;
    }

    // Summary: Fade the label out.
    public void Clear()
    {
        targetAlpha = 0f;
    }

    // Summary: Master on/off gate. Hiding snaps to invisible immediately; unhiding lets the
    // label fade back in from zero rather than popping at a stale alpha.
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

    void LateUpdate()
    {
        if (!visible)
            return;

        if (targetAlpha > 0f)
        {
            if (label != null && !string.IsNullOrEmpty(pendingText))
                label.text = pendingText;

            if (anchor != null)
                root.position = anchor.position;

            // Billboard: face the label toward the camera.
            if (Camera.main != null)
                root.rotation = Quaternion.LookRotation(root.position - Camera.main.transform.position);
        }

        // Unscaled: the grimoire sets timeScale to 0, and the fade should still resolve.
        currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed * Time.unscaledDeltaTime);
        if (group != null)
            group.alpha = currentAlpha;
    }
}
