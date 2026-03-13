using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GunController gunController;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Ammo UI")]
    [SerializeField] private TMP_Text ammoText;

    [Header("Reload UI")]
    [SerializeField] private Slider reloadSlider;

    [Header("Dash UI")]
    [SerializeField] private Slider dashSlider;

    private void Start()
    {
        if (reloadSlider != null)
            reloadSlider.gameObject.SetActive(false);

        if (dashSlider != null)
            dashSlider.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (gunController != null)
        {
            if (ammoText != null)
                ammoText.text = $"Ammo: {gunController.CurrentAmmo}/{gunController.MagazineSize}";

            if (reloadSlider != null)
            {
                bool showReload = gunController.IsReloading;
                reloadSlider.gameObject.SetActive(showReload);

                if (showReload)
                    reloadSlider.value = gunController.ReloadProgress;
            }
        }

        if (playerMovement != null && dashSlider != null)
        {
            bool showDashCooldown = playerMovement.IsDashOnCooldown;
            dashSlider.gameObject.SetActive(showDashCooldown);

            if (showDashCooldown)
                dashSlider.value = playerMovement.DashCooldownProgress;
        }
    }
}