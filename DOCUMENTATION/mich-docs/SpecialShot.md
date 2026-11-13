# Special Shot

The gun's charged ability. The player charges it by hitting weakpoints in a row without misfiring. Once charged it stays banked, and it's fired by **holding both fire buttons and releasing them**.

It's a piercing, rail-gun style shot that:

- kills Standard and Thrall enemies outright,
- instantly staggers Champions and bosses,
- is the only thing that can destroy **Special weakpoints**,
- costs no ammo and can't misfire.

It starts locked and is unlocked by picking up an item.

**See also:** Enemy Behaviour, Camera Effects (hit feedback), Shaders & VFX (Impact Frame, Shatter).

---

## Files

The Special Shot is spread across the existing weapon scripts:

| File | Special Shot job |
|---|---|
| `SpecialShot.cs` | Charge state and streak. |
| `WeaponInputReader.cs` | Tells "one button" apart from "both buttons". |
| `ShotOrchestrator.cs` | Fires the shot and works out what it hit. |
| `WeaponHitscan.cs` | The piercing raycast (`GetSpecialShotHits`). |
| `GunVisuals.cs` | Charge bar, glow while charging, fire effects. |
| `ShotOutcome.cs` | The `SpecialHit` and `SpecialMiss` outcomes. |
| `WeakPoint.cs`, `WeakPointType.cs` | The `Special` weakpoint type. |
| `Projectile.cs`, `DamageInfo.cs` | Which shot types can destroy a projectile. |
| `Enemy.cs` | **Immune To Special Shot** and what a body hit does. |
| `AbilityManager.cs` | Unlocking. |

---

## Setup

**On the weapon:**

1. Add **SpecialShot** next to the other weapon components and set **Streak To Charge**.
2. On **WeaponInputReader**, set **Special Shot Buffer** to about 0.1 seconds (see [Input](#input)).
3. On **ShotOrchestrator**, assign **Special Shot** (found automatically if on the same object).
4. On **GunVisuals**, assign the True Shot fields (see [Visuals](#visuals)).

**For unlocking:**

5. Give the unlock pickup's `CollectibleObject` a Grimoire entry with a name (e.g. `Silver Bullet`).
6. On **AbilityManager**, set **Special Shot Entry Name** to exactly that name.

Tick **Start Unlocked** on SpecialShot to skip the pickup while testing.

---

## Charging

| State | Meaning |
|---|---|
| **Locked** | Not unlocked yet. |
| **Charging** | Each weakpoint hit adds 1 to the streak. Reaching **Streak To Charge** makes it Ready. |
| **Ready** | Banked. Nothing can lose the charge. |

**Breaks the streak:** a miss, the wrong ammo, or shooting a staggered enemy's body. Everything else leaves it alone.

Firing resets the streak to 0, even if the shot misses.

> **In code:** `SpecialShot` listens to `WeaponEvents.ShotResolved` and checks `result.Outcome.Rules().Combo` (Increment, Break or Neutral). `Unlock()` is called by `AbilityManager`, which checks the Grimoire for the entry name each frame. UI can listen to `OnStreakChanged(current, needed)` and `OnStateChanged(state)`.

---

## Input

- **No charge ready:** pressing a button fires its normal shot straight away, as usual.
- **Charge ready:** a single press waits up to **Special Shot Buffer** seconds for the other button:
  - other button pressed in time → **charging**; releasing both fires the Special Shot,
  - button released (a tap) → normal shot fires straight away,
  - button held past the buffer → normal shot fires.

One press always gives one shot. If the gun is still on cooldown when both buttons are released, the Special Shot fires as soon as it's free. It can't be fired during a reload.

> **In code:** `WeaponInputReader.GetShotIntent()` returns one `ShotIntent` per frame (`None`, `Iron`, `Silver`, `Special`) from a small state machine (Idle, Pending, Single, Charging). `IsChargingSpecial` is true while both buttons are held with a charge ready, which the visuals and the camera hold shake use. `ShotOrchestrator` queues a Special intent in `queuedSpecialShot` until the gun is free.

---

## What the Shot Hits

The shot travels along the normal aim ray and passes through targets:

| It meets | Result |
|---|---|
| A **wall** or solid prop | Stops. |
| A non-enemy **trigger** (room volumes etc.) | Passes through. |
| A **Special weakpoint** | Destroyed. |
| An **Iron/Silver weakpoint** | Passes through untouched. |
| An **enemy body** | Standard/Thrall die, Champions and bosses are staggered. |
| An enemy with **Immune To Special Shot** | Stops, **unless** the shot also goes through that enemy's exposed Special weakpoint, then it pierces. |
| A **projectile** | Destroyed if its **Destroyable By** includes Special, otherwise passed through. |

Each target hit counts separately for score and camera feedback, so a shot through three enemies scores three times.

> **In code:** `WeaponHitscan.GetSpecialShotHits()` uses `Physics.RaycastAll`, sorts hits by distance, and cuts the list at the first solid collider or blocking immune enemy. `ShotOrchestrator.ResolveSpecialShot()` then handles each hit and raises one `ShotResolved` with `SpecialHit` per target (or a single `SpecialMiss`). Body hits call `Enemy.HandleSpecialShotHit()`, which returns true if it did something. Override it in an `Enemy` subclass to change what the Special Shot does to that enemy (the bosses do this).

---

## Special Weakpoints

Set a WeakPoint's **Weak Point Type** to **Special** and assign its **Special Element** visual. It can only be destroyed by the Special Shot. Iron or Silver shots on it count as **wrong ammo** (a misfire), so make Special weakpoints look clearly different.

Special weakpoints follow the WeakPointManager's list order like any other weakpoint.

> **In code:** destroying one raises the static event `WeakPoint.SpecialDestroyed(Vector3 position)` for anything that needs to react.

---

## Projectiles and Shot Types

Each enemy projectile chooses which shots can destroy it with **Destroyable By** (Iron, Silver, Special, any mix). Leave it empty for a projectile that can't be shot down.

- Iron/Silver on an allowed projectile: destroyed, no ammo used.
- Iron/Silver on a projectile that doesn't allow it: **wrong ammo** misfire.

> **In code:** the shot type travels in `DamageInfo.shotType` as a `ShotTypeMask` flags enum, so any `IDamageable` can check what hit it. Non-player damage uses `ShotTypeMask.None`. Convert with `weakPointType.ToShotMask()`.

---

## Shot Outcomes

Every shot produces a `ShotOutcome`, and each outcome has fixed rules in one table in `ShotOutcome.cs`:

| Outcome | Combo | Points | Keeps ammo | Miss popup |
|---|---|---|---|---|
| Miss | Break | | | Yes |
| Wrong Ammo | Break | | | |
| Enemy Hit | Neutral | | Yes | |
| Enemy Hit Staggered | Break | | | |
| Shootable Target Hit | Neutral | | Yes | |
| Weak Point Hit | Increment | Yes | Yes | |
| **Special Hit** | Neutral | Yes | Yes | |
| **Special Miss** | Neutral | | Yes | Yes |

Scoring, the Fear bar and the Special Shot all read these rules, so change behaviour here rather than in each system.

---

## Visuals

| GunVisuals field | What it does |
|---|---|
| **True Shot** | The `SpecialShot` to read. |
| **True Shot Charge Bar** | Image whose Fill Amount shows the streak. |
| **True Shot Charge Indicator** | Image shown when fully charged. |
| **True Shot Material** | Swapped onto the gun while charging. |
| **True Shot Charge Fx Emitter** / **True Shot Fx Emitter** | Particles while charging and on fire. |
| **Special Muzzle Flash** | Falls back to the Iron flash if empty. |

The gun's Animator bool **`trueshot`** is true while charging.

The camera shake while charging and the hitstop, impact frame and shake on hits are covered in Camera Effects.

---

## Gotchas

- **The unlock entry name must match exactly.**
- **Something the shot should stop at isn't stopping it?** It's probably a trigger.
- **Immune enemies shield everything behind them** unless the shot goes through their Special weakpoint.
