using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    // EDIT (weapon consolidation): three weapon references collapsed into one WeaponController.
    [Header("References")]
    [SerializeField] private WeaponController weaponController;

    // text readout for current ammo in magazine and magazine capacity
    [Header("Ammo UI")]
    [SerializeField] private TMP_Text ammoText;

    // reload progress bar, shown only while actively reloading
    [Header("Reload UI")]
    [SerializeField] private Slider reloadSlider;

    // crosshair for aiming
    [Header("Crosshair UI")]
    [SerializeField] private Image crosshairImage;

    // EDIT (interaction prompt system): HUD interaction prompt, toggled alongside the rest of the HUD.
    [Header("Interaction UI")]
    [SerializeField] private HudPromptPresenter interactionPrompt;

    private void Start()
    {
        // hide conditional UI at startup so HUD begins mostly blank
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
    }

    private void Update()
    {
        // fallback polling if events aren't connected
        if (weaponController == null) return;

        if (!HasEventSubscriptions())
        {
            RefreshAmmoDisplay();

            if (reloadSlider != null)
            {
                bool showReload = weaponController.IsReloading;
                reloadSlider.gameObject.SetActive(showReload);

                if (showReload)
                    reloadSlider.value = weaponController.ReloadProgress;
            }
        }
    }

    private bool HasEventSubscriptions()
    {
        // If OnEnable ran successfully, events are wired. This replaces the old
        // "weaponEvents == null" check that gated the polling fallback.
        return weaponController != null;
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

    public void UIVisible(bool state)
    {
        if (crosshairImage != null) crosshairImage.gameObject.SetActive(state);
        if (ammoText != null) ammoText.gameObject.SetActive(state);

        if (reloadSlider != null)
        {
            bool showReload = state && weaponController != null && weaponController.IsReloading;
            reloadSlider.gameObject.SetActive(showReload);
        }

        // EDIT (interaction prompt system): mute/unmute the interaction prompt with the rest of the HUD.
        if (interactionPrompt != null) interactionPrompt.SetVisible(state);
    }
}
