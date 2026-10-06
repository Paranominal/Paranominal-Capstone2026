// Summary:
// Main boss. Each phase is one full weakpoint cycle, and completing the final cycle kills it.
// A phase change ends the current stagger, summons that phase's minions, and releases any Downed Sub-bosses.
// Natural stagger recovery also summons minions and releases the Subs.
// When the Main dies, its Sub-bosses and any living minions die with it.
// Minions spawn at random points within a radius around the boss and persist between phases.
// EDIT (boss-autolink): if Sub Bosses is left empty, the Main links any Sub-bosses automatically, whichever order they spawn in.

using System;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_BossMain : Enemy_Boss, IEnemySpawner
{
    [Serializable]
    public class Phase
    {
        [Tooltip("Attacks usable in this phase. Leave empty to allow every attack. Priority still follows the Attacks list.")]
        public EnemyAttack_Base[] allowedAttacks;
        [Tooltip("Minions summoned when this phase starts. Ignored on the first phase.")]
        [Min(0)] public int summonsOnPhaseStart;
        [Tooltip("Minions summoned when the boss naturally recovers from a stagger during this phase.")]
        [Min(0)] public int summonsOnStaggerRecovery;
    }

    [Header("Boss")]
    // EDIT (boss-autolink): tooltip updated
    [Tooltip("Sub-bosses fought alongside this boss. Leave empty to link Sub-bosses automatically (including ones spawned later). Nothing needs assigning on the Subs.")]
    [SerializeField] private Enemy_BossSub[] subBosses;

    [Header("Phases")]
    [Tooltip("One entry per phase. Each phase is one full weakpoint cycle. The boss dies when the last phase's cycle is completed.")]
    [SerializeField] private Phase[] phases = new Phase[] { new Phase() };

    [Header("Minions")]
    [SerializeField] private GameObject minionPrefab;
    [Tooltip("Minions spawn at a random point within this radius around the boss.")]
    [SerializeField] private float minionSpawnRadius = 6f;
    [Tooltip("Height above the boss's pivot that minions spawn at.")]
    [SerializeField] private float minionSpawnHeight = 2f;
    [Tooltip("Spawn points overlapping these layers are rejected (e.g. walls and props).")]
    [SerializeField] private LayerMask minionBlockingLayers = ~0;
    [Tooltip("Radius of the overlap check used to keep minions out of walls.")]
    [SerializeField] private float minionClearance = 0.5f;

    #if UNITY_EDITOR
    [ShowIf("debugMode", Header = "Boss Runtime State (Play Mode)")]
    [SerializeField] private int _phase;
    [ShowIf("debugMode")]
    [SerializeField] private int _livingMinions;
    #endif

    private const int SpawnPointAttempts = 8;

    // runtime
    private int currentPhase;
    private bool endingStaggerForPhase;     // ignores the OnStaggerEnd fired by our own phase-change ForceEndStagger
    private readonly List<Enemy> minions = new List<Enemy>();

    // EDIT (boss-autolink): Subs linked at runtime (manual or automatic), and every active Main so late-spawned Subs can find one
    private readonly List<Enemy_BossSub> linkedSubs = new List<Enemy_BossSub>();
    private static readonly List<Enemy_BossMain> activeMains = new List<Enemy_BossMain>();

    public override int CurrentPhase => currentPhase;
    public int PhaseCount => phases != null && phases.Length > 0 ? phases.Length : 1;
    // EDIT (boss-autolink): only Mains with no manually assigned Subs accept automatic links
    public bool AutoLinksSubs => !HasManualSubs();

    // raised when the boss enters a new phase (0-based), for music, UI, etc.
    public event Action<int> PhaseChanged;


    // Lifecycle
    protected override void Awake()
    {
        base.Awake();

        if (stagger != null) stagger.OnStaggerEnd += HandleStaggerEnd;

        // EDIT (boss-autolink): manual Subs first, otherwise pick up any unlinked Subs already spawned
        if (HasManualSubs())
        {
            foreach (Enemy_BossSub sub in subBosses)
                LinkSub(sub);
        }
        else
        {
            foreach (Enemy_BossSub sub in Enemy_BossSub.GetActiveSubs())
            {
                if (sub.MainBoss == null) LinkSub(sub);
            }
        }

        activeMains.RemoveAll(m => m == null);
        activeMains.Add(this);
    }

    // EDIT (boss-autolink): clears the static list when entering play mode (covers domain reload being turned off)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ClearActiveMains()
    {
        activeMains.Clear();
    }

    // EDIT (boss-autolink): returns a living Main that accepts automatic links, or null. Used by Subs that spawn later.
    public static Enemy_BossMain FindAutoLinkMain()
    {
        activeMains.RemoveAll(m => m == null);
        foreach (Enemy_BossMain main in activeMains)
        {
            if (!main.IsDying && main.AutoLinksSubs) return main;
        }
        return null;
    }

    // EDIT (boss-autolink): links a Sub to this Main
    public void LinkSub(Enemy_BossSub sub)
    {
        if (sub == null || linkedSubs.Contains(sub)) return;
        linkedSubs.Add(sub);
        sub.LinkToMain(this);
        if (debugMode) Debug.Log($"[{this}] Linked Sub-boss {sub.name}.");
    }

    private bool HasManualSubs()
    {
        if (subBosses == null) return false;
        foreach (Enemy_BossSub sub in subBosses)
        {
            if (sub != null) return true;
        }
        return false;
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        if (phases == null || phases.Length == 0) phases = new Phase[] { new Phase() };
    }


    // Phases
    protected override void OnCycleComplete(int cyclesComplete)
    {
        // final cycle
        if (cyclesComplete >= PhaseCount)
        {
            if (debugMode) Debug.Log($"[{this}] Final phase complete.");
            Die();
            return;
        }

        currentPhase = cyclesComplete;
        if (debugMode) Debug.Log($"[{this}] Entering phase {currentPhase + 1}/{PhaseCount}.");
        #if UNITY_EDITOR
        _phase = currentPhase;
        #endif

        // end the leftover stagger now, without counting it as a natural recovery
        endingStaggerForPhase = true;
        if (stagger != null) stagger.ForceEndStagger();
        endingStaggerForPhase = false;

        SummonMinions(GetPhase().summonsOnPhaseStart);
        ReleaseSubs();
        PhaseChanged?.Invoke(currentPhase);
    }

    private void HandleStaggerEnd()
    {
        if (endingStaggerForPhase || IsDying) return;

        if (debugMode) Debug.Log($"[{this}] Recovered from stagger.");
        SummonMinions(GetPhase().summonsOnStaggerRecovery);
        ReleaseSubs();
    }

    private Phase GetPhase()
    {
        if (phases == null || phases.Length == 0) return new Phase();
        return phases[Mathf.Clamp(currentPhase, 0, phases.Length - 1)];
    }

    protected override EnemyAttack_Base[] GetAllowedAttacks()
    {
        return GetPhase().allowedAttacks;
    }

    // EDIT (boss-autolink): uses the runtime list
    private void ReleaseSubs()
    {
        foreach (Enemy_BossSub sub in linkedSubs)
        {
            if (sub != null) sub.ReleaseFromDowned();
        }
    }


    // Death
    public override void Die()
    {
        if (IsDying) return;
        base.Die();

        // EDIT (boss-autolink): stop accepting links, and use the runtime list
        activeMains.Remove(this);
        foreach (Enemy_BossSub sub in new List<Enemy_BossSub>(linkedSubs))
        {
            if (sub != null) sub.Die();
        }
        KillMinions();
    }


    // Minions
    private void SummonMinions(int count)
    {
        if (count <= 0) return;
        if (minionPrefab == null)
        {
            Debug.LogWarning($"[{this}] Missing minion prefab!", gameObject);
            return;
        }

        if (debugMode) Debug.Log($"[{this}] Summoning {count} minions.");

        for (int i = 0; i < count; i++)
        {
            GameObject spawned = Instantiate(minionPrefab, GetMinionSpawnPoint(), Quaternion.identity);
            Enemy minion = spawned.GetComponent<Enemy>();
            if (minion == null)
            {
                Debug.LogWarning($"[{this}] Minion prefab {minionPrefab.name} is missing an Enemy component.", gameObject);
                continue;
            }

            minion.SetOwnerSpawner(this);
            minions.Add(minion);
        }

        #if UNITY_EDITOR
        _livingMinions = minions.Count;
        #endif
    }

    // random point in a circle around the boss, rejecting points inside blocking geometry
    private Vector3 GetMinionSpawnPoint()
    {
        Vector3 centre = transform.position + Vector3.up * minionSpawnHeight;

        for (int i = 0; i < SpawnPointAttempts; i++)
        {
            Vector2 circle = UnityEngine.Random.insideUnitCircle * minionSpawnRadius;
            Vector3 point = centre + new Vector3(circle.x, 0f, circle.y);
            if (!Physics.CheckSphere(point, minionClearance, minionBlockingLayers, QueryTriggerInteraction.Ignore))
                return point;
        }

        return centre;
    }

    private void KillMinions()
    {
        // copy first, Die() reports back through NotifyEnemyDeath and edits the list
        List<Enemy> living = new List<Enemy>(minions);
        foreach (Enemy minion in living)
        {
            if (minion != null && !minion.IsDying) minion.Die();
        }
        minions.Clear();
    }

    public void NotifyEnemyDeath(Enemy deadEnemy)
    {
        if (deadEnemy == null) return;
        minions.Remove(deadEnemy);

        #if UNITY_EDITOR
        _livingMinions = minions.Count;
        #endif
    }

    


    // Scene Gizmos
#if UNITY_EDITOR
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        // minion spawn ring (magenta)
        Gizmos.color = Color.magenta;
        Vector3 centre = transform.position + Vector3.up * minionSpawnHeight;
        const int segments = 32;
        Vector3 prev = centre + new Vector3(minionSpawnRadius, 0f, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float a = i / (float)segments * Mathf.PI * 2f;
            Vector3 next = centre + new Vector3(Mathf.Cos(a) * minionSpawnRadius, 0f, Mathf.Sin(a) * minionSpawnRadius);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }

        // links to the Subs (cyan). EDIT (boss-autolink): shows runtime links in play mode
        Gizmos.color = Color.cyan;
        if (Application.isPlaying)
        {
            foreach (Enemy_BossSub sub in linkedSubs)
            {
                if (sub != null) Gizmos.DrawLine(transform.position, sub.transform.position);
            }
        }
        else if (subBosses != null)
        {
            foreach (Enemy_BossSub sub in subBosses)
            {
                if (sub != null) Gizmos.DrawLine(transform.position, sub.transform.position);
            }
        }
    }
    #endif
}