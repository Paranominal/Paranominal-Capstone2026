using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SensitivityAdjuster : MonoBehaviour
{
    [SerializeField] private PlayerLook playerLook;
    [SerializeField] private TMP_Text sensitivityText;
    [SerializeField] private Slider sensitivitySlider;
    private float sensitivityValue;


    void Start()
    {

        if (sensitivitySlider != null)
        {
            sensitivityValue = playerLook.GetLookSensitivity();
            sensitivitySlider.value = sensitivityValue;
        }

    }

    
    void Update()
    {
        if (playerLook != null)
        {
            sensitivityValue = sensitivitySlider.value;
            playerLook.SetLookSensitivity(sensitivityValue);
            UpdateSensitivityText();
        }

    }

    void UpdateSensitivityText()
    {
        sensitivityText.text = (sensitivityValue * 10).ToString("F1");
    }
}
