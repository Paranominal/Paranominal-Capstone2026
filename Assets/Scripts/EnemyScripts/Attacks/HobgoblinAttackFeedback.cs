using System.Collections;
using UnityEngine;

/// Summary:
/// Visual feedback for the Hobgoblin's melee attack. Subscribes to MeleeAttack
/// events and drives three layered effects:
///   - Claw rear-back during windup, snap-forward at strike
///   - Material swap on the hitbox to signal "live now" during the strike
///   - TrailRenderer enabled during the strike for a swing arc
///
/// All effects are independent and optional - leave any reference null to skip
/// that layer. The component cleans itself up if the attack is cancelled mid-swing.

[RequireComponent(typeof(MeleeAttack))]
public class HobgoblinAttackFeedback : MonoBehaviour
{
    [Header("Rear-Back")]
    [Tooltip("Transform that lerps backward during windup, then snaps forward at strike. " +
             "Typically a 'ClawPivot' parent of the visible claw mesh.")]
    [SerializeField] private Transform clawPivot;
    [Tooltip("Local offset from the rest position at the peak of the rear-back.")]
    [SerializeField] private Vector3 rearBackOffset = new Vector3(0f, 0f, -0.4f);

    [Header("Hitbox Material Swap")]
    [Tooltip("Renderer on the hitbox child whose material is swapped during the strike.")]
    [SerializeField] private Renderer hitboxRenderer;
    [Tooltip("Material applied during windup and recovery (typically dim/dormant).")]
    [SerializeField] private Material idleMaterial;
    [Tooltip("Material applied during the strike (typically bright/charged).")]
    [SerializeField] private Material strikeMaterial;

    [Header("Trail")]
    [Tooltip("TrailRenderer enabled during the strike. Disable Auto Destruct on the trail itself.")]
    [SerializeField] private TrailRenderer swingTrail;

    private MeleeAttack meleeAttack;
    private Vector3 clawRestPosition;
    private Coroutine rearBackRoutine;

    private void Awake()
    {
        meleeAttack = GetComponent<MeleeAttack>();

        if (clawPivot != null) clawRestPosition = clawPivot.localPosition;
        if (swingTrail != null) swingTrail.emitting = false;
        if (hitboxRenderer != null && idleMaterial != null) hitboxRenderer.sharedMaterial = idleMaterial;
    }

    private void OnEnable()
    {
        meleeAttack.OnWindupStart += HandleWindupStart;
        meleeAttack.OnStrikeStart += HandleStrikeStart;
        meleeAttack.OnStrikeEnd += HandleStrikeEnd;
        meleeAttack.OnAttackCancelled += HandleAttackCancelled;
    }

    private void OnDisable()
    {
        meleeAttack.OnWindupStart -= HandleWindupStart;
        meleeAttack.OnStrikeStart -= HandleStrikeStart;
        meleeAttack.OnStrikeEnd -= HandleStrikeEnd;
        meleeAttack.OnAttackCancelled -= HandleAttackCancelled;
    }

    private void HandleWindupStart()
    {
        //start the rear-back lerp; this runs for the full windup duration so the
        //claw smoothly reaches peak rear-back right as the strike begins
        if (clawPivot != null)
        {
            if (rearBackRoutine != null) StopCoroutine(rearBackRoutine);
            rearBackRoutine = StartCoroutine(RearBackRoutine(meleeAttack.WindupDuration));
        }
    }

    private void HandleStrikeStart()
    {
        //snap to forward (rest) immediately - the swing follows-through from rear-back to rest
        if (clawPivot != null)
        {
            if (rearBackRoutine != null) StopCoroutine(rearBackRoutine);
            clawPivot.localPosition = clawRestPosition;
        }

        //swap to the live/strike material
        if (hitboxRenderer != null && strikeMaterial != null)
        {
            hitboxRenderer.sharedMaterial = strikeMaterial;
        }

        //start the trail
        if (swingTrail != null)
        {
            swingTrail.Clear();
            swingTrail.emitting = true;
        }
    }

    private void HandleStrikeEnd()
    {
        //back to dormant
        if (hitboxRenderer != null && idleMaterial != null)
        {
            hitboxRenderer.sharedMaterial = idleMaterial;
        }

        if (swingTrail != null)
        {
            swingTrail.emitting = false;
        }
    }

    private void HandleAttackCancelled()
    {
        //if we were mid-windup, snap back to rest so the claw doesn't get stuck back
        if (clawPivot != null)
        {
            if (rearBackRoutine != null) StopCoroutine(rearBackRoutine);
            clawPivot.localPosition = clawRestPosition;
        }

        if (hitboxRenderer != null && idleMaterial != null)
        {
            hitboxRenderer.sharedMaterial = idleMaterial;
        }

        if (swingTrail != null)
        {
            swingTrail.emitting = false;
        }
    }

    private IEnumerator RearBackRoutine(float duration)
    {
        Vector3 startPos = clawRestPosition;
        Vector3 peakPos = clawRestPosition + rearBackOffset;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            //ease-out feels more natural than linear: fast at first, slowing as it approaches peak.
            //matches the "coiling up" of a real swing.
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - (1f - t) * (1f - t);
            clawPivot.localPosition = Vector3.Lerp(startPos, peakPos, eased);

            elapsed += Time.deltaTime;
            yield return null;
        }

        clawPivot.localPosition = peakPos;
        rearBackRoutine = null;
    }
}
