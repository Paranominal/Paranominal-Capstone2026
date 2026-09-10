using UnityEngine;

// Listens to WeaponEvents and plays sounds for firing and reloading. Sits alongside the other weapon components on the weapon GameObject.
[RequireComponent(typeof(AudioSource))]
public class WeaponAudio : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WeaponEvents weaponEvents;
    [SerializeField] private AudioSource fireSource;
    [SerializeField] private AudioSource reloadSource;
    [SerializeField] private AudioSource misfireSource;

    [Header("Sounds")]
    [SerializeField] private SoundDataSO shotgunFire;
    [SerializeField] private SoundDataSO shotgunReload;
    [SerializeField] private SoundDataSO shotgunMisfire;

    private void Reset()
    {
        weaponEvents = GetComponent<WeaponEvents>();
        // Pre-fill fireSource with the first AudioSource on this GameObject.
        fireSource = GetComponent<AudioSource>();
        // reloadSource must be wired up manually in the inspector (designer adds a second AudioSource and drags it into the slot).
    }

    private void Awake()
    {
        if (weaponEvents == null) weaponEvents = GetComponent<WeaponEvents>();
        if (fireSource == null) fireSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        if (weaponEvents == null) return;
        weaponEvents.ShotFired += OnShotFired;
        weaponEvents.ReloadStarted += OnReloadStarted;
        weaponEvents.Misfired += OnMisfired;
    }

    private void OnDisable()
    {
        if (weaponEvents == null) return;
        weaponEvents.ShotFired -= OnShotFired;
        weaponEvents.ReloadStarted -= OnReloadStarted;
        weaponEvents.Misfired -= OnMisfired;
    }

    private void OnShotFired(WeakPointType shotType)
    {
        if (shotgunFire != null) AudioManager.PlaySound(shotgunFire, fireSource);
    }

    private void OnReloadStarted()
    {
        if (shotgunReload != null) AudioManager.PlaySound(shotgunReload, reloadSource);
    }

    private void OnMisfired()
    {
        if (shotgunMisfire != null) AudioManager.PlaySound(shotgunMisfire, misfireSource != null ? misfireSource : fireSource);
    }
}