using UnityEngine;

public class FearBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WeaponEvents weaponEvents;

    [Header("Fear")]
    [SerializeField] private float maxFear = 100f;
    public float MaxFear => maxFear;

    [SerializeField] private float fearLevel = 0; // serialised so we can see it but do not modify directly
    public float FearLevel => fearLevel;

    [SerializeField] private float fearDamage = 10; // flat fallback for calls without a defined amount
    [SerializeField] private float fearHealPerShot = 5f; // fear reduced on each rewarded shot

    [Header("Tick Rates (fear per second)")]
    [Tooltip("How fast fear creeps up automatically at each rank. Fine = all chill, High = panic time!!")]
    [SerializeField] private float tickRateFine = 0f;
    [SerializeField] private float tickRateLow = 0.5f;
    [SerializeField] private float tickRateMedium = 1f;
    [SerializeField] private float tickRateHigh = 2f;

    public enum FearRank { Fine, Low, Medium, High }
    public FearRank CurrentRank { get; private set; } = FearRank.Fine;

    public event System.Action<FearRank> OnFearChanged;
    public event System.Action OnFearMaxed;

    private bool isMaxed = false;

    public void Awake()
    {
        fearLevel = 0;
        if (weaponEvents != null)
        {
            weaponEvents.ShotResolved += HandleShotResolved;
        }
    }

    private void Update()
    {
        if (isMaxed) return;

        float rate = CurrentRank switch
        {
            FearRank.Fine => tickRateFine,
            FearRank.Low => tickRateLow,
            FearRank.Medium => tickRateMedium,
            FearRank.High => tickRateHigh,
            _ => 0f
        };

        if (rate > 0f)
        {
            ModifyFear(rate * Time.deltaTime);
        }
    }

    public void TakeDamage() // flat fallback, used if an attack doesn't define its own value
    {
        ModifyFear(fearDamage);
    }

    public void TakeDamage(float amount) // overflow called by attacks with their own defined damage values
    {
        ModifyFear(amount);
    }

    public void HealFear(float amount) // convenience wrapper for external callers, like spirit had
    {
        ModifyFear(-amount);
    }

    public void ChangeFear(float amount) // generic call for modifying
    {
        ModifyFear(amount);
    }

    public void ModifyFear(float amount) // positive increases fear (worsens), negative decreases (improves)
    {
        if (isMaxed) return;

        fearLevel = Mathf.Clamp(fearLevel + amount, 0, maxFear);

        if (fearLevel >= maxFear)
        {
            isMaxed = true;
            EvaluateRank();
            OnFearMaxed?.Invoke();
        }

        EvaluateRank();
    }

    private void EvaluateRank()
    {
        float percent = (fearLevel / maxFear) * 100f;

        FearRank newRank = percent switch // check this bad boy out
        {
            <= 0 => FearRank.Fine, // no more overflow! yay!!!
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

    private void HandleShotResolved(WeakPointType shotType, bool rewarded)
    {
        if (rewarded)
        {
            ModifyFear(-fearHealPerShot);
        }
    }
}