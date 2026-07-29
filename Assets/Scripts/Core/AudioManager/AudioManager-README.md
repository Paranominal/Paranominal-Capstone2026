# Audio Manager v2 - Readme

A simple guide for getting the Audio Manager set up and playing sounds in a scene.

## What's in the System

- **`SoundDataSO.cs`** - A `ScriptableObject` representing one sound. Each sound in the project is its own asset.
- **`SoundDataSOEditor.cs`** - Editor-only helper. Adds preview controls and validation warnings to the SoundDataSO inspector.
- **`AudioManager.cs`** - The runtime singleton that plays sounds. Goes on a GameObject in your scene.
- **`SoundPlayer.cs`** - Drop-in component that plays a sound on demand. Use it on UI buttons, animation events, or anything else that fires a single sound.
- **`SoundOnTrigger.cs`** - Drop-in component that plays sounds when a collider enters or exits a trigger.

---

## Setting Up the Scene

1. Create an empty GameObject and name it `AudioManager`.
2. Add the **Audio Manager** component to it. An **Audio Source** component is added automatically.
3. Done - the manager is ready to use.

> The manager persists across scene loads automatically. Add it once to an early-loaded scene and it'll stick around.

---

## Creating a Sound

1. In the **Project** window, right-click > **Create > Audio > Sound Data**.
2. Name the asset descriptively (e.g. `UIClick`, `EnemyFootstep`, `ExplosionLarge`).
3. Select the asset and configure it in the inspector.

### Configuring a sound

The inspector is organised into sections. Here's what each does:

- **Clips** - Drag in one or more `AudioClip`s. Multiple clips will be picked from according to the selection mode.
- **Selection Mode** - How to pick between clips:
  - **Random** - Any clip, each play.
  - **Random No Repeat** - Random, but won't repeat the last one played. Great for footsteps and impacts.
  - **Sequential** - Cycles through clips in order.
- **Mixer** - Drag in an `AudioMixerGroup` for routing (SFX, UI, Music, etc.). Optional.
- **Volume** - Base playback volume (0–1).
- **Offset Range** - Optional random offset added to the volume per play, for natural variation. Leave at (0, 0) for fixed volume.
- **Pitch Range** - Random pitch sampled per play. Set both values equal for a fixed pitch.
- **Spatial Blend** - 0 is 2D (UI-style, non-positional). 1 is fully 3D (positional, attenuates with distance).
- **Min/Max Distance + Rolloff** - Only shown for 3D sounds. Controls how the sound falls off with distance.
- **Loop** - Whether the sound loops. Only takes effect when played through a custom AudioSource (see "Positional playback" below).
- **Min Interval** - Cooldown between plays in seconds. Useful for preventing rapid stacking on bursty sounds. Set to 0 to disable.

### Previewing a sound

At the top of the inspector are **Play Preview** and **Stop** buttons. Click Play to audition the selected clip.

> **Note:** Preview plays the clip at its natural volume and pitch - it does not reflect the volume or pitch settings in the inspector. Treat the preview as "is this the right clip?", not "is this the right mix?"

---

## Triggering Sounds - Three Routes

There are three routes for triggering a sound, depending on what's firing it. Pick the one that fits your situation.

### Pattern 1 - `SoundPlayer` component (no code needed)

Use when a GameObject doesn't already need a custom script - e.g. UI buttons, animation events, simple triggers. `SoundPlayer` is a drop-in component that holds one sound and exposes a `PlaySound()` method anything can call.

**Setup:**

1. Select the GameObject.
2. **Add Component > Audio > Sound Player**. An AudioSource is auto-added; the source field is auto-filled.
3. Drag the relevant `SoundDataSO` into the **Sound** slot.
4. Wire up the trigger:
   - **UI Button:** In the button's **OnClick()** event, drag the GameObject in and pick `SoundPlayer.PlaySound`.
   - **Animation Event:** In the Animation window, add an event on the frame you want, and pick `SoundPlayer.PlaySound` from the dropdown.
   - **From any UnityEvent:** Same idea - pick `SoundPlayer.PlaySound` from the method dropdown.

### Pattern 2 - `SoundOnTrigger` component (no code needed)

Use for trigger volumes - pickups, area effects, doorways, etc.

**Setup:**

1. Make sure the GameObject has a collider with **Is Trigger** ticked.
2. **Add Component > Audio > Sound On Trigger**. An AudioSource is auto-added.
3. Drag sound assets into **On Enter Sound** and/or **On Exit Sound**. Either can be left empty to disable that event.
4. Optionally type a tag in **Required Tag** to filter (e.g. `Player` to only fire when the player enters). Leave empty to fire for any collider.

Works for both 3D and 2D triggers automatically.

### Pattern 3 - Direct `SoundDataSO` fields on an existing script

Use when the GameObject already needs a custom script for its actual behaviour. Adding `SoundDataSO` fields directly to that script is cleaner than stacking on extra `SoundPlayer` components.

This is the pattern for things like a Door that's controlled by code, an enemy with multiple attack sounds, a character with footsteps, etc.

```csharp
public class Door : MonoBehaviour
{
    [Header("Sounds")]
    [SerializeField] private SoundDataSO openSound;
    [SerializeField] private SoundDataSO closeSound;
    [SerializeField] private AudioSource source;

    private void Reset()
    {
        source = GetComponent<AudioSource>();
    }

    public void Open()
    {
        // ...door swing logic...
        if (openSound != null) AudioManager.PlaySound(openSound, source);
    }

    public void Close()
    {
        // ...door swing logic...
        if (closeSound != null) AudioManager.PlaySound(closeSound, source);
    }
}
```

The designer's workflow: drag the two `SoundDataSO` assets into the slots on the door script. The `AudioSource` field auto-fills via `Reset()`.

---

## Which Pattern to Use

| Situation | Use |
|---|---|
| UI button click sound | `SoundPlayer` |
| Animation event sound (e.g. footstep on a frame) | `SoundPlayer` |
| Sound when something enters/exits a trigger zone | `SoundOnTrigger` |
| Sound triggered by an existing custom script | Direct `SoundDataSO` field on that script |
| Multiple sounds needed on one custom-script GameObject | Direct `SoundDataSO` fields (one per sound) |

The rule of thumb: **if the GameObject already needs a script for its real behaviour, use direct `SoundDataSO` fields on it. If it doesn't, use one of the drop-in components.**

---

## Playing Sounds From Code

If you're writing a script that calls `AudioManager` directly (Pattern 3 above), there are two overloads:

```csharp
// 2D / UI sound - plays through the manager's own AudioSource
AudioManager.PlaySound(soundRef);

// Positional / 3D sound - plays through a supplied AudioSource (usually on the same GameObject)
AudioManager.PlaySound(soundRef, mySource);
```

The single-argument version is fire-and-forget and can't loop or be stopped. The two-arg version applies the SoundDataSO's full spatial settings and supports looping, stopping, and other modifications via the supplied source.

---

## Quick Reference

| Task | Where |
|---|---|
| Add the manager to a scene | Empty GameObject + **Audio Manager** component |
| Create a new sound | **Project > Create > Audio > Sound Data** |
| Configure a sound | Select its asset in the Project window |
| Preview a sound | **Play Preview** button at the top of the sound's inspector |
| Add a sound to a UI button | `SoundPlayer` component + wire up via OnClick |
| Add a sound to an animation | `SoundPlayer` component + Animation Event |
| Add a sound to a trigger zone | `SoundOnTrigger` component |
| Add sounds to a custom-script GameObject | Direct `SoundDataSO` fields on the script |
| Play a 2D sound from code | `AudioManager.PlaySound(soundRef);` |
| Play a positional sound from code | `AudioManager.PlaySound(soundRef, mySource);` |
| Control category volumes (SFX, Music, UI) | Assign **Mixer** groups per sound + use Unity's `AudioMixer` |

---

## About Volume

There are two layers of volume in the system:

- **Per-sound volume + offset** - Set on the SoundDataSO. Baseline level for that sound, with optional variation.
- **Mixer group volume** - Controlled through Unity's AudioMixer. Use this for category-level player controls (Master, SFX, Music, UI sliders).

For player-facing volume sliders, route categories through `AudioMixerGroup`s and control those - don't try to adjust individual SoundDataSO volumes at runtime.