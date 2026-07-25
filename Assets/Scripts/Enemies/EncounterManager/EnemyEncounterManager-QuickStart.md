# Enemy Encounter Manager - Quick Start

A quick guide for getting an encounter room set up in a scene.

---

## What's in the System

| Script | Purpose |
|---|---|
| `EnemyEncounterManager.cs` | The main component. Goes on the root encounter room GameObject. |
| `EnemySpawnPoint.cs` | Child component. Marks a spawn location and (in Standard mode) stores the per-wave enemy pool. |
| `RoomEntryDetector.cs` | Add this to a trigger Collider in the room. Notifies the manager when the player enters or leaves. |
| `IEnemySpawner.cs` | Interface - no setup needed. Implemented by the manager. |
| `EnemyBehaviourBase.cs` | Abstract base class - no setup needed. All enemy scripts must inherit from this. |
| `EnemyEncounterManagerEditor.cs` | Custom editor - no setup needed. Place in an `Editor` folder. |

---

## Scene Hierarchy

```
EncounterRoom (GameObject)
  EnemyEncounterManager       <- EnemyEncounterManager.cs lives here
    SpawnPoint_01             <- EnemySpawnPoint.cs on each
    SpawnPoint_02
    ...
  RoomEntryDetector           <- Trigger Collider + RoomEntryDetector.cs
```

> Spawn points must be children of the `EnemyEncounterManager` GameObject. The manager finds them automatically via `GetComponentsInChildren`.

---

## Setting Up a Room

1. Create an empty GameObject for the room. Name it something like `EncounterRoom`.
2. Add **Enemy Encounter Manager** to it.
3. Create child GameObjects for each spawn location. Add **Enemy Spawn Point** to each.
4. Create a trigger Collider on a separate child GameObject. Add **Room Entry Detector** to it.
5. Make sure your player GameObject has the **Player** tag.
6. Select the **Encounter Mode** and fill in the relevant fields.

---

## Standard Mode

Use this when you want specific enemies to spawn at specific points on specific waves.

**On `EnemyEncounterManager`:**
- Set **Encounter Mode** to `Standard`.
- Set **Max Waves** to the number of waves you want.
- Adjust **Time Between Waves** and **Reset Counter** as needed.

**On each `EnemySpawnPoint`:**
- The **Enemy Pool** list has one slot per wave (auto-resized when Max Waves changes).
- Assign an enemy prefab to each slot. Leave a slot empty to skip that spawn point on that wave.

---

## Arena Mode

Use this for dynamic, budget-driven encounters where the enemy mix changes each wave.

**On `EnemyEncounterManager`:**
- Set **Encounter Mode** to `Arena`.
- Set **Max Waves** and **Budget Per Wave**. Wave budget = `currentWave * budgetPerWave`.
- Set **Max Enemies Per Wave** as a hard cap.
- Choose a **Spawn Timing Mode**:
  - `SimultaneousAtWaveStart` - all enemies spawn at once.
  - `OverWaveDuration` - enemies are spread evenly over **Wave Duration** seconds.
- Choose a **Spawn Location Mode**:
  - `SpawnPoints` - uses child `EnemySpawnPoint` transforms as spawn locations.
  - `RandomWithinRadius` - picks random NavMesh positions within **Random Spawn Radius**. Requires a **Ground Layer** to be set.

**In the Enemy Pool**, add one `EnemyEntry` per enemy type:

| Field | Description |
|---|---|
| `enemyPrefab` | The enemy prefab to spawn. |
| `cost` | Budget cost when this enemy is selected. |
| `minimumWave` | Earliest wave this enemy can appear on. |
| `spawnWeight` | Relative spawn probability. Higher = selected more often. |

---

## How the Encounter Runs

1. Nothing happens until the player enters the room trigger.
2. The wave loop starts. Enemies are spawned, and the manager waits for all of them to die before advancing.
3. If the player **leaves the room**, all enemies are paused and a reset countdown begins.
4. If the player **returns** before the countdown expires, enemies unpause and the encounter resumes.
5. If the countdown **expires**, all enemies are destroyed and the encounter resets to the beginning.
6. Once all waves are complete, the encounter is marked done and won't restart.

---

## Enemy Requirements

All enemy prefabs must have a script inheriting from `EnemyBehaviourBase`. At minimum, override:

```csharp
// Called when the enemy is paused or resumed by the manager.
protected override void OnPauseStateChanged(bool isPaused)
{
    navAgent.isStopped = isPaused;
    animator.speed = isPaused ? 0f : 1f;
}

// Called just before the enemy GameObject is destroyed.
protected override void OnDying()
{
    // Disable colliders, play death effects, spawn drops, etc.
}
```

To kill the enemy, call `Die()` from your subclass when health reaches zero.

```csharp
if (currentHealth <= 0)
{
    Die();
}
```

---

## Shared Settings (Both Modes)

| Field | Description |
|---|---|
| **Max Waves** | Total waves before the encounter completes. |
| **Time Between Waves** | Seconds between waves. Only counts while the player is in the room. |
| **Reset Counter** | Seconds the player must be absent before the encounter resets. |
