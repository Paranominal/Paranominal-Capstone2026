using System;
using UnityEngine;
using System.Collections;

public class PlayerStatus : MonoBehaviour, IDamageable
{
    [SerializeField] private FearBar fearBar;
    [SerializeField] private CameraEffects cameraEffects;
    [SerializeField] private PlayerMover playerMover;

    [Header("Stun")]
    [Tooltip("The amount of force applied to the stun knockback when the player is hit. -1 will use the damage amount.")]
    [SerializeField] private float knockbackForce = 2f;
    [SerializeField] private float screenShakeIntensity = 1f;

    [Header("Audio")]
    [SerializeField] private AudioManager hurtManager;
    [SerializeField] private SoundDataSO hurtVocalSound;
    [SerializeField] private SoundDataSO hurtImpactSound;
    [SerializeField] private AudioSource hurtSource;

    [SerializeField] private float invincibleDuration = 1f;

    // Michael feature (fear-effects): tracks whether the player is in an active encounter. Set by encounter managers externally.
    public bool IsInEncounter { get; set; }

    // Tracks whether the player is currently safe
    private bool isInvincible;

    // Michael feature (auto-resolve): fallback for cross-prefab references.
    private void Awake()
    {
        if (cameraEffects == null)
            cameraEffects = FindAnyObjectByType<CameraEffects>();
    }

    public void TakeDamage(DamageInfo info)
    {

        if (isInvincible)
            return;
        fearBar.TakeDamage(info.amount);
        CameraEffects.Instance?.Shake(info.amount * screenShakeIntensity);

        AudioManager.PlaySound(hurtImpactSound, hurtSource, true);
        AudioManager.PlaySound(hurtVocalSound, hurtSource, true);

        float stunDuration = cameraEffects != null ? cameraEffects.shakeDuration : 0f;
        StartInvinciblePeriod(stunDuration + invincibleDuration);
        playerMover.stunPlayer(stunDuration, info, knockbackForce);
    }

    private Coroutine invincibleCoroutine;
    private void StartInvinciblePeriod(float duration)
    {
        // If already invincible, do not restart the timer
        if (isInvincible)
            return;

        if (invincibleCoroutine != null)
            StopCoroutine(invincibleCoroutine);

        invincibleCoroutine = StartCoroutine(InvinciblePeriodRoutine(duration));
    }

    private IEnumerator InvinciblePeriodRoutine(float duration)
    {
        isInvincible = true;
        yield return new WaitForSeconds(duration);
        isInvincible = false;
        invincibleCoroutine = null;
    }
}