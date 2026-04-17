using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    // HUD reads state from these scripts each frame and mirrors it to UI elements
    [Header("References")]
    [SerializeField] private GunController gunController;
    [SerializeField] private Dashing dashing;

    // text readout for current ammo in magazine and magazine capacity
    [Header("Ammo UI")]
    [SerializeField] private TMP_Text ammoText;

    // reload progress bar, shown only while actively reloading
    [Header("Reload UI")]
    [SerializeField] private Slider reloadSlider;

    // dash cooldown bar, shown only while dash is unavailable
    [Header("Dash UI")]
    [SerializeField] private Slider dashSlider;

    // crosshair for aiming
    [Header("Crosshair UI")]
    [SerializeField] private Image crosshairImage;

    private void Start()
    {
        // hide conditional UI at startup so HUD begins mostly blank
        // these sliders are enabled only when their corresponding action is in progress/cooldown
        if (reloadSlider != null)
            reloadSlider.gameObject.SetActive(false);

        if (dashSlider != null)
            dashSlider.gameObject.SetActive(false);
    }

    private void Update()
    {
        // gun-related HUD updates like ammo text and reload progress.
        // null checks keep HUD safe even if references are temporarily missing
        if (gunController != null)
        {
            if (ammoText != null)
                ammoText.text = $"{gunController.CurrentAmmo}/{gunController.MagazineSize}";

            if (reloadSlider != null)
            {
                // show slider only when reloading so the player sees progress contextually
                bool showReload = gunController.IsReloading;
                reloadSlider.gameObject.SetActive(showReload);

                if (showReload)
                    reloadSlider.value = gunController.ReloadProgress;
            }
        }

        // dash cooldown HUD updates
        if (dashing != null && dashSlider != null)
        {
            bool showDashCooldown = dashing.IsDashOnCooldown;
            dashSlider.gameObject.SetActive(showDashCooldown);

            if (showDashCooldown)
                dashSlider.value = dashing.DashCooldownProgress;
        }
    }

    public void UIVisible(bool state)
    {
        reloadSlider.gameObject.SetActive(state);
        crosshairImage.gameObject.SetActive(state);
        ammoText.gameObject.SetActive(state);
    }
}