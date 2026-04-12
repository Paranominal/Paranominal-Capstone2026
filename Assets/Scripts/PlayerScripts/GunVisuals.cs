using System.Collections;
using UnityEngine;

public class GunVisuals : MonoBehaviour
{
    // Scene references used to drive feedback from a shot
    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Transform gunModel;
    [SerializeField] private SpriteRenderer ironMuzzleFlash;
    [SerializeField] private SpriteRenderer silverMuzzleFlash;

    // Recoil and flash tuning values
    [Header("Visual Recoil")]
    [SerializeField] private float cameraRecoilUpDegrees = 1.5f;
    [SerializeField] private Vector3 gunKickOffset = new Vector3(0f, 0.02f, -0.08f);
    [SerializeField] private float gunKickUpRotationDegrees = 4f;
    [SerializeField] private float gunKickTime = 0.05f;
    [SerializeField] private float gunReturnTime = 0.1f;
    [SerializeField] private float muzzleFlashDuration = 0.05f;

    // Rest pose cache for the gun model
    // Kick animation always returns to these values to prevent drift over repeated shots
    private Vector3 gunRestLocalPosition;
    private Quaternion gunRestLocalRotation;

    // Coroutine handles are kept so ongoing effects can be interrupted and restarted cleanly
    // This avoids stacked coroutines fighting over visibility or transform state
    private Coroutine muzzleFlashRoutine;
    private Coroutine gunKickRoutine;

    private void Awake()
    {
        // Auto-set movement if not assigned manually
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        // Ensure flash sprites begin hidden so the gun does not spawn flashing
        if (ironMuzzleFlash != null) ironMuzzleFlash.enabled = false;
        if (silverMuzzleFlash != null) silverMuzzleFlash.enabled = false;

        // Capture gun resting transform once at startup for recoil animation
        if (gunModel != null)
        {
            gunRestLocalPosition = gunModel.localPosition;
            gunRestLocalRotation = gunModel.localRotation;
        }
    }

    // entry point called by firing logic
    // triggers both effects together so shot feedback feels cohesive
    public void PlayShotVisuals(WeakPointType shotType)
    {
        PlayMuzzleFlash(shotType);
        PlayRecoil();
    }

    private void PlayMuzzleFlash(WeakPointType shotType)
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
        // Camera recoil is delegated to movement/camera system so visuals and camera stay coordinated
        if (playerMovement != null)
            playerMovement.AddVerticalRecoil(cameraRecoilUpDegrees);

        // If no model exists for whatever reason, still allow camera recoil and exit safely
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
}
