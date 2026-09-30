using UnityEngine;

public class InputGlyphs : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject keyboardGlyphs;
    [SerializeField] private GameObject gamepadGlyphs;

    void onEnable()
    {
        PlayerInputReader.OnActiveDeviceChanged += SetVisual;

        bool isGamepad = PlayerInputReader.Instance != null && PlayerInputReader.Instance.IsGamepadActive;
        SetVisual(isGamepad);
    }

    void OnDisable()
    {
        PlayerInputReader.OnActiveDeviceChanged -= SetVisual;
    }

    void SetVisual(bool isGamepad)
    {
        if (keyboardGlyphs != null) keyboardGlyphs.SetActive(!isGamepad);
        if (gamepadGlyphs != null) gamepadGlyphs.SetActive(isGamepad);
    }
}