// Summary:
// Contract for any system that spawns enemies and wants to be notified when one of its
// spawned enemies dies. Implemented by both EnemyEncounterManager (room-based, paused
// when the player leaves) and EnemyArenaSpawner (continuous wave-based).
// Kept deliberately minimal: the only thing an enemy needs to do with its owner is
// report its own death. Anything else stays the responsibility of the concrete spawner.
public interface IEnemySpawner
{
    // Called by an enemy when it dies, so the spawner can remove it from its active list.
    // Safe to call with a null argument; implementations should no-op in that case.
    void NotifyEnemyDeath(EnemyBehaviourBase deadEnemy);
}
