using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ComboUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ComboSystem comboSystem;
    [SerializeField] private GameObject comboVisuals;   // the parent object to be disabled
    [SerializeField] private Slider comboSlider;        
    [SerializeField] private TextMeshProUGUI multiplierText;

    private void Awake()
    {
        comboSystem.OnComboChanged += HandleComboChanged;
        comboSystem.OnComboEnded += HandleComboEnded;

        comboSlider.interactable = false;
        comboVisuals.SetActive(false);
    }

    private void Update()
    {
        if (!comboSystem.IsActive)
        {
            return;
        }
        comboSlider.value = comboSystem.TimeRemaining / comboSystem.Duration;
    }

    private void HandleComboChanged(float multiplier)
    {
        multiplierText.text = $"{1f + multiplier:0.0}x";

        if (!comboVisuals.activeSelf)
        {
            comboVisuals.SetActive(true);
        }
    }

    private void HandleComboEnded()
    {
        comboVisuals.SetActive(false);
        comboSlider.value = 0f;
    }
}
