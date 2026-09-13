using System.Collections;
using UnityEngine;

public class GunVisuals : MonoBehaviour
{
    // Scene references used to drive feedback from a shot
    [Header("References")]
    [SerializeField] private Transform gunModel;
    [SerializeField] private SpriteRenderer ironMuzzleFlash;
    [SerializeField] private SpriteRenderer silverMuzzleFlash;
    [SerializeField] private Animator gunAnimator;
    [SerializeField] private Renderer[] gunPartRenderers;

    // Recoil and flash tuning values
    [Header("Weapon Model Recoil")]
    [SerializeField] private Vector3 gunKickOffset = new Vector3(0f, 0.02f, -0.08f);
    [SerializeField] private float gunKickUpRotationDegrees = 4f;
    [SerializeField] private float gunKickTime = 0.05f;
    [SerializeField] private float gunReturnTime = 0.1f;
    [SerializeField] private float muzzleFlashDuration = 0.05f;

    // Misfire animation and texture tuning values
    [Header("Misfire Visuals")]
    [SerializeField] private Material misfireMaterial;
    [SerializeField] private float misfiresTextureChangeDuration = 0.2f;

    // Rest pose cache for the gun model
    // Kick animation always returns to these values to prevent drift over repeated shots
    private Vector3 gunRestLocalPosition;
    private Quaternion gunRestLocalRotation;
    // Cache original materials for each renderer so they can be restored after texture changes
    private Material[] originalMaterials;

    // Coroutine handles are kept so ongoing effects can be interrupted and restarted cleanly
    // This avoids stacked coroutines fighting over visibility or transform state
    private Coroutine muzzleFlashRoutine;
    private Coroutine gunKickRoutine;
    private Coroutine textureChangeRoutine;

    private void Awake()
    {
        // Ensure flash sprites begin hidden so the gun does not spawn flashing
        if (ironMuzzleFlash != null) ironMuzzleFlash.enabled = false;
        if (silverMuzzleFlash != null) silverMuzzleFlash.enabled = false;

        // Auto-find gun model if not explicitly assigned
        if (gunModel == null)
        {
            gunModel = GetComponent<Transform>();
        }

        // Capture gun resting transform once at startup for recoil animation
        if (gunModel != null)
        {
            gunRestLocalPosition = gunModel.localPosition;
            gunRestLocalRotation = gunModel.localRotation;
        }

        // Cache the original materials from all gun part renderers so they can be restored after misfire texture changes
        if (gunPartRenderers != null && gunPartRenderers.Length > 0)
        {
            originalMaterials = new Material[gunPartRenderers.Length];
            for (int i = 0; i < gunPartRenderers.Length; i++)
            {
                if (gunPartRenderers[i] != null)
                {
                    originalMaterials[i] = gunPartRenderers[i].material;
                }
            }
        }

        // Find the animator if not explicitly assigned in inspector
        if (gunAnimator == null)
        {
            gunAnimator = GetComponent<Animator>();
        }
    }

    // entry point called by firing logic
    // triggers both effects together so shot feedback feels cohesive
    public void PlayShotVisuals(WeakPointType shotType)
    {
        PlayMuzzleFlash(shotType);
        PlayRecoil();
    }

    // Returns the muzzle flash duration
    // Used to delay misfire texture/animation until after the muzzle flash completes
    public float GetMuzzleFlashDuration()
    {
        return muzzleFlashDuration;
    }

    public float GetMisfireVisualsDuration()
    {
        return misfiresTextureChangeDuration;
    }

    // entry point called by misfire logic
    // triggers animator animation and texture change for misfire feedback
    public void PlayMisfireVisuals()
    {
        // Trigger the misfire animation in the animator
        if (gunAnimator != null)
        {
            gunAnimator.SetTrigger("misfire");
        }

        // Change the gun part textures to indicate a misfire occurred
        if (misfireMaterial != null && gunPartRenderers != null && gunPartRenderers.Length > 0)
        {
            // Interrupt any ongoing texture change to restart cleanly
            if (textureChangeRoutine != null)
                StopCoroutine(textureChangeRoutine);

            textureChangeRoutine = StartCoroutine(TextureChangeRoutine());
        }
    }

    // entry point called by reload logic
    // triggers animator animation for reload feedback
    public void PlayReloadAnimation()
    {
        if (gunAnimator != null)
        {
            gunAnimator.SetTrigger("reload");
        }
    }

    public void PlayMuzzleFlash(WeakPointType shotType)
    {
        // Restart flash if a rapid shot occurs before previous flash finished (should only happen if firing faster than flash duration (which shouldn't ever happen, but just for redundancy sake??))
        if (muzzleFlashRoutine != null)
            StopCoroutine(muzzleFlashRoutine);

        // reset both flashes first, then enable only the selected one
        if (ironMuzzleFlash != null) ironMuzzleFlash.enabled = false;
        if (silverMuzzleFlash != null) silverMuzzleFlash.enabled = false;

        // Iron vs Silver weak-point rounds can have distinct muzzle visual assets
        SpriteRenderer target = shotType == WeakPointType.Iron ? ironMuzzleFlash : silverMuzzleFlash;
        if (target == null) return;

        muzzleFlashRoutine = StartCoroutine(MuzzleFlashRoutine(target));
    }

    private IEnumerator MuzzleFlashRoutine(SpriteRenderer target)
    {
        // Show flash briefly (or however long is set in inspector)
        target.enabled = true;
        yield return new WaitForSeconds(muzzleFlashDuration);
        target.enabled = false;
    }

    private void PlayRecoil()
    {
        // If no model exists for whatever reason, exit safely
        if (gunModel == null)
            return;

        // Interrupt current kick to avoid overlapping animations during high fire rates
        if (gunKickRoutine != null)
            StopCoroutine(gunKickRoutine);

        gunKickRoutine = StartCoroutine(GunKickRoutine());
    }

    private IEnumerator GunKickRoutine()
    {
        if (gunModel == null)
            yield break;

        // Start from current pose so restarting mid-animation remains smooth
        Vector3 fromPos = gunModel.localPosition;
        Quaternion fromRot = gunModel.localRotation;

        // Compute target kicked pose from cached rest pose
        // Position pulls backward/up; rotation pitches up for recoil impact
        Vector3 kickedPos = gunRestLocalPosition + gunKickOffset;
        Quaternion kickedRot = gunRestLocalRotation * Quaternion.Euler(-gunKickUpRotationDegrees, 0f, 0f);

        // move quickly into kick pose.
        float t = 0f;
        while (t < gunKickTime)
        {
            t += Time.deltaTime;
            float alpha = gunKickTime <= 0f ? 1f : Mathf.Clamp01(t / gunKickTime);
            gunModel.localPosition = Vector3.Lerp(fromPos, kickedPos, alpha);
            gunModel.localRotation = Quaternion.Slerp(fromRot, kickedRot, alpha);
            yield return null;
        }

        // recover to rest pose, usually slightly slower for natural recoil feel
        t = 0f;
        while (t < gunReturnTime)
        {
            t += Time.deltaTime;
            float alpha = gunReturnTime <= 0f ? 1f : Mathf.Clamp01(t / gunReturnTime);
            gunModel.localPosition = Vector3.Lerp(kickedPos, gunRestLocalPosition, alpha);
            gunModel.localRotation = Quaternion.Slerp(kickedRot, gunRestLocalRotation, alpha);
            yield return null;
        }

        // Hard-set final pose to eliminate tiny floating-point interpolation issues (such as not fully returning to rest pose after many shots)
        gunModel.localPosition = gunRestLocalPosition;
        gunModel.localRotation = gunRestLocalRotation;
    }

    private IEnumerator TextureChangeRoutine()
    {
        if (gunPartRenderers == null || gunPartRenderers.Length == 0)
            yield break;

        // Swap each gun part to the misfire material counterpart to visually indicate the weapon misfired
        for (int i = 0; i < gunPartRenderers.Length; i++)
        {
            if (gunPartRenderers[i] != null)
            {
                gunPartRenderers[i].material = misfireMaterial;
            }
        }

        // Keep the misfire texture visible for the configured duration
        yield return new WaitForSeconds(misfiresTextureChangeDuration);

        // Restore the original materials for each part after the misfire effect concludes
        for (int i = 0; i < gunPartRenderers.Length; i++)
        {
            if (gunPartRenderers[i] != null && originalMaterials != null && i < originalMaterials.Length && originalMaterials[i] != null)
            {
                gunPartRenderers[i].material = originalMaterials[i];
            }
        }
    }

    public void SetVisualsVisible(bool visible)
    {
        if (!visible)
        {
            if (muzzleFlashRoutine != null)
            {
                StopCoroutine(muzzleFlashRoutine);
                muzzleFlashRoutine = null;
            }

            if (gunKickRoutine != null)
            {
                StopCoroutine(gunKickRoutine);
                gunKickRoutine = null;
            }

            // Stop any ongoing texture changes and restore original materials
            if (textureChangeRoutine != null)
            {
                StopCoroutine(textureChangeRoutine);
                textureChangeRoutine = null;
            }

            if (ironMuzzleFlash != null) ironMuzzleFlash.enabled = false;
            if (silverMuzzleFlash != null) silverMuzzleFlash.enabled = false;

            // Ensure the original materials are restored when visuals are disabled
            if (gunPartRenderers != null)
            {
                for (int i = 0; i < gunPartRenderers.Length; i++)
                {
                    if (gunPartRenderers[i] != null && originalMaterials != null && i < originalMaterials.Length && originalMaterials[i] != null)
                    {
                        gunPartRenderers[i].material = originalMaterials[i];
                    }
                }
            }
        }

        if (gunModel != null)
        {
            if (visible)
            {
                gunModel.localPosition = gunRestLocalPosition;
                gunModel.localRotation = gunRestLocalRotation;
            }

            gunModel.gameObject.SetActive(visible);
        }
    }
}
