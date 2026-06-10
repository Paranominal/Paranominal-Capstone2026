using UnityEngine;

public class SpiritBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private FearBar fearBar;

    [Header("Spirit")]
    [SerializeField] private float visibleSpirit = 100f; // if changing, also change public int currentScore in ScoreManager

    public float VisibleSpirit => visibleSpirit;

    public event System.Action OnSpiritDepleted;

    private float drainRate = 0f;
    private int lastKnownScore;
    private bool isDepleted = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        lastKnownScore = scoreManager.currentScore;
        scoreManager.OnPointsAdded += HandlePointsAdded;
        fearBar.OnFearChanged += HandleFearChanged;
    }

    private void Update()
    {
        if (isDepleted)
        {
            return; // stops game over from triggering over and over
        }

        visibleSpirit -= drainRate * Time.deltaTime;

        if (visibleSpirit <= 0f)
        {
            visibleSpirit = 0f;
            isDepleted = true;
            OnSpiritDepleted?.Invoke(); // event for the game over manager
        }
    }

    public void ModifySpirit(float amount) // positive values heal, negative values damage
    { // its the same as update but can be called from anywhere
        if (isDepleted)
        {
            return; // stops game over from triggering over and over
        }

        visibleSpirit += amount;

        if (visibleSpirit <= 0f)
        {
            visibleSpirit = 0f;
            isDepleted = true;
            OnSpiritDepleted?.Invoke(); // event for the game over manager
        }
    }

    private void HandlePointsAdded(int newTotal)
    {
        int gained = newTotal - lastKnownScore;
        lastKnownScore = newTotal;
        visibleSpirit += gained;
    }

    private void HandleFearChanged(FearBar.FearRank rank)
    {
        drainRate = rank switch // another lit ass switch statement
        {
            FearBar.FearRank.Fine => 0f,
            FearBar.FearRank.Low => 1f,
            FearBar.FearRank.Medium => 0.5f,
            FearBar.FearRank.High => 0.33f,
            _ => 0f
        };
    }
}