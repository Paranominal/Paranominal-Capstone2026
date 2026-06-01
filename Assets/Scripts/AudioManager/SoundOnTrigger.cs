using UnityEngine;

// Summary: Plays sounds when a Collider enters or exits this GameObject's trigger collider. Each event has its own sound slot; either can be left empty to disable.
// Optional tag filter restricts firing to colliders with a specific tag.
[AddComponentMenu("Audio/Sound On Trigger")]
[RequireComponent(typeof(AudioSource))]
public class SoundOnTrigger : MonoBehaviour
{
    [SerializeField] private SoundDataSO onEnterSound;
    [SerializeField] private SoundDataSO onExitSound;
    [SerializeField] private AudioSource source;

    // Tag filter — leave empty to fire for any collider.
    [SerializeField] private string requiredTag = "";

    // Summary: Pre-fills the source field with the AudioSource on this GameObject.
    private void Reset()
    {
        source = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!PassesTagFilter(other)) return;
        if (onEnterSound == null) return;
        AudioManager.PlaySound(onEnterSound, source);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!PassesTagFilter(other)) return;
        if (onExitSound == null) return;
        AudioManager.PlaySound(onExitSound, source);
    }

    // 2D collider equivalents — Unity dispatches to whichever pair matches
    // the colliders involved, so supporting both costs nothing.
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!PassesTagFilter(other)) return;
        if (onEnterSound == null) return;
        AudioManager.PlaySound(onEnterSound, source);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!PassesTagFilter(other)) return;
        if (onExitSound == null) return;
        AudioManager.PlaySound(onExitSound, source);
    }

    // Returns true if the other collider matches the tag filter (or if no filter is set).
    private bool PassesTagFilter(Component other)
    {
        if (string.IsNullOrEmpty(requiredTag)) return true;
        return other.CompareTag(requiredTag);
    }
}
