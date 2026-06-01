using UnityEngine;

// Summary: Listens to WeaponEvents and plays sounds for firing and reloading.
// Sits alongside the other weapon components on the weapon GameObject.
[RequireComponent(typeof(AudioSource))]
public class WeaponAudio : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WeaponEvents weaponEvents;
    [SerializeField] private AudioSource source;

    [Header("Sounds")]
    [SerializeField] private SoundDataSO shotgunFire;
    [SerializeField] private SoundDataSO shotgunReload;

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
    }

    private void OnDisable()
    {
        if (weaponEvents == null) return;
        weaponEvents.ShotFired -= OnShotFired;
        weaponEvents.ReloadStarted -= OnReloadStarted;
    }

    private void OnShotFired(WeakPointType shotType)
    {
        if (shotgunFire != null) AudioManager.PlaySound(shotgunFire, source);
    }

    private void OnReloadStarted()
    {
        if (shotgunReload != null) AudioManager.PlaySound(shotgunReload, source);
    }
}