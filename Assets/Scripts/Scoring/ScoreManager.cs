using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WeaponEvents weaponEvents;
    [SerializeField] private ComboSystem comboSystem;

    [Header("Scoring")]
    [Tooltip("Accuracy scoring lerps linearly between these two values.")]
    [SerializeField] private int minWeakpointPoints = 1;
    [SerializeField] private int maxWeakpointPoints = 10;

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

    // points to display = final awarded (after combo), precision = 1-10 base, position = where it landed, ownerCentre = where the enemy is
    public event System.Action<int, int, Vector3, Vector3> OnPointsAwarded;

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

    private int PointsForAccuracy(float accuracy)
    {
        return Mathf.Clamp(
            Mathf.RoundToInt(Mathf.Lerp(minWeakpointPoints, maxWeakpointPoints, accuracy)),
            minWeakpointPoints, maxWeakpointPoints);
    }

    private void AwardHit(ShotResult result)
    {
        int basePoints = PointsForAccuracy(result.Accuracy);
        float multiplier = comboSystem != null ? comboSystem.Multiplier : 0f;
        int points = Mathf.RoundToInt(basePoints * (1f + multiplier));

        if (debugMode) Debug.Log($"Weakpoint hit: {result.Accuracy:0.00} accuracy = {basePoints} base x {1f + multiplier:0.0} = {points} points");
        AddScore(points);
        OnPointsAwarded?.Invoke(points, basePoints, result.HitPoint, result.OwnerCentre);
    }

    private void HandleShotResolved(ShotResult result)
    {
        OutcomeRules rules = result.Outcome.Rules();

        if (rules.AwardsPoints)
            AwardHit(result);

        switch (rules.Combo)
        {
            case ComboEffect.Increment:
                comboSystem.RegisterHit();
                break;

            case ComboEffect.Break:
                comboSystem.BreakCombo();
                break;

            case ComboEffect.Neutral:
                break;
        }
    }
}
