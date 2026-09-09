using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SensitivityAdjuster : MonoBehaviour
{
    [SerializeField] private PlayerLook playerLook;
    [SerializeField] private WeaponSway weaponSway;
    [SerializeField] private WeaponSway bookSway;
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
            if (weaponSway != null) weaponSway.SetSwayMultiplier(sensitivityValue * 2);
            if (bookSway != null) bookSway.SetSwayMultiplier(sensitivityValue * 2);
            UpdateSensitivityText();
        }

    }

    void UpdateSensitivityText()
    {
        sensitivityText.text = (sensitivityValue * 10).ToString("F1");
    }
}
