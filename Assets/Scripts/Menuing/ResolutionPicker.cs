// Summary: Resolution picker for the settings screen. 
// Populates a TMP_Dropdown with target resolutions from RenderResolutionManager, marks unsupported ones with an asterisk,
// and applies the player's selection via URP render scale.

using UnityEngine;
using TMPro;

public class ResolutionPicker : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown resolutionDropdown;

    private Vector2Int[] resolutions;

    private void OnEnable()
    {
        if (resolutionDropdown == null)
            resolutionDropdown = GetComponent<TMP_Dropdown>();

        PopulateDropdown();
    }

    private void PopulateDropdown()
    {
        resolutionDropdown.ClearOptions();

        RenderResolutionManager manager = RenderResolutionManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning("ResolutionPicker: RenderResolutionManager not found.");
            return;
        }

        resolutions = manager.GetTargetResolutions();
        Vector2Int selected = manager.SelectedResolution;

        var options = new System.Collections.Generic.List<string>();
        int currentIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            Vector2Int res = resolutions[i];
            string label = $"{res.x} x {res.y}";

            // Mark unsupported resolutions with an asterisk.
            if (!manager.IsResolutionSupported(res))
                label += " *";

            options.Add(label);

            // Match against the manager's selected resolution.
            if (res.x == selected.x && res.y == selected.y)
                currentIndex = i;
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.SetValueWithoutNotify(currentIndex);
        resolutionDropdown.onValueChanged.AddListener(OnResolutionSelected);
    }

    private void OnResolutionSelected(int index)
    {
        if (resolutions == null || index < 0 || index >= resolutions.Length) return;

        Vector2Int selected = resolutions[index];
        RenderResolutionManager.Instance.SetResolution(selected.x, selected.y);
    }

    private void OnDisable()
    {
        if (resolutionDropdown != null)
            resolutionDropdown.onValueChanged.RemoveListener(OnResolutionSelected);
    }
}