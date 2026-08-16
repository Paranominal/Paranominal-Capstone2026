using UnityEngine;
using System.Collections;
using System;
using UnityEngine.UI;

public class EnemyStagger : MonoBehaviour, IDamageable
{
    [SerializeField] private Slider staggerBar;
    [SerializeField] private Image staggerBarFill;
    [SerializeField] private Color barColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color barResetColor = new Color(1f, 3f, 5f, 1f);
    [SerializeField] private WeakPointManager weakPointManager;
    [HideInInspector] public bool canBeHit = true;
    private bool isStaggered = false;
    [SerializeField] private int hitsToStagger = 2;
    public int HitsToStagger => hitsToStagger;
    [SerializeField] private bool stunOnWindup = true;
    [Range(0,2)]
    [Tooltip("The degree to which the enemy resists being Staggered. (i.e: The rate the Stagger Bar goes down).")]
    [SerializeField] private float staggerResistance = 0.3f;
    [Range(0.5f,5f)]
    [SerializeField] private float staggerTime = 2;
    [SerializeField] private float timeAddedOnHit = 0.5f;
    private float currentStaggerTimeRemaining;
    // public event Action OnStaggerEnd;
    // [SerializeField] private bool doStaggerColor = true;
    // [SerializeField] private Color staggerColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    public bool debugMode;
    public bool IsStaggered => isStaggered; //returns whether the enemy is staggered
    private Coroutine currentStagger;
    void Start()
    {
        // if (doStaggerColor) InitializeColor();
    }
    void Update()
    {
        // if (WeakpointWasHit()) weakPointManager.NextInSequence();
        StaggerBar();
    }
    bool WeakpointWasHit()
    {
        if (weakPointManager == null) return false;
        if (weakPointManager.weakpoints[weakPointManager.currentWeakpoint].hasBeenHit) return true;
        else return false;
    }
    public void TriggerStagger()
    {
        //stops any previous stagger coroutine from running and starts a new one
        if (currentStagger != null) StopCoroutine(currentStagger);
        currentStaggerTimeRemaining = staggerTime;
        currentStagger = StartCoroutine(DoStagger());
    }
    int cachedCurrentWeakpoint;
    private IEnumerator DoStagger() //handles duration and recovery
    {      
        EnterStagger();  
        while (currentStaggerTimeRemaining > 0)  //wait for the duration to end
        {
            if (weakPointManager.currentWeakpoint > cachedCurrentWeakpoint) ExtendStagger();
            if (debugMode) Debug.Log($"[{this}] Stagger Time Remaining for {gameObject}: {currentStaggerTimeRemaining}");
            currentStaggerTimeRemaining -= Time.deltaTime;
            yield return null;
        }
        ExitStagger();
    }
    private void EnterStagger()
    {
        if (debugMode) Debug.Log($"[EnemyStagger] {gameObject.name} was staggered!", gameObject);
        isStaggered = true;
        if (weakPointManager != null) weakPointManager.StartSequence();
        // if (doStaggerColor) DoColor(staggerColor);
    }
    public void ExtendStagger() //extends stagger on Weakpoint hit while staggered.
    {
        if (!isStaggered) return;
        currentStaggerTimeRemaining += timeAddedOnHit;
        cachedCurrentWeakpoint = weakPointManager.currentWeakpoint;
        if (debugMode) Debug.Log($"[EnemyStagger] Stagger extended! Duration: {currentStaggerTimeRemaining:F2}s", gameObject);
    }
    private void ExitStagger()
    {
        if (debugMode) Debug.Log($"[EnemyStagger] {gameObject.name} recovered from stagger", gameObject);
        isStaggered = false;
        if (weakPointManager != null) weakPointManager.EndSequence();
        // if (doStaggerColor) UndoColor();
        damageTaken = 0;
    }
    private SpriteRenderer[] spriteRenderers;
    private Color[] cachedColors;
    private bool isInitialized;

    // private void InitializeColor()
    // {
    //     if (isInitialized) return;

    //     spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
    //     Debug.Log($"[{this}] Sprite Renderers {spriteRenderers}");
    //     if (spriteRenderers != null && spriteRenderers.Length > 0)
    //     {
    //         cachedColors = new Color[spriteRenderers.Length];

    //         for (int i = 0; i < spriteRenderers.Length; i++)
    //         {
    //             cachedColors[i] = spriteRenderers[i].color;
    //         }

    //         isInitialized = true;
    //     }
    // }

    // //apply color effect to sprites
    // public void DoColor(Color color)
    // {
    //     InitializeColor();
    //     // if (!isInitialized) return;
    //     foreach (SpriteRenderer renderer in spriteRenderers)
    //     {
    //         if (renderer.gameObject.tag != "WeakPoint") renderer.color = color;
    //     }
    // }
    // //restore  sprites to  original colors
    // public void UndoColor()
    // {
    //     InitializeColor();
    //     // if (!isInitialized) return;
    //     foreach (SpriteRenderer renderer in spriteRenderers)
    //     {
    //         if (renderer.gameObject.tag != "WeakPoint") renderer.color = cachedColors[Array.IndexOf(spriteRenderers, renderer)];
    //     }
    // }
    float damageTaken = 0;
    public void TakeDamage(DamageInfo info)
    {
        //nak code from ToTough.cs
        //ignore regular damage, only weakpoints can be shot
        if (isStaggered) return;
        if (!canBeHit) return;

        // if (knockback != null) knockback.ApplyKnockback();
        damageTaken++;
        if (damageTaken >= AdjustedHitsToStagger()) TriggerStagger();
    }
    [HideInInspector] public bool windingUp;
    int AdjustedHitsToStagger()
    {
        if (!stunOnWindup) return hitsToStagger;
        else if (windingUp) return 1;
        else return hitsToStagger;
    }

    private void StaggerBar()
    {
        // if (isStaggered) staggerBar.enabled = false;
        // else 
        if (damageTaken > 0) damageTaken -= Time.deltaTime * staggerResistance;
        else damageTaken = 0;
        if (staggerBar.value == 0) staggerBar.gameObject.SetActive(false);
        else staggerBar.gameObject.SetActive(true);
        if (isStaggered)
        {
            staggerBar.value = currentStaggerTimeRemaining / staggerTime;
            staggerBarFill.color = barResetColor;
        }
        else
        {
            staggerBar.value = damageTaken / hitsToStagger;
            staggerBarFill.color = barColor;
        } 
        
    }
}