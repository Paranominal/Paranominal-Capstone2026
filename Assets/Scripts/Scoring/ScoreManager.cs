using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WeaponEvents weaponEvents;
    [SerializeField] private ComboSystem comboSystem;

    [Header("Scoring")]
    [SerializeField] private int pointsPerWeakpointHit = 10;

    [Header("Ranks")]
    [Tooltip("In ascending order of pointThreshold. Player holds the highest rank whose threshold they've met.")]
    [SerializeField]
    private RankDefinition[] ranks = new RankDefinition[]
    {
        new RankDefinition { label = "D", pointThreshold = 50  },
        new RankDefinition { label = "C", pointThreshold = 100 },
        new RankDefinition { label = "B", pointThreshold = 200 },
        new RankDefinition { label = "A", pointThreshold = 400 },
        new RankDefinition { label = "S", pointThreshold = 800 },
    };
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    public string CurrentRank { get; private set; } = string.Empty;
    public event System.Action<string> OnRankChanged;

    public int currentScore = 0;

    public event System.Action<int> OnPointsAdded;

    private void Awake()
    {
        if (weaponEvents == null)
        {
            weaponEvents = GetComponent<WeaponEvents>(); // getting the weapon events from the current object (assuming Player) if not manually assigned
        }
        if (comboSystem == null)
        {
            comboSystem = GetComponent<ComboSystem>();
        }

        weaponEvents.ShotResolved += HandleShotResolved;

        EvaluateRank();
    }

    public void AddScore(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        currentScore += amount;
        if (debugMode) Debug.Log($"Current score: {currentScore}"); // printing this in console for now, will be displayed on end screen
        OnPointsAdded?.Invoke(currentScore);

        EvaluateRank();
    }

    public float GetProgressToNextRank() // for spiritbarui
    {
        if (ranks == null || ranks.Length == 0) return 0f;

        int currentRankIndex = -1;
        for (int i = 0; i < ranks.Length; i++)
        {
            if (currentScore >= ranks[i].pointThreshold)
                currentRankIndex = i;
        }

        if (currentRankIndex == ranks.Length - 1) return 1f; // at max rank, bar is full

        int lower = currentRankIndex >= 0 ? ranks[currentRankIndex].pointThreshold : 0;
        int upper = ranks[currentRankIndex + 1].pointThreshold;

        return Mathf.Clamp01((float)(currentScore - lower) / (upper - lower));
    }

    private void EvaluateRank()
    {
        string newRank = string.Empty;
        foreach (RankDefinition rank in ranks)
        {
            if (currentScore >= rank.pointThreshold)
                newRank = rank.label;
        }
        if (newRank != CurrentRank)
        {
            CurrentRank = newRank;
            OnRankChanged?.Invoke(CurrentRank);
            if (debugMode) Debug.Log($"Rank up! New rank: {CurrentRank}");
        }
    }

    // kinda combos stuff below this point but pulling it into combosystem felt worse

    private void AwardWeakPointHit()
    {
        float multiplier = comboSystem != null ? comboSystem.Multiplier : 0f;
        int points = Mathf.RoundToInt(pointsPerWeakpointHit * (1f + multiplier));

        if (debugMode) Debug.Log($"Weakpoint hit: {pointsPerWeakpointHit} x {1f + multiplier:0.0} = {points} points");
        AddScore(points);
    }

    private void HandleShotResolved(WeakPointType shotType, ShotOutcome outcome)
    {
        switch (outcome)
        {
            case ShotOutcome.WeakPointHit:
                AwardWeakPointHit();
                comboSystem.RegisterWeakPointHit();
                break;

            case ShotOutcome.Miss:
            case ShotOutcome.WrongAmmo:
                comboSystem.BreakCombo();
                break;

            case ShotOutcome.EnemyHit:
                break; // neutral: no increment, no break, timer keeps running
        }
    }
}
