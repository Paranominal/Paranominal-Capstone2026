// Summary:
// Interface for movement scripts that can be dropped onto Enemy. Implement this on a MonoBehaviour to define how an enemy moves.
// The behaviour script calls the semantic methods (Chase, Strafe, etc.) and the movement script handles them.
// The movement script owns engagement distance, chase territory, and all movement parameters.

using UnityEngine;

public interface IEnemyMovement
{
    void Initialize();

    // called each frame during the respective state
    void Chase(Vector3 target, float stopDistance);
    void Strafe(Vector3 orbitCenter, float orbitRadius);

    // called once when entering the state, movement continues toward stored target
    void BeginRetreat(Vector3 awayFrom);
    void BeginReturn();

    void Stop();
    void FaceTarget(Vector3 target);
    void SetPaused(bool paused);

    // called by the behaviour to check if the enemy should exit chase (territory/leash)
    bool ShouldExitChase(bool playerInAggroRange);
    bool CanChase(bool anyAttackReady);

    bool HasReachedTarget { get; }
    float StrafeRadius { get; }
    bool ReturnEnabled { get; }
    bool RetreatEnabled { get; }
    bool StrafeEnabled { get; }
}
