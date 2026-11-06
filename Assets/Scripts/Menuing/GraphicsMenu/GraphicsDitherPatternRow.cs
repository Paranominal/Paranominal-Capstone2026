using UnityEngine;
using UnityEngine.UI;

// Summary: Dither pattern tick-boxes on the Graphics settings page. Only one pattern can be active, so ticking one unticks the rest.
// Assign one Toggle per pattern in DitherPattern order (2x2, 4x4, 8x8, 16x16, PS1). Greys out while Dithering is off.
public class GraphicsDitherPatternRow : MonoBehaviour
{
    [Header("References")]
    [Tooltip("One Toggle per pattern, in order: 2x2, 4x4, 8x8, 16x16, PS1.")]
    [SerializeField] private Toggle[] patternToggles = new Toggle[5];
    [Tooltip("Optional. Fades the whole row while it's not interactable.")]
    [SerializeField] private CanvasGroup rowGroup;
    [SerializeField, Range(0f, 1f)] private float disabledAlpha = 0.4f;

    private GraphicsSettingsManager manager;
    private UnityEngine.Events.UnityAction<bool>[] listeners;

    private void OnEnable()
    {
        if (manager == null)
            manager = GraphicsSettingsManager.Instance != null ? GraphicsSettingsManager.Instance : FindAnyObjectByType<GraphicsSettingsManager>();

        if (manager == null)
        {
            Debug.LogWarning($"[{this}] Missing GraphicsSettingsManager.");
            return;
        }

        // Cached so the same delegates can be removed in OnDisable.
        listeners = new UnityEngine.Events.UnityAction<bool>[patternToggles.Length];
        for (int i = 0; i < patternToggles.Length; i++)
        {
            if (patternToggles[i] == null) continue;

            DitherPattern pattern = (DitherPattern)i;
            listeners[i] = isOn => OnPatternToggled(pattern, isOn);
            patternToggles[i].onValueChanged.AddListener(listeners[i]);
        }

        manager.OnSettingsChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (manager != null)
            manager.OnSettingsChanged -= Refresh;

        if (listeners == null) return;
        for (int i = 0; i < patternToggles.Length; i++)
        {
            if (patternToggles[i] != null && listeners[i] != null)
                patternToggles[i].onValueChanged.RemoveListener(listeners[i]);
        }
    }

    private void OnPatternToggled(DitherPattern pattern, bool isOn)
    {
        // Unticking the active pattern isn't allowed, Refresh puts the tick back.
        if (isOn)
            manager.SetDitherPattern(pattern);
        else
            Refresh();
    }

    private void Refresh()
    {
        int active = (int)manager.GetDitherPattern();
        bool interactable = manager.IsDitherPatternInteractable();

        for (int i = 0; i < patternToggles.Length; i++)
        {
            if (patternToggles[i] == null) continue;

            patternToggles[i].SetIsOnWithoutNotify(i == active);
            patternToggles[i].interactable = interactable;
        }

        if (rowGroup != null)
            rowGroup.alpha = interactable ? 1f : disabledAlpha;
    }
}
