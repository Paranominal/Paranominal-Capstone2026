using UnityEngine;

public class ComboSystem : MonoBehaviour
{
    [Header("Combo")]
    [SerializeField] private float comboDuration = 5f;
    [SerializeField] private float multiplierPerHit = 0.1f;

    [Header("Penalties")]
    [SerializeField] private float missPenalty = 0.1f;       // Michael edit: multiplier reduction on a missed shot
    [SerializeField] private float drainPenalty = 0.1f;      // Michael edit: multiplier reduction when the combo bar drains
    [SerializeField] private float damagePenalty = 0.1f;      // Michael edit: multiplier reduction when the player takes damage

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    public float Multiplier { get; private set; }
    public float TimeRemaining { get; private set; }
    public float Duration => comboDuration;
    public bool IsActive => TimeRemaining > 0f;

    public event System.Action<float> OnComboChanged;
    public event System.Action OnComboEnded;

    private void Update()
    {
        if (!IsActive) return;

        TimeRemaining -= Time.deltaTime * Multiplier;

        if (TimeRemaining <= 0f)
        {
            PenalizeCombo(drainPenalty); // Michael edit: drop one tier instead of full reset
        }
    }

    public void RegisterHit()
    {
        Multiplier += multiplierPerHit;
        TimeRemaining = comboDuration;

        if (debugMode) Debug.Log($"Combo up! Multiplier now {1f + Multiplier:0.0}x, timer reset to {comboDuration}s");
        OnComboChanged?.Invoke(Multiplier);
    }

    // Michael edit: called by ScoreManager when a shot misses
    public void ApplyMissPenalty()
    {
        PenalizeCombo(missPenalty);
    }

    // Michael edit: called externally when the player takes damage
    public void ApplyDamagePenalty()
    {
        PenalizeCombo(damagePenalty);
    }

    // Michael edit: reduces multiplier by the given amount, resets timer if combo survives, ends it if it hits zero
    private void PenalizeCombo(float penalty)
    {
        if (!IsActive && Multiplier <= 0f) return;

        Multiplier = Mathf.Max(0f, Multiplier - penalty);

        if (Multiplier <= 0f)
        {
            if (debugMode) Debug.Log("Combo ended (multiplier hit zero)");
            Multiplier = 0f;
            TimeRemaining = 0f;
            OnComboChanged?.Invoke(Multiplier);
            OnComboEnded?.Invoke();
        }
        else
        {
            TimeRemaining = comboDuration; // reset timer so the player gets a full window at the reduced tier
            if (debugMode) Debug.Log($"Combo penalized: multiplier now {1f + Multiplier:0.0}x, timer reset");
            OnComboChanged?.Invoke(Multiplier);
        }
    }

    // Michael edit: hard reset, kept for edge cases
    public void BreakCombo()
    {
        if (!IsActive && Multiplier <= 0f) return;

        if (debugMode) Debug.Log($"Combo broken at {1f + Multiplier:0.0}x");

        Multiplier = 0f;
        TimeRemaining = 0f;

        OnComboChanged?.Invoke(Multiplier);
        OnComboEnded?.Invoke();
    }
}
