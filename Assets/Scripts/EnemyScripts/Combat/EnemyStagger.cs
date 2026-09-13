// Summary:
// Handles enemy stagger mechanics: tracks hits, triggers stagger when threshold is reached (or 1-hit during windup),
// manages stagger duration with weakpoint extensions, and drives the stagger bar UI.

using UnityEngine;
using System.Collections;
using System;
using UnityEngine.UI;

public class EnemyStagger : MonoBehaviour, IDamageable
{
    [Header("Stagger Bar")]
    [SerializeField] private Slider staggerBar;
    [SerializeField] private Image staggerBarFill;
    [SerializeField] private Color barColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color barResetColor = new Color(1f, 3f, 5f, 1f);

    [Header("Weak Points")]
    public WeakPointManager weakPointManager;

    [Header("Stagger Settings")]
    [SerializeField] private int hitsToStagger = 2;
    public int HitsToStagger => hitsToStagger;
    [SerializeField] private bool stunOnWindup = true;
    [Range(0, 2)]
    [Tooltip("The rate the stagger bar drains. Higher = harder to stagger.")]
    [SerializeField] private float staggerResistance = 0.3f;
    [Range(0.5f, 5f)]
    [SerializeField] private float staggerTime = 2;
    [SerializeField] private float timeBeforeBarDrain = 0.4f;
    [SerializeField] private float timeAddedOnHit = 0.5f;

    [Header("Debug")]
    public bool debugMode;

    // events
    public event Action OnStaggerStart;
    public event Action OnStaggerEnd;

    // state
    [HideInInspector] public bool canBeHit = true;
    [HideInInspector] public bool windingUp;
    private bool isStaggered;
    public bool IsStaggered => isStaggered;
    private float currentStaggerTimeRemaining;
    private Coroutine currentStagger;
    private int cachedCurrentWeakpoint;

    private float damageTaken = 0;
    public float DamageTaken => damageTaken;
    private float currentRecoveryBuffer = 0;

    // hide stagger bar in Awake so it's never visible on spawn
    private void Awake()
    {
        if (staggerBar != null) staggerBar.gameObject.SetActive(false);
    }

    private void Update()
    {
        UpdateStaggerBar();
    }


    // Damage
    public void TakeDamage(DamageInfo info)
    {
        if (isStaggered) return;
        if (!canBeHit) return;

        damageTaken++;
        currentRecoveryBuffer = timeBeforeBarDrain;
        if (damageTaken >= AdjustedHitsToStagger()) TriggerStagger();
    }

    private int AdjustedHitsToStagger()
    {
        if (!stunOnWindup) return hitsToStagger;
        if (windingUp) return 1;
        return hitsToStagger;
    }


    // Stagger
    public void TriggerStagger()
    {
        if (currentStagger != null) StopCoroutine(currentStagger);
        currentStaggerTimeRemaining = staggerTime;
        currentStagger = StartCoroutine(DoStagger());
    }

    private IEnumerator DoStagger()
    {
        EnterStagger();
        while (currentStaggerTimeRemaining > 0)
        {
            if (weakPointManager != null && weakPointManager.CurrentWeakpoint > cachedCurrentWeakpoint)
                ExtendStagger();
            if (debugMode) Debug.Log($"[EnemyStagger] Stagger remaining for {gameObject.name}: {currentStaggerTimeRemaining:F2}");
            currentStaggerTimeRemaining -= Time.deltaTime;
            yield return null;
        }
        ExitStagger();
    }

    private void EnterStagger()
    {
        if (debugMode) Debug.Log($"[EnemyStagger] {gameObject.name} was staggered!", gameObject);
        isStaggered = true;
        cachedCurrentWeakpoint = 0;
        if (weakPointManager != null) weakPointManager.StartSequence();
        OnStaggerStart?.Invoke();
    }

    public void ExtendStagger()
    {
        if (!isStaggered) return;
        currentStaggerTimeRemaining += timeAddedOnHit;
        cachedCurrentWeakpoint = weakPointManager.CurrentWeakpoint;
        if (debugMode) Debug.Log($"[EnemyStagger] Stagger extended! Duration: {currentStaggerTimeRemaining:F2}s", gameObject);
    }

    private void ExitStagger()
    {
        if (debugMode) Debug.Log($"[EnemyStagger] {gameObject.name} recovered from stagger", gameObject);
        isStaggered = false;
        if (weakPointManager != null) weakPointManager.EndSequence();
        damageTaken = 0;
        OnStaggerEnd?.Invoke();
    }


    // Stagger Bar
    private void UpdateStaggerBar()
    {
        if (staggerBar == null) return;

        // drain the bar over time when not being hit
        if (currentRecoveryBuffer > 0) currentRecoveryBuffer -= Time.deltaTime;
        else if (damageTaken > 0) damageTaken -= Time.deltaTime * staggerResistance;
        else damageTaken = 0;

        // show/hide bar based on value
        if (staggerBar.value == 0) staggerBar.gameObject.SetActive(false);
        else staggerBar.gameObject.SetActive(true);

        // bar display: stagger duration when staggered, hit progress when not
        if (isStaggered)
        {
            staggerBar.value = currentStaggerTimeRemaining / staggerTime;
            if (staggerBarFill != null) staggerBarFill.color = barResetColor;
        }
        else
        {
            staggerBar.value = damageTaken / hitsToStagger;
            if (staggerBarFill != null) staggerBarFill.color = barColor;
        }
    }
}