using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WeaponController weaponController;

    [Header("Canvas")]
    [SerializeField] private Canvas hudCanvas;

    [Header("Ammo UI")]
    [SerializeField] private TMP_Text ammoText;

    [Header("Reload UI")]
    [SerializeField] private Slider reloadSlider;

    [Header("Crosshair UI")]
    [SerializeField] private Image crosshairImage;

    [Header("Interaction UI")]
    [SerializeField] private HudPromptPresenter interactionPrompt;

    // EDIT (auto-resolve): fallback for cross-prefab references.
    private void Awake()
    {
        if (weaponController == null)
            weaponController = FindAnyObjectByType<WeaponController>();
    }

    private void Start()
    {
        if (reloadSlider != null)
            reloadSlider.gameObject.SetActive(false);

        RefreshAmmoDisplay();

        if (weaponController != null && weaponController.IsReloading)
        {
            OnReloadStarted();
            OnReloadProgressChanged(weaponController.ReloadProgress);
        }
    }

    private void OnEnable()
    {
        if (weaponController != null)
        {
            weaponController.AmmoChanged += OnAmmoChanged;
            weaponController.ReloadStarted += OnReloadStarted;
            weaponController.ReloadProgressChanged += OnReloadProgressChanged;
            weaponController.ReloadFinished += OnReloadFinished;
        }

        // EDIT (weapon system): hide weapon HUD when in empty hand mode.
        WeaponManager.OnWeaponModeChanged += OnWeaponModeChanged;
    }

    private void OnDisable()
    {
        if (weaponController != null)
        {
            weaponController.AmmoChanged -= OnAmmoChanged;
            weaponController.ReloadStarted -= OnReloadStarted;
            weaponController.ReloadProgressChanged -= OnReloadProgressChanged;
            weaponController.ReloadFinished -= OnReloadFinished;
        }

        WeaponManager.OnWeaponModeChanged -= OnWeaponModeChanged;
    }

    // EDIT (weapon system): toggle ammo and reload UI based on weapon mode.
    // Crosshair stays visible in empty hand since the player is still aiming.
    private void OnWeaponModeChanged(bool weaponEquipped)
    {
        if (ammoText != null) ammoText.gameObject.SetActive(weaponEquipped);
        if (reloadSlider != null && !weaponEquipped) reloadSlider.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (weaponController == null) return;

        RefreshAmmoDisplay();

        if (reloadSlider != null)
        {
            bool showReload = weaponController.IsReloading;
            reloadSlider.gameObject.SetActive(showReload);

            if (showReload)
                reloadSlider.value = weaponController.ReloadProgress;
        }
    }

    private void RefreshAmmoDisplay()
    {
        if (ammoText != null && weaponController != null)
            ammoText.text = $"{weaponController.CurrentAmmo}/{weaponController.MagazineSize}";
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

    // Summary: Toggles individual gameplay HUD elements. Used by the Grimoire to hide
    // the crosshair/ammo/prompt while the full Grimoire UI is open.
    public void UIVisible(bool state)
    {
        if (crosshairImage != null) crosshairImage.gameObject.SetActive(state);
        if (ammoText != null) ammoText.gameObject.SetActive(state);

        if (reloadSlider != null)
        {
            bool showReload = state && weaponController != null && weaponController.IsReloading;
            reloadSlider.gameObject.SetActive(showReload);
        }

        if (interactionPrompt != null) interactionPrompt.SetVisible(state);
    }

    // Summary: Toggles the entire HUD canvas on or off. Used by the pause menu.
    public void SetHUDActive(bool active)
    {
        if (hudCanvas != null)
            hudCanvas.gameObject.SetActive(active);
    }
}