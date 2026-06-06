using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerFOVAdjuster : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private TMP_Text fovText;
    [SerializeField] private Slider fovSlider;
    private float fovValue;

    void Start()
    {
        fovValue = playerCamera.fieldOfView;

        if (fovSlider != null)
        {
            fovSlider.value = fovValue;
            fovSlider.onValueChanged.AddListener(delegate { UpdatePlayerFOV(); });
        }
    }

    void Update()
    {

        if (playerCamera != null)
        { 
            UpdateFOVText();
        }

    }

    void UpdateFOVText ()
    {
        fovText.text = fovValue.ToString("F0");
    }

    void UpdatePlayerFOV()
    {
        if (playerCamera != null && fovSlider != null)
        {
            playerCamera.fieldOfView = fovSlider.value;
            fovValue = fovSlider.value;
        }
    }
}
