// Summary:
// Owns the Special Shot's charge and state. Builds a streak from weakpoint hits (via WeaponEvents.ShotResolved),
// becomes Ready once the streak hits the threshold, and stays banked until the player arms it.
// Once armed, the next barrel press fires the Special Shot (handled by ShotOrchestrator). Unlocked through AbilityManager.

using System;
using UnityEngine;

public class SpecialShot : MonoBehaviour
{
    public enum SpecialShotState { Locked, Charging, Ready, Armed }

    [Header("References")]
    [SerializeField] private WeaponEvents weaponEvents;

    [Header("Unlock")]
    [Tooltip("Start with the Special Shot unlocked. Normally unlocked by AbilityManager via a pickup.")]
    [SerializeField] private bool startUnlocked;

    [Header("Charge")]
    [Tooltip("Weakpoint hits in a row (without a misfire) needed to charge the Special Shot.")]
    [SerializeField, Min(1)] private int streakToCharge = 5;

    [Header("Debug")]
    [SerializeField] private bool debugMode;

    private SpecialShotState state = SpecialShotState.Locked;
    private int currentStreak;

    public SpecialShotState State => state;
    public int CurrentStreak => currentStreak;
    public int StreakToCharge => streakToCharge;
    public bool IsUnlocked => state != SpecialShotState.Locked;
    public bool IsReady => state == SpecialShotState.Ready;
    public bool IsArmed => state == SpecialShotState.Armed;

    // current streak, streak needed
    public event Action<int, int> OnStreakChanged;
    public event Action<SpecialShotState> OnStateChanged;

    private void Awake()
    {
        if (weaponEvents == null) weaponEvents = GetComponent<WeaponEvents>();
        if (weaponEvents == null) weaponEvents = FindAnyObjectByType<WeaponEvents>();

        state = startUnlocked ? SpecialShotState.Charging : SpecialShotState.Locked;
    }

    private void OnEnable()
    {
        if (weaponEvents != null) weaponEvents.ShotResolved += HandleShotResolved;
    }

    private void OnDisable()
    {
        if (weaponEvents != null) weaponEvents.ShotResolved -= HandleShotResolved;
    }

    // Called by AbilityManager when the pickup is collected
    public void Unlock()
    {
        if (IsUnlocked) return;

        if (debugMode) Debug.Log($"[{this}] Special Shot unlocked");
        SetStreak(0);
        SetState(SpecialShotState.Charging);
    }

    // Called by ShotOrchestrator on the arm input. Can't be cancelled once armed.
    public bool TryArm()
    {
        if (state != SpecialShotState.Ready) return false;

        if (debugMode) Debug.Log($"[{this}] Special Shot armed");
        SetState(SpecialShotState.Armed);
        return true;
    }

    // Called by ShotOrchestrator once the Special Shot has been fired
    public void Consume()
    {
        if (state != SpecialShotState.Armed) return;

        if (debugMode) Debug.Log($"[{this}] Special Shot fired, charge reset");
        SetStreak(0);
        SetState(SpecialShotState.Charging);
    }

    private void HandleShotResolved(ShotResult result)
    {
        // streak only builds while charging, a banked charge can't be lost
        if (state != SpecialShotState.Charging) return;

        switch (result.Outcome.Rules().Combo)
        {
            case ComboEffect.Increment:
                SetStreak(currentStreak + 1);
                if (currentStreak >= streakToCharge)
                {
                    if (debugMode) Debug.Log($"[{this}] Special Shot ready");
                    SetState(SpecialShotState.Ready);
                }
                break;

            case ComboEffect.Break:
                if (debugMode && currentStreak > 0) Debug.Log($"[{this}] Streak broken at {currentStreak}");
                SetStreak(0);
                break;

            case ComboEffect.Neutral:
                break;
        }
    }

    private void SetStreak(int value)
    {
        if (currentStreak == value) return;
        currentStreak = value;
        OnStreakChanged?.Invoke(currentStreak, streakToCharge);
    }

    private void SetState(SpecialShotState newState)
    {
        if (state == newState) return;
        state = newState;
        OnStateChanged?.Invoke(state);
    }
}
