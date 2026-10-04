// Summary:
// Abstract base class for all modular enemy attacks. Provides a shared interface for the behaviour script: range, cooldown, state queries, lifecycle events, and windup indicator management. 
// Subclasses implement their own attack sequences and call StartCooldown() when finished.

using System;
using System.Collections;
using UnityEngine;

public abstract class EnemyAttack_Base : MonoBehaviour
{
    [Header("Attack Range")]
    // EDIT (attack-priority): min range lets attacks have a dead zone (e.g. a ranged attack that won't fire up close).
    [Tooltip("The attack won't be used if the target is closer than this. 0 = no minimum.")]
    [SerializeField] private float minRange = 0f;
    // EDIT (attack-priority): tooltip updated to describe how range drives movement.
    [Tooltip("Max distance at which this attack can be used. When this is the enemy's highest-priority ready attack, the enemy moves to this distance.")]
    [SerializeField] private float attackRange = 5f;

    [Header("Cooldown")]
    [Tooltip("Time between attacks, measured from end of one to availability of next.")]
    [SerializeField] private float cooldownTime = 2f;

    [Header("Windup Indicator")]
    [Tooltip("Optional sprite shown during windup. Hidden on spawn, toggled automatically.")]
    [SerializeField] private SpriteRenderer windupIndicator;

    public float AttackRange => attackRange;
    // EDIT (attack-priority)
    public float MinRange => minRange;
    public float CooldownTime => cooldownTime;

    [Header("Windup Movement")]
    [Tooltip("If enabled, the enemy continues moving during the windup phase instead of stopping immediately.")]
    [SerializeField] private bool moveWhileWindingUp;
    public bool MoveWhileWindingUp => moveWhileWindingUp;

    // state
    private bool isOnCooldown;
    private Coroutine cooldownRoutine;

    // properties
    public bool IsReady => !IsAttacking && !isOnCooldown;
    public abstract bool IsAttacking { get; }
    public virtual bool IsWindingUp => false;
    public virtual float WindupDuration => 0f;

    // events for feedback components, animation, etc.
    public event Action OnWindupStart;
    public event Action OnStrikeStart;
    public event Action OnStrikeEnd;
    public event Action OnAttackCancelled;

    // core interface
    public abstract void PerformAttack(Transform target);
    public abstract void CancelAttack();

    // override for attack-specific usage conditions (e.g. "only on stationary targets").
    // returns true by default. the behaviour script checks this during attack selection.
    public virtual bool ShouldUse(Transform target) => true;

    // EDIT (attack-priority): shared range check so attack selection always respects min and max range.
    // tolerance is a small buffer on the max range only.
    public bool IsInRange(float distance, float tolerance)
    {
        return distance >= minRange && distance <= attackRange + tolerance;
    }


    // Lifecycle
    // hide indicator on spawn so it's never visible before the first attack
    protected virtual void Awake()
    {
        SetWindupIndicator(false);
    }

    // EDIT (attack-priority): keep min range from exceeding max range
    protected virtual void OnValidate()
    {
        minRange = Mathf.Clamp(minRange, 0f, attackRange);
    }


    // Cooldowm
    protected void StartCooldown()
    {
        if (cooldownRoutine != null) StopCoroutine(cooldownRoutine);
        cooldownRoutine = StartCoroutine(CooldownSequence(cooldownTime));
    }

    protected void StartCooldown(float multiplier)
    {
        if (cooldownRoutine != null) StopCoroutine(cooldownRoutine);
        cooldownRoutine = StartCoroutine(CooldownSequence(cooldownTime * multiplier));
    }

    private IEnumerator CooldownSequence(float duration)
    {
        isOnCooldown = true;
        float timer = duration;
        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            yield return null;
        }
        isOnCooldown = false;
        cooldownRoutine = null;
    }


    // Indicators
    // toggle the windup indicator directly if needed outside the event invokers
    protected void SetWindupIndicator(bool show)
    {
        if (windupIndicator != null) windupIndicator.enabled = show;
    }


    // Evemt Invokers
    // subclasses call these to fire the shared events.
    // windup indicator is managed automatically through these.
    protected void InvokeWindupStart()
    {
        SetWindupIndicator(true);
        OnWindupStart?.Invoke();
    }

    protected void InvokeStrikeStart()
    {
        SetWindupIndicator(false);
        OnStrikeStart?.Invoke();
    }

    protected void InvokeStrikeEnd()
    {
        OnStrikeEnd?.Invoke();
    }

    protected void InvokeAttackCancelled()
    {
        SetWindupIndicator(false);
        OnAttackCancelled?.Invoke();
    }
}