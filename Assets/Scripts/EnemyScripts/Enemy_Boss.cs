// Summary:
// Shared base for the boss scripts (Enemy_BossMain and Enemy_BossSub). Not added to GameObjects directly.
// Locks the enemy class to Boss, detects completed weakpoint cycles (one per phase), gates attacks by phase,
// and makes the Special Shot stagger the boss in one hit.
// Note: Enemy's Update, Reset, OnDestroy and OnDisable are private, so boss scripts must not declare those methods (it would hide them).

using System;
using UnityEngine;

public abstract class Enemy_Boss : Enemy
{
    private int handledCycles;

    public bool IsDowned => behaviourState == BehaviourState.Downed;
    // current phase of the fight (0-based). The Sub reads the Main's.
    public abstract int CurrentPhase { get; }


    // Lifecycle
    protected override void Awake()
    {
        enemyClass = EnemyClass.Boss;
        base.Awake();
    }

    protected virtual void OnValidate()
    {
        // bosses are always the Boss class, the dropdown is locked
        enemyClass = EnemyClass.Boss;
    }


    // Phases
    // replaces the Champion/Standard death checks. Reacts once per completed weakpoint cycle.
    protected override void CheckDie()
    {
        if (stagger == null || stagger.weakPointManager == null) return;
        if (IsDying) return;

        int cycles = stagger.weakPointManager.CyclesComplete;
        if (cycles <= handledCycles) return;
        handledCycles = cycles;

        OnCycleComplete(handledCycles);
    }

    // called once each time a full weakpoint cycle is completed (total cycles so far)
    protected abstract void OnCycleComplete(int cyclesComplete);


    // Attack Gating
    protected override bool CanUseAttack(EnemyAttack_Base attack)
    {
        if (!base.CanUseAttack(attack)) return false;

        EnemyAttack_Base[] allowed = GetAllowedAttacks();
        if (allowed == null || allowed.Length == 0) return true;
        return Array.IndexOf(allowed, attack) >= 0;
    }

    // attacks allowed in the current phase. Null or empty allows every attack.
    protected abstract EnemyAttack_Base[] GetAllowedAttacks();


    // Special Shot
    // staggers in one hit, unless Downed, already staggered or still spawning
    public override bool HandleSpecialShotHit()
    {
        if (IsDying || IsDowned) return false;
        if (ImmuneToSpecialShot) return false;
        if (stagger == null || !stagger.canBeHit || IsStunned()) return false;

        if (debugMode) Debug.Log($"[{this}] Staggered by Special Shot");
        stagger.TriggerStagger();
        return true;
    }
}
