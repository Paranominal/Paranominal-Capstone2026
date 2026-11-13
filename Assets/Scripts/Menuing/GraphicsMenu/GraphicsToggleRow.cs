using UnityEngine;
using UnityEngine.UI;

// Summary: A toggle row on the Graphics settings page. Pick the setting it controls in the inspector and it binds itself
// to GraphicsSettingsManager. Refreshes its value and greyed-out state whenever any setting changes.
public class GraphicsToggleRow : MonoBehaviour
{
    [Header("Setting")]
    [SerializeField] private GraphicsToggle setting;

    [Header("References")]
    [SerializeField] private Toggle toggle;
    [Tooltip("Optional. Fades the whole row (label included) while it's not interactable.")]
    [SerializeField] private CanvasGroup rowGroup;
    [SerializeField, Range(0f, 1f)] private float disabledAlpha = 0.4f;

    private GraphicsSettingsManager manager;

    private void Awake()
    {
        if (toggle == null)
            toggle = GetComponentInChildren<Toggle>(true);
    }

    private void OnEnable()
    {
        if (manager == null)
            manager = GraphicsSettingsManager.Instance != null ? GraphicsSettingsManager.Instance : FindAnyObjectByType<GraphicsSettingsManager>();

        if (manager == null || toggle == null)
        {
            Debug.LogWarning($"[{this}] Missing GraphicsSettingsManager or Toggle.");
            return;
        }

        manager.OnSettingsChanged += Refresh;
        toggle.onValueChanged.AddListener(OnToggleChanged);
        Refresh();
    }

    private void OnDisable()
    {
        if (manager != null)
            manager.OnSettingsChanged -= Refresh;
        if (toggle != null)
            toggle.onValueChanged.RemoveListener(OnToggleChanged);
    }

    private void OnToggleChanged(bool value)
    {
        manager.SetToggle(setting, value);
    }

    private void Refresh()
    {
        toggle.SetIsOnWithoutNotify(manager.GetToggle(setting));

        bool interactable = manager.IsInteractable(setting);
        toggle.interactable = interactable;
        if (rowGroup != null)
            rowGroup.alpha = interactable ? 1f : disabledAlpha;
    }
}
