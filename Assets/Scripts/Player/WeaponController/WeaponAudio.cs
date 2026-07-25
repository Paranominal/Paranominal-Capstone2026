using UnityEngine;

// Listens to WeaponController events and plays sounds for firing and reloading.
// EDIT (weapon consolidation): was referencing WeaponEvents, now references WeaponController directly.
[RequireComponent(typeof(AudioSource))]
public class WeaponAudio : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WeaponController weaponController;
    [SerializeField] private AudioSource fireSource;
    [SerializeField] private AudioSource reloadSource;

    [Header("Sounds")]
    [SerializeField] private SoundDataSO shotgunFire;
    [SerializeField] private SoundDataSO shotgunReload;

    private void Reset()
    {
        weaponController = GetComponent<WeaponController>();
        fireSource = GetComponent<AudioSource>();
    }

    private void Awake()
    {
        if (weaponController == null) weaponController = GetComponent<WeaponController>();
        if (fireSource == null) fireSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        if (weaponController == null) return;
        weaponController.ShotFired += OnShotFired;
        weaponController.ReloadStarted += OnReloadStarted;
    }

    private void OnDisable()
    {
        if (weaponController == null) return;
        weaponController.ShotFired -= OnShotFired;
        weaponController.ReloadStarted -= OnReloadStarted;
    }

    private void OnShotFired(WeakPointType shotType)
    {
        if (shotgunFire != null) AudioManager.PlaySound(shotgunFire, fireSource);
    }

    private void OnReloadStarted()
    {
        if (shotgunReload != null) AudioManager.PlaySound(shotgunReload, reloadSource);
    }
}
