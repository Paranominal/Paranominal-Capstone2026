using System;
using System.Collections;
using UnityEngine;

// Summary: Handles the lower/raise animation for weapon switching.
// Lerps the weapon parent transform down out of view and back up.
// Used by WeaponManager for all weapon transitions and the quick shove sequence.
public class WeaponSwitchAnimator : MonoBehaviour
{
    [Header("Animation")]
    [Tooltip("How far the weapon drops below its rest position.")]
    [SerializeField] private Vector3 lowerOffset = new Vector3(0f, -0.5f, 0f);

    [Tooltip("How long the lower/raise animation takes in seconds.")]
    [SerializeField] private float transitionDuration = 0.15f;

    private Vector3 restLocalPosition;
    private Coroutine activeRoutine;

    private void Awake()
    {
        restLocalPosition = transform.localPosition;
    }

    // Summary: Lerp down to the lowered position, then fire the callback.
    public void Lower(Action onComplete = null)
    {
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(LerpRoutine(restLocalPosition + lowerOffset, onComplete));
    }

    // Summary: Lerp back up to the rest position, then fire the callback.
    public void Raise(Action onComplete = null)
    {
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(LerpRoutine(restLocalPosition, onComplete));
    }

    // Summary: Snap to lowered position immediately, no animation.
    public void SnapToLowered()
    {
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        transform.localPosition = restLocalPosition + lowerOffset;
    }

    // Summary: Snap to rest position immediately, no animation.
    public void SnapToRest()
    {
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        transform.localPosition = restLocalPosition;
    }

    private IEnumerator LerpRoutine(Vector3 target, Action onComplete)
    {
        Vector3 start = transform.localPosition;
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            // Ease-out for snappy feel.
            float eased = 1f - (1f - t) * (1f - t);
            transform.localPosition = Vector3.Lerp(start, target, eased);
            yield return null;
        }

        transform.localPosition = target;
        activeRoutine = null;
        onComplete?.Invoke();
    }
}
