using UnityEngine;

public class FearBar : MonoBehaviour
{
    [Header("Fear")]
    [SerializeField] private int maxFear = 12;
    public int MaxFear => maxFear;

    [SerializeField] private int fearLevel = 0; // serialised so we can see it but do not modify!!!
    public int FearLevel => fearLevel;

    public enum FearRank { Fine, Low, Medium, High }

    public FearRank CurrentRank { get; private set; } = FearRank.Fine;

    public event System.Action<FearRank> OnFearChanged;

    public void Awake()
    {
        fearLevel = maxFear;
    }

    public void TakeDamage()
    {
        fearLevel = fearLevel - 1;
        fearLevel = Mathf.Min(fearLevel, maxFear);
        EvaluateRank();
    }

    private void EvaluateRank()
    {
        FearRank newRank;

        newRank = fearLevel switch // check this bad boy out
        {
            0 => FearRank.Fine,
            <= 4 => FearRank.Low,
            <= 8 => FearRank.Medium,
            _ => FearRank.High
        };

        if (newRank != CurrentRank)
        {
            CurrentRank = newRank;
        }

        OnFearChanged?.Invoke(CurrentRank); // event for fearbarUI & spiritbar to update, happens every time player takes damage
    }
}