// Summary:
// Handles enemy stagger mechanics: tracks hits, triggers stagger when threshold is reached (or 1-hit during windup),
// manages stagger duration with weakpoint extensions, and drives the stagger bar UI.
// Bar fills from center outward (Sekiro-style) via the MiddleOutFill shader's _FillAmount property.

using UnityEngine;
using System.Collections;
using System;
using UnityEngine.UI;
using UnityEngine.Sprites;

public class EnemyStagger : MonoBehaviour, IDamageable
{
    [Header("Stagger Bar")]
    [Tooltip("Root object to show/hide the stagger bar.")]
    [SerializeField] private GameObject staggerBarRoot;
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

    // per-instance material for the fill shader
    private Material fillMaterial;
    private static readonly int FillAmountID = Shader.PropertyToID("_FillAmount");
    private static readonly int FillUVRectID = Shader.PropertyToID("_FillUVRect");

    // hide stagger bar in Awake and create a material instance so each enemy is independent
    private void Awake()
    {
        // resolve bar references if not assigned
        if (staggerBarFill == null)
        {
            foreach (var img in GetComponentsInChildren<Image>(true))
            {
                if (img.name == "Fill") { staggerBarFill = img; break; }
            }
        }
        if (staggerBarRoot == null && staggerBarFill != null)
            staggerBarRoot = staggerBarFill.transform.parent.gameObject;

        if (staggerBarFill != null)
        {
            fillMaterial = new Material(staggerBarFill.material);
            staggerBarFill.material = fillMaterial;
            fillMaterial.SetFloat(FillAmountID, 0f);
            UpdateFillUVRect();
        }
        if (staggerBarRoot != null) staggerBarRoot.SetActive(false);
    }

    private void Update()
    {
        UpdateStaggerBar();
    }

    private void OnDestroy()
    {
        if (fillMaterial != null) Destroy(fillMaterial);
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


    // Stagger Bar (middle-out fill via shader _FillAmount)
    private void UpdateStaggerBar()
    {
        if (fillMaterial == null || staggerBarRoot == null) return;

        // canBeHit is disabled while spawning and as soon as death begins.
        // Keep the UI hidden regardless of any stagger value still draining.
        if (!canBeHit)
        {
            fillMaterial.SetFloat(FillAmountID, 0f);
            staggerBarRoot.SetActive(false);
            return;
        }

        UpdateFillUVRect();

        // drain the bar over time when not being hit
        if (currentRecoveryBuffer > 0) currentRecoveryBuffer -= Time.deltaTime;
        else if (damageTaken > 0) damageTaken -= Time.deltaTime * staggerResistance;
        else damageTaken = 0;

        // bar display: stagger duration when staggered, hit progress when not
        float fillAmount;
        if (isStaggered)
        {
            fillAmount = currentStaggerTimeRemaining / staggerTime;
            if (staggerBarFill != null) staggerBarFill.color = barResetColor;
        }
        else
        {
            fillAmount = damageTaken / hitsToStagger;
            if (staggerBarFill != null) staggerBarFill.color = barColor;
        }

        fillAmount = Mathf.Clamp01(fillAmount);
        fillMaterial.SetFloat(FillAmountID, fillAmount);

        // show/hide bar based on value
        staggerBarRoot.SetActive(fillAmount > 0f);
    }

    private void UpdateFillUVRect()
    {
        if (fillMaterial == null || staggerBarFill == null) return;

        Sprite sprite = staggerBarFill.sprite;
        Vector4 uv = sprite != null
            ? DataUtility.GetOuterUV(sprite)
            : new Vector4(0f, 0f, 1f, 1f);

        fillMaterial.SetVector(FillUVRectID, new Vector4(
            uv.x,
            uv.y,
            Mathf.Max(uv.z - uv.x, 0.0001f),
            Mathf.Max(uv.w - uv.y, 0.0001f)));
    }
}
