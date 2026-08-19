using UnityEngine;

public class ComboSystem : MonoBehaviour
{
    [Header("Combo")]
    [SerializeField] private float comboDuration = 5f;
    [SerializeField] private float multiplierPerHit = 0.1f;

    [Header("Debug")]
    [SerializeField] private bool debugMode = true; // haven't assembled the ui yet

    public float Multiplier { get; private set; }
    public float TimeRemaining { get; private set; }
    public float Duration => comboDuration;
    public bool IsActive => TimeRemaining > 0f;

    public event System.Action<float> OnComboChanged;
    public event System.Action OnComboEnded;

    private void Update()
    {
        if (!IsActive) return;

        TimeRemaining -= Time.deltaTime;

        if (TimeRemaining <= 0f)
        {
            if (debugMode) Debug.Log($"Combo timed out at {Multiplier:0.0}x");
            BreakCombo();
        }
    }

    public void RegisterWeakPointHit()
    {
        Multiplier += multiplierPerHit;
        TimeRemaining = comboDuration;

        if (debugMode) Debug.Log($"Combo up! Multiplier now {Multiplier:0.0}x, timer reset to {comboDuration}s");
        OnComboChanged?.Invoke(Multiplier);
    }

    public void BreakCombo()
    {
        if (!IsActive && Multiplier <= 0f) return; // already broken, nothing to announce

        if (debugMode) Debug.Log($"Combo broken at {Multiplier:0.0}x");

        Multiplier = 0f;
        TimeRemaining = 0f;

        OnComboChanged?.Invoke(Multiplier);
        OnComboEnded?.Invoke();
    }
}