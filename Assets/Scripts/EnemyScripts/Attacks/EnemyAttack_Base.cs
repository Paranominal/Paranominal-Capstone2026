// Summary:
// Abstract base class for all modular enemy attacks. Provides a shared interface for the behaviour script: range, cooldown, state queries, lifecycle events,
// and windup indicator management. Subclasses implement their own attack sequences and call StartCooldown() when finished.

using System;
using System.Collections;
using UnityEngine;

public abstract class EnemyAttack_Base : MonoBehaviour
{
    [Header("Attack Range")]
    [Tooltip("Distance within which the behaviour script will consider using this attack.")]
    [SerializeField] private float attackRange = 5f;

    [Header("Cooldown")]
    [Tooltip("Time between attacks, measured from end of one to availability of next.")]
    [SerializeField] private float cooldownTime = 2f;

    [Header("Windup Indicator")]
    [Tooltip("Optional sprite shown during windup. Hidden on spawn, toggled automatically.")]
    [SerializeField] private SpriteRenderer windupIndicator;

    public float AttackRange => attackRange;
    public float CooldownTime => cooldownTime;

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

    // Override for attack-specific usage conditions (e.g. "only on stationary targets"). Returns true by default. the behaviour script checks this during attack selection.
    public virtual bool ShouldUse(Transform target) => true;


    // Attack Lifecycle
    // hide indicator on spawn so it's never visible before the first attack
    protected virtual void Awake()
    {
        SetWindupIndicator(false);
    }


    // Attack Cooldown
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


    // Attack Indicators
    // toggle the windup indicator directly if needed outside the event invokers
    protected void SetWindupIndicator(bool show)
    {
        if (windupIndicator != null) windupIndicator.enabled = show;
    }


    // Event Invokers
    // subclasses call these to fire the shared events. Windup indicator is managed automatically through these.
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
