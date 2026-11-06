# Fear Effects

Fear is the player's health. As it rises, the game should *feel* more dangerous. This system turns the player's fear into feedback:

- **Fear Vignette:** a noisy vignette that creeps in from the screen edges.
- **Heartbeat:** louder and faster as fear rises.
- **Intrusive effects:** a ready-made slot for in-world scares (not filled in yet).

The fear value belongs to `FearBar` (part of scoring). This system only reads it.

**See also:** Shaders & VFX (the Fear Vignette shader), Interface & Settings (Fear Vignette setting), Audio Manager.

---

## How It Fits Together

`FearEffectsManager` reads fear and passes it to the effect components next to it:

| Call | When | For |
|---|---|---|
| `UpdateIntensity(normalizedFear, isInEncounter)` | Every frame | Effects that scale smoothly (fear from 0 to 1). |
| `OnRankChanged(rank)` | When `FearBar.OnFearChanged` fires | Effects that change in steps. |

| Rank | Fear |
|---|---|
| Healthy | 15 or less |
| Fine | Above 15, up to 40 |
| Critical | Above 40, up to 80 |
| Doomed | Above 80 |

`isInEncounter` comes from `PlayerStatus.IsInEncounter`, a property for whatever starts and ends combat to set. Nothing uses it yet.

`OnFearChanged` fires on **every** fear change, not just rank changes, so rank handlers should be fine being called with the same rank twice.

---

## Setup

1. Put **FearEffectsManager** on an object in the scene.
2. Put the effect components (**FearPostProcessEffects**, **FearAudioEffects**, **FearIntrusiveEffects**) on the **same object**. Any can be left off.
3. Make sure the scene's Volume profile has a **Fear Vignette** override and the Fear Vignette renderer feature is on the renderer.

Fear Bar and Player Status are found automatically if left empty.

---

## Fear Vignette

```
strength = fear (0 to 1) x Effect Strength x player's Fear Vignette setting
```

As strength rises the vignette closes in, its edge gets more broken up by noise, and the noise moves faster.

| Field | What it does |
|---|---|
| **Post Process Volume** | The Volume with the Fear Vignette override. If empty, uses the first Volume under **SceneEssentialsBundle**. |
| **Vignette Color**, **Blend Mode** | The look (Multiply, Screen, Overlay or Hard Light). |
| **Effect Strength** | Overall multiplier. |
| **Vignette Intensity Max** | How far in it reaches at full fear. |
| **Noise Intensity Max** | How broken up the edge gets at full fear. |
| **Noise Speed Min / Max** | Creep speed at no fear and full fear. |
| **Vignette Softness**, **Noise Scale**, **Cycle Duration** | Passed straight to the shader. |

Changing the Fear Vignette graphics setting re-applies the effect straight away, even while paused. Disabling the component turns the vignette off.

> **In code:** values are written with `Override()` on the `FearVignetteVolume` from the Volume's **`sharedProfile`** (using `profile` would create a copy that never shows on screen).

---

## Heartbeat

Plays at every rank except Healthy. Each rank has its own sound and beat interval, so it speeds up as the rank rises. Volume follows fear smoothly from **Volume Min** to **Volume Max**.

| Field | What it does |
|---|---|
| **Fine / Critical / Doomed Beat** | SoundDataSO per rank (clip and pitch are used, volume comes from fear). |
| **Fine / Critical / Doomed Interval** | Seconds between beats. |
| **Volume Min / Max** | Volume at no fear and full fear. |

It adds its own AudioSource, routed through the Fine Beat's mixer group.

---

## Adding a Fear Effect

1. Write a component with either or both methods:

   ```csharp
   public void UpdateIntensity(float normalizedFear, bool isInEncounter) { }
   public void OnRankChanged(FearBar.FearRank rank) { }
   ```

2. Put it on the manager's object.
3. In `FearEffectsManager`, add a field, `GetComponent` it in `Awake`, and call it from `Update` and `OnRankChanged`.

`FearIntrusiveEffects.OnRankChanged` and `FearPostProcessEffects.OnRankChanged` are already wired up and empty, ready for in-world scares and rank-based screen effects (e.g. chromatic aberration at Doomed).

If the effect is visual, scale it by a Graphics Settings multiplier so the player can turn it down.

---

## Gotchas

- **Effect components must be on the same object as the manager.**
- **Nothing on screen?** Check the profile has a Fear Vignette override and the renderer has the feature.
