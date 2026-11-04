using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Summary: A slider row on the Graphics settings page. Pick the setting it controls in the inspector and it binds itself
// to GraphicsSettingsManager, which also sets the slider's range and the value label's format.
// For a "toggle and slider" setting, use a GraphicsToggleRow and a GraphicsSliderRow together. The slider greys out while the toggle is off.
public class GraphicsSliderRow : MonoBehaviour
{
    [Header("Setting")]
    [SerializeField] private GraphicsSlider setting;

    [Header("References")]
    [SerializeField] private Slider slider;
    [Tooltip("Optional. Shows the current value (e.g. 50%, 2x, 16).")]
    [SerializeField] private TMP_Text valueLabel;
    [Tooltip("Optional. Fades the whole row (label included) while it's not interactable.")]
    [SerializeField] private CanvasGroup rowGroup;
    [SerializeField, Range(0f, 1f)] private float disabledAlpha = 0.4f;

    private GraphicsSettingsManager manager;

    private void Awake()
    {
        if (slider == null)
            slider = GetComponentInChildren<Slider>(true);
    }

    private void OnEnable()
    {
        if (manager == null)
            manager = GraphicsSettingsManager.Instance != null ? GraphicsSettingsManager.Instance : FindAnyObjectByType<GraphicsSettingsManager>();

        if (manager == null || slider == null)
        {
            Debug.LogWarning($"[{this}] Missing GraphicsSettingsManager or Slider.");
            return;
        }

        manager.GetSliderRange(setting, out float min, out float max, out bool wholeNumbers);
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = wholeNumbers;

        manager.OnSettingsChanged += Refresh;
        slider.onValueChanged.AddListener(OnSliderChanged);
        Refresh();
    }

    private void OnDisable()
    {
        if (manager != null)
            manager.OnSettingsChanged -= Refresh;
        if (slider != null)
            slider.onValueChanged.RemoveListener(OnSliderChanged);
    }

    private void OnSliderChanged(float value)
    {
        manager.SetSlider(setting, value);
    }

    private void Refresh()
    {
        float value = manager.GetSlider(setting);
        slider.SetValueWithoutNotify(value);

        if (valueLabel != null)
            valueLabel.SetText(manager.FormatSlider(setting, value));

        bool interactable = manager.IsInteractable(setting);
        slider.interactable = interactable;
        if (rowGroup != null)
            rowGroup.alpha = interactable ? 1f : disabledAlpha;
    }
}
