using UnityEngine;

// Summary: 
// Listens to WeaponEvents and plays sounds for firing, reloading, and misfiring.
// Uses a single AudioSource and PlayOneShot via the AudioManager's new overload, so overlapping sounds (e.g. reload starting immediately after the final shot) don't interrupt each other.
[RequireComponent(typeof(AudioSource))]
public class WeaponAudio : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WeaponEvents weaponEvents;
    [SerializeField] private AudioSource source;

    [Header("Sounds")]
    [SerializeField] private SoundDataSO shotgunFire;
    [SerializeField] private SoundDataSO shotgunReload;
    [SerializeField] private SoundDataSO shotgunMisfire;

    private void Reset()
    {
        weaponEvents = GetComponent<WeaponEvents>();
        source = GetComponent<AudioSource>();
    }

    private void Awake()
    {
        if (weaponEvents == null) weaponEvents = GetComponent<WeaponEvents>();
        if (source == null) source = GetComponent<AudioSource>();
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
        if (shotgunFire != null) AudioManager.PlaySound(shotgunFire, source, true);
    }

    private void OnReloadStarted()
    {
        if (shotgunReload != null) AudioManager.PlaySound(shotgunReload, source, true);
    }

    private void OnMisfired()
    {
        if (shotgunMisfire != null) AudioManager.PlaySound(shotgunMisfire, source, true);
    }
}