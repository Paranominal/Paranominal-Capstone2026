using UnityEngine;
using UnityEngine.UI;

// Summary: An expandable shelf on the Graphics settings page. Clicking the header shows or hides the shelf's content.
// Shelves are independent, so several can be open at once. Rebuilds the parent layout so the shelves below move to fit.
public class GraphicsShelf : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Header button that expands and minimises the shelf.")]
    [SerializeField] private Button headerButton;
    [Tooltip("Parent of the shelf's setting rows. Shown when expanded.")]
    [SerializeField] private GameObject content;
    [Tooltip("Optional. Rotated to show the shelf's state (e.g. a chevron).")]
    [SerializeField] private RectTransform arrow;
    [Tooltip("Optional. The layout to rebuild when the shelf resizes, usually the ScrollView's Content. Defaults to this shelf's parent.")]
    [SerializeField] private RectTransform layoutRoot;

    [Header("Settings")]
    [SerializeField] private bool startExpanded = false;
    [SerializeField] private float expandedArrowAngle = -90f;
    [SerializeField] private float collapsedArrowAngle = 0f;

    private bool expanded;

    private void Awake()
    {
        if (layoutRoot == null)
            layoutRoot = transform.parent as RectTransform;

        if (headerButton != null)
            headerButton.onClick.AddListener(Toggle);

        SetExpanded(startExpanded);
    }

    public void Toggle()
    {
        SetExpanded(!expanded);
    }

    public void SetExpanded(bool value)
    {
        expanded = value;

        if (content != null)
            content.SetActive(expanded);

        if (arrow != null)
            arrow.localEulerAngles = new Vector3(0f, 0f, expanded ? expandedArrowAngle : collapsedArrowAngle);

        // Layout groups only update next frame by default, which makes the page jump. Rebuild now instead.
        if (layoutRoot != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot);
    }
}
