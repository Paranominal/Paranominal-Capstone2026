# Enemy Encounter & Spawning System

This system gets enemies into the level. There are two ways to spawn enemies, and they can be used side by side:

- **Encounter rooms** (`EnemyEncounterManager`): waves of enemies that start when the player walks in, pause when they leave, and reset if they stay away too long.
- **Standalone spawn points** (`EnemySpawnPoint_Standalone`): a single enemy that spawns once, when the player comes near or when something tells it to. The boss fight uses these.

This document replaces the old *Enemy Encounter Manager - Quick Start*.

**See also:** Enemy Behaviour, World-space Labels & Interaction (doors), Editor Scripts (the Encounter Manager's custom Inspector).

---

## Files

| File | Job |
|---|---|
| `EnemyEncounterManager.cs` | Runs an encounter room. |
| `EnemySpawnPoint.cs` | A spawn location in an encounter room. |
| `EnemySpawnPoint_Standalone.cs` | Spawns one enemy once. Also locks boss doors. |
| `RoomEntryDetector.cs` | Trigger that tells a room when the player enters or leaves. Also starts boss fights. |
| `IEnemySpawner.cs` | Interface so spawners hear about their enemies dying. |

> **In code:** every spawner implements `IEnemySpawner.NotifyEnemyDeath(Enemy)`. When a spawner creates an enemy it calls `enemy.SetOwnerSpawner(this)`, and the enemy reports back exactly once when it dies or is destroyed. `Enemy_BossMain` also implements it to track its minions.

---

## Setting Up an Encounter Room

```
EncounterRoom          <- EnemyEncounterManager
  SpawnPoint_01        <- EnemySpawnPoint
  SpawnPoint_02        <- EnemySpawnPoint
  RoomTrigger          <- Trigger Collider + RoomEntryDetector
```

1. Add **Enemy Encounter Manager** to an empty GameObject for the room.
2. Add child objects with **Enemy Spawn Point**. Rotate them to set which way enemies face.
3. Add a child with a **trigger collider** covering the room and a **Room Entry Detector**.
4. Pick an **Encounter Mode** and fill in its settings (below).

Spawn points **must be children of the manager**. The manager collects them automatically, so the Spawn Points list fills itself.

The player must be tagged **Player**.

### How it runs

1. The player enters the trigger. After **Initial Spawn Delay**, the first wave spawns.
2. When every enemy in the wave is dead, the manager waits **Time Between Waves** and spawns the next.
3. After **Max Waves**, the room is complete and never runs again.

**If the player leaves mid-fight:** enemies freeze and a **Reset Counter** countdown starts. Come back in time and the fight carries on. Otherwise the enemies are destroyed and the room resets to wave one.

Delays only count down while the player is in the room.

> **In code:** the wave loop is the `WaveLoopRoutine` coroutine. Player entry and exit come in through `SetPlayerInRoom(bool)`, which pauses enemies with `Enemy.SetPaused`. A wave is cleared when the `spawnedEnemies` list is empty.

---

## Standard Mode

Hand-placed waves. Each spawn point's **Enemy Pool** has one slot per wave (it resizes automatically when Max Waves changes). Put an enemy prefab in each slot, or leave it empty to spawn nothing there that wave.

| Spawn point | Wave 1 | Wave 2 | Wave 3 |
|---|---|---|---|
| SpawnPoint_01 | Imp | Imp | Gargoyle |
| SpawnPoint_02 | Imp | *(empty)* | Imp |

Spawned enemies are hidden for **Spawn Visual Delay** seconds so the Animator can set the spawn pose before they appear.

---

## Arena Mode

Random waves built from a budget. Each wave's budget is **wave number x Budget Per Wave**. The manager keeps picking affordable enemies from the pool (weighted by **Spawn Weight**) until the budget or **Max Enemies Per Wave** runs out.

| Enemy Pool field | What it does |
|---|---|
| **Enemy Prefab** | The enemy. |
| **Cost** | Budget it uses. Entries costing 0 are skipped. |
| **Minimum Wave** | First wave it can appear in. |
| **Spawn Weight** | Relative chance of being picked. |

| Setting | What it does |
|---|---|
| **Starting Wave** | Start further in, with a bigger budget and more enemies unlocked. |
| **Spawn Timing Mode** | All at once, or spread over **Wave Duration**. |
| **Spawn Location Mode** | Use the child spawn points (random or in order), or random NavMesh points within **Random Spawn Radius** (needs a **Ground Layer**). |

---

## If a Room Does Nothing

The manager **disables itself** on Start, with a warning in the Console, if:

- Max Waves or Reset Counter is 0,
- it has no child spawn points (even in Arena mode with random positions),
- *(Arena)* the Enemy Pool is empty, Wave Duration is 0 with timed spawning, or random spawning has no radius or Ground Layer.

Check the Console first.

---

## Standalone Spawn Points

Spawns **one** enemy, **once**, without needing an Encounter Manager.

| Field | What it does |
|---|---|
| **Use Radius Detection** / **Detection Radius** | Spawn when the player gets this close (yellow gizmo). Off = only spawns when told to. |
| **Enemy Prefab** | The enemy. |
| **Is Boss Spawner** / **Boss Doors** | Locks these doors when the enemy spawns and unlocks them when it dies (red gizmo lines). |

> **In code:** call `SpawnEnemy()` from any script, UnityEvent or trigger to spawn it. Calling it again does nothing. `HasSpawned` and `SpawnedEnemy` give its state. Boss doors use `Door.BossLock()` and `Door.BossUnlock()`.

---

## Setting Up the Boss Fight

```
BossRoom
  BossTrigger      <- Trigger Collider + RoomEntryDetector (Is Boss Room on)
  MainBossSpawn    <- EnemySpawnPoint_Standalone (Is Boss Spawner on, Boss Doors set)
  SubBossSpawn     <- EnemySpawnPoint_Standalone
```

1. Add a trigger over the arena with a **Room Entry Detector**. It must **not** be under an Encounter Manager. Tick **Is Boss Room**.
2. Add a standalone spawn point for the **Main boss**, with radius detection off, **Is Boss Spawner** on and the arena doors in **Boss Doors**.
3. Add a standalone spawn point for the **Sub-boss**, with radius detection off.
4. Add both spawn points to the detector's **Boss Spawn Points**.

When the player walks in, both bosses spawn, link up, and the doors lock. When the Main dies, everything dies with it and the doors unlock.

> **In code:** `RoomEntryDetector` also raises a `PlayerEntry` event on every entry, for anything else that should react to the player arriving.

---

## Gotchas

- **Every spawned prefab needs an `Enemy` component** (or subclass) on its root, or the spawner can't track it.
- **A trigger under an Encounter Manager ignores Is Boss Room.**
- **Standalone spawn points never respawn.**
- **Resetting a room destroys its enemies outright.** No death effects, no Bestiary kills.
