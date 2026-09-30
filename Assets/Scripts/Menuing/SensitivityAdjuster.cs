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
        // resolve cross-prefab references
        if (playerLook == null)
            playerLook = FindFirstObjectByType<PlayerLook>();

        if (playerLook != null && (weaponSway == null || bookSway == null))
        {
            // both WeaponSway components live on the player: weapon under RHand, book under LHand
            // GetComponentsInChildren traverses depth-first so RHand (weapon) comes before LHand (book)
            var allSway = playerLook.transform.root.GetComponentsInChildren<WeaponSway>(true);
            if (allSway.Length >= 2)
            {
                if (weaponSway == null) weaponSway = allSway[0];
                if (bookSway == null) bookSway = allSway[1];
            }
            else if (allSway.Length == 1 && weaponSway == null)
            {
                weaponSway = allSway[0];
            }
        }

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