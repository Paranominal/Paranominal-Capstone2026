// Summary:
// Sub-boss, fought alongside a Main boss (linked from the Main's Sub Bosses list).
// EDIT (boss-autolink): if the Main's Sub Bosses list is empty, the Sub links itself to it automatically, whichever order they spawn in.
// Never dies from weakpoints. Completing a cycle puts it into a Downed state (no attacks, immune to hits, immune feedback plays)
// until the Main recovers from a stagger or changes phase. Dies when the Main dies.
// EDIT (downed-timer): Downed can also end on its own after a set time (0 = no timer). Whichever happens first wins.
// Its attacks are gated by the Main's current phase.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_BossSub : Enemy_Boss
{
    [Serializable]
    public class Phase
    {
        [Tooltip("Attacks usable while the Main boss is in this phase. Leave empty to allow every attack.")]
        public EnemyAttack_Base[] allowedAttacks;
    }

    [Header("Phases")]
    [Tooltip("Indexed by the Main boss's phase. If shorter than the Main's list, the last entry carries on. Leave empty to allow every attack.")]
    [SerializeField] private Phase[] phases;

    // EDIT (downed-timer)
    [Header("Downed")]
    [Tooltip("Seconds before the Sub gets back up on its own. 0 = stays down until the Main recovers from a stagger or changes phase.")]
    [Min(0f)]
    [SerializeField] private float downedDuration = 0f;

    // runtime
    private Enemy_BossMain mainBoss;
    // EDIT (downed-timer)
    private Coroutine downedTimer;
    // EDIT (boss-autolink): every active Sub, so a Main that spawns later can find them
    private static readonly List<Enemy_BossSub> activeSubs = new List<Enemy_BossSub>();

    public Enemy_BossMain MainBoss => mainBoss;
    public override int CurrentPhase => mainBoss != null ? mainBoss.CurrentPhase : 0;


    // Lifecycle
    // EDIT (boss-autolink): registers itself, and links to a Main that's already spawned if one accepts automatic links
    protected override void Awake()
    {
        base.Awake();

        activeSubs.RemoveAll(s => s == null);
        activeSubs.Add(this);

        if (mainBoss == null)
        {
            Enemy_BossMain main = Enemy_BossMain.FindAutoLinkMain();
            if (main != null) main.LinkSub(this);
        }
    }

    private void Start()
    {
        // EDIT (boss-autolink): no longer a problem on its own, a Main spawned later will link it
        if (mainBoss == null && debugMode)
            Debug.Log($"[{this}] No Main boss linked yet. It will link when a Main spawns.", gameObject);
    }

    // EDIT (boss-autolink): clears the static list when entering play mode (covers domain reload being turned off)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ClearActiveSubs()
    {
        activeSubs.Clear();
    }

    // EDIT (boss-autolink): living Subs, used by a Main when it spawns
    public static List<Enemy_BossSub> GetActiveSubs()
    {
        activeSubs.RemoveAll(s => s == null || s.IsDying);
        return new List<Enemy_BossSub>(activeSubs);
    }

    // called by the Main in its Awake
    public void LinkToMain(Enemy_BossMain main)
    {
        mainBoss = main;
    }


    // Phases
    protected override void OnCycleComplete(int cyclesComplete)
    {
        EnterDowned();
    }

    protected override EnemyAttack_Base[] GetAllowedAttacks()
    {
        if (phases == null || phases.Length == 0) return null;
        return phases[Mathf.Clamp(CurrentPhase, 0, phases.Length - 1)].allowedAttacks;
    }


    // Downed
    private void EnterDowned()
    {
        if (IsDying || IsDowned) return;
        if (debugMode) Debug.Log($"[{this}] Downed.");

        CancelCurrentAttack();
        behaviourState = BehaviourState.Downed;

        if (stagger != null)
        {
            stagger.ForceEndStagger();
            stagger.SetImmune(true);
            // covers alwaysShowAll, which re-shows weakpoints when the cycle resets
            if (stagger.weakPointManager != null) stagger.weakPointManager.EndSequence();
        }

        // EDIT (downed-timer): start the timer if one is set
        if (downedDuration > 0f) downedTimer = StartCoroutine(DownedTimer());
    }

    // EDIT (downed-timer): counts down while Downed, skipping time while the enemy is paused, then releases
    private IEnumerator DownedTimer()
    {
        float remaining = downedDuration;
        while (remaining > 0f)
        {
            if (!IsPaused) remaining -= Time.deltaTime;
            yield return null;
        }

        downedTimer = null;
        if (debugMode) Debug.Log($"[{this}] Downed timer ran out.");
        ReleaseFromDowned();
    }

    // called by the Main when it recovers from a stagger or changes phase, or by the Downed timer
    public void ReleaseFromDowned()
    {
        if (!IsDowned || IsDying) return;
        if (debugMode) Debug.Log($"[{this}] Released from Downed.");

        // EDIT (downed-timer): stop the timer if the Main released it first
        if (downedTimer != null)
        {
            StopCoroutine(downedTimer);
            downedTimer = null;
        }

        if (stagger != null) stagger.SetImmune(false);
        behaviourState = BehaviourState.Idling;
    }
}