using UnityEngine;

public class FearBar : MonoBehaviour
{
    [Header("Fear")]
    [SerializeField] private int maxFear = 100;
    public int MaxFear => maxFear;

    [SerializeField] private int fearLevel = 0; // serialised so we can see it but do not modify directly
    public int FearLevel => fearLevel;

    [SerializeField] private int fearDamage = 10;
    public enum FearRank { Fine, Low, Medium, High }

    public FearRank CurrentRank { get; private set; } = FearRank.Fine;

    public event System.Action<FearRank> OnFearChanged;

    public void Awake()
    {
        fearLevel = 0;
    }

    public void TakeDamage()
    {
        ModifyFear(fearDamage);
    }

    public void ChangeFear(int amount)
    {
        ModifyFear(amount);
    }

    public void ModifyFear(int amount) // positive values increase fear (worsen), negative values decrease fear (improve)
    {
        fearLevel = Mathf.Clamp(fearLevel + amount, 0, maxFear);
        Debug.Log("yo");
        EvaluateRank();
    }

    private void EvaluateRank()
    {
        FearRank newRank;

        newRank = fearLevel switch // check this bad boy out
        {
            // removed defining fine bc it was causing overflow issues, will fix soon
            <= 33 => FearRank.Low,
            <= 66 => FearRank.Medium,
            _ => FearRank.High
        };

        if (newRank != CurrentRank)
        {
            CurrentRank = newRank;
        }

        OnFearChanged?.Invoke(CurrentRank); // event for fearbarUI & spiritbar to update, happens every time player takes damage
    }
}