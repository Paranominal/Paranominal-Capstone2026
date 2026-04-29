using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    // HUD reads state from these scripts each frame and mirrors it to UI elements
    [Header("References")]
    [SerializeField] private WeaponFiringLogic weaponFiringLogic;
    [SerializeField] private WeaponEvents weaponEvents;
    [SerializeField] private WeaponStateController weaponStateController;
    //[SerializeField] private Dashing dashing;

    // text readout for current ammo in magazine and magazine capacity
    [Header("Ammo UI")]
    [SerializeField] private TMP_Text ammoText;

    // reload progress bar, shown only while actively reloading
    [Header("Reload UI")]
    [SerializeField] private Slider reloadSlider;

    // dash cooldown bar, shown only while dash is unavailable
    //[Header("Dash UI")]
    //[SerializeField] private Slider dashSlider;

    // crosshair for aiming
    [Header("Crosshair UI")]
    [SerializeField] private Image crosshairImage;

    private void Start()
    {
        if (weaponFiringLogic == null)
            weaponFiringLogic = GetComponent<WeaponFiringLogic>();

        if (weaponEvents == null)
            weaponEvents = GetComponent<WeaponEvents>();

        // hide conditional UI at startup so HUD begins mostly blank
        // these sliders are enabled only when their corresponding action is in progress/cooldown
        if (reloadSlider != null)
            reloadSlider.gameObject.SetActive(false);

        //if (dashSlider != null)
        //    dashSlider.gameObject.SetActive(false);

        RefreshAmmoDisplay();

        if (weaponFiringLogic != null && weaponFiringLogic.IsReloading)
        {
            OnReloadStarted();
            OnReloadProgressChanged(weaponFiringLogic.ReloadProgress);
        }
    }

    private void OnEnable()
    {
        if (weaponEvents == null)
            weaponEvents = GetComponent<WeaponEvents>();

        if (weaponEvents != null)
        {
            weaponEvents.AmmoChanged += OnAmmoChanged;
            weaponEvents.ReloadStarted += OnReloadStarted;
            weaponEvents.ReloadProgressChanged += OnReloadProgressChanged;
            weaponEvents.ReloadFinished += OnReloadFinished;
        }
    }

    private void OnDisable()
    {
        if (weaponEvents != null)
        {
            weaponEvents.AmmoChanged -= OnAmmoChanged;
            weaponEvents.ReloadStarted -= OnReloadStarted;
            weaponEvents.ReloadProgressChanged -= OnReloadProgressChanged;
            weaponEvents.ReloadFinished -= OnReloadFinished;
        }
    }

    private void Update()
    {
        // fallback if event source is missing
        if (weaponEvents == null)
        {
            RefreshAmmoDisplay();

            if (reloadSlider != null && weaponFiringLogic != null)
            {
                bool showReload = weaponFiringLogic.IsReloading;
                reloadSlider.gameObject.SetActive(showReload);

                if (showReload)
                    reloadSlider.value = weaponFiringLogic.ReloadProgress;
            }
        }

        // dash cooldown HUD updates
        /*if (dashing != null && dashSlider != null)
        {
            bool showDashCooldown = dashing.IsDashOnCooldown;
            dashSlider.gameObject.SetActive(showDashCooldown);

            if (showDashCooldown)
                dashSlider.value = dashing.DashCooldownProgress;
        }*/
    }

    private void RefreshAmmoDisplay()
    {
        if (ammoText != null && weaponFiringLogic != null)
            ammoText.text = $"{weaponFiringLogic.CurrentAmmo}/{weaponFiringLogic.MagazineSize}";
    }

    private void OnAmmoChanged(int currentAmmo, int magazineSize)
    {
        if (ammoText != null)
            ammoText.text = $"{currentAmmo}/{magazineSize}";
    }

    private void OnReloadStarted()
    {
        if (reloadSlider != null)
        {
            reloadSlider.gameObject.SetActive(true);
            reloadSlider.value = 0f;
        }
    }

    private void OnReloadProgressChanged(float progress)
    {
        if (reloadSlider != null)
        {
            reloadSlider.gameObject.SetActive(true);
            reloadSlider.value = progress;
        }
    }

    private void OnReloadFinished()
    {
        if (reloadSlider != null)
        {
            reloadSlider.value = 0f;
            reloadSlider.gameObject.SetActive(false);
        }
    }

    public void UIVisible(bool state)
    {
        if (crosshairImage != null) crosshairImage.gameObject.SetActive(state);
        if (ammoText != null) ammoText.gameObject.SetActive(state);

        if (reloadSlider != null)
        {
            bool showReload = state && weaponFiringLogic != null && weaponFiringLogic.IsReloading;
            reloadSlider.gameObject.SetActive(showReload);
        }


    }
}