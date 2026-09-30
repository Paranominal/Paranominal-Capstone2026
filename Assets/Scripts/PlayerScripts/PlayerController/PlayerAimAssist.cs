using UnityEngine;

public class PlayerAimAssist : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInputReader inputReader;
    [SerializeField] private Camera playerCamera;

    [Header("FOV Settings")]
    //distance from crosshair to a weakpoint before activation
    [SerializeField] private float aimAssistRadius = 0.2f;
    [SerializeField] private float enemyAssistRadius = 0.3f;

    [Header("Enemy Targeting")]
    //targetting mask and range of enemies for aim assist
    [SerializeField] private LayerMask enemyMask;
    [SerializeField] private LayerMask obstructionMask;
    [SerializeField] private float enemyRange = 30f;

    [Header("Strength Settings")]
    //strength of aim assist
    [SerializeField] private float aimAssistStrength = 10f;
    //strenth of the pull at the edge of a weak point
    [SerializeField] private float edgeStrength = 0.3f;

    [Header("Override Settings")]
    //stick override value in which aim assist lets go
    [SerializeField] private float overrideStrength = 0.7f;

    [Header("Settle")]
    //time in seconds to keep aim assist active after stick is released
    [SerializeField] private float settleTime = 0.2f;
    private float lastStickTime = -999f;

    [Header("Smoothing")]
    //smoothing
    [SerializeField] private float assistSmoothTime = 0.05f;
    private float engagement;
    private Component engagedTarget;

    [Header("Stickiness")]
    //how close the new target has to be to steal from the current target
    [SerializeField] private float stealMargin = 0.85f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs;
    private Component lastLoggedTarget;

    //current targer, only one is active at a time
    private WeakPoint currentWeakPoint;
    private Collider currentEnemy;
    //buffer enemy overlap
    private readonly Collider[] enemyBuffer = new Collider[32];
    private Vector3 lastTargetPoint;

    private bool HasTarget => currentWeakPoint != null || currentEnemy != null;

    private void Awake()
    {
        if (inputReader == null) inputReader = GetComponent<PlayerInputReader>();

        if (playerCamera == null) playerCamera = Camera.main;

        if (enemyMask.value == 0) enemyMask = LayerMask.GetMask("Enemy");
        if (obstructionMask.value == 0) obstructionMask = LayerMask.GetMask("Default", "Environment", "Ground");
    }

    //called by PlayerLook to return x and y (tells how much to rotate)
    //pull is recalced every frame to prevent overshooting
    public Vector2 AssistDelta()
    {
        Vector2 rawDelta = ComputeRawAssistDelta();

        Component target = currentWeakPoint != null ? currentWeakPoint : currentEnemy;
        if (target != engagedTarget)
        {
            engagedTarget = target;
            engagement = 0f;
        }

        //starting strength is eased in
        float smoothFactor = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.0001f, assistSmoothTime));
        engagement = Mathf.Lerp(engagement, target != null ? 1f : 0f, smoothFactor);
        return rawDelta * engagement;
    }

    //calcs raw data before ramping
    private Vector2 ComputeRawAssistDelta()
    {
        //aim assist is only active if on controller
        if (inputReader == null || playerCamera == null || !inputReader.IsGamepadActive)
        {
            ClearTarget();
            return Vector2.zero;
        }

        //stick flick overrides the aim assist
        if (inputReader.GamepadLookMagnitude > overrideStrength)
        {
            ClearTarget();
            return Vector2.zero;
        }

        //aim assist is only active if the player is nudging the stick
        bool nudging = inputReader.IsUsingGamepad;
        if (nudging) lastStickTime = Time.time;
        bool settling = !nudging && HasTarget && Time.time - lastStickTime <= settleTime;

        if (!nudging && !settling)
        {
            ClearTarget();
            return Vector2.zero;
        }

        if (!SelectTarget(out Vector3 targetPoint, out float distance, out float radius)) return Vector2.zero;
        lastTargetPoint = targetPoint;

        Vector3 toTarget = targetPoint - playerCamera.transform.position;
        if (toTarget.sqrMagnitude < 0.01f) return Vector2.zero;

        //full strength aim assist if aim is closer to the center
        float falloff = Mathf.Lerp(1f, edgeStrength, distance / radius);

        //rotate a fraction of hte remainign angle towards the target
        Quaternion currentRotation = playerCamera.transform.rotation;
        Quaternion targetRotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
        Quaternion slerp = Quaternion.Slerp(currentRotation, targetRotation, aimAssistStrength * falloff * Time.deltaTime);
        Quaternion deltaRotation = Quaternion.Inverse(currentRotation) * slerp;

        Vector3 deltaEuler = deltaRotation.eulerAngles;
        return new Vector2(Mathf.DeltaAngle(0f, deltaEuler.y), Mathf.DeltaAngle(0f, deltaEuler.x));
    }

    //weakpoints take priority over enemies
    private bool SelectTarget(out Vector3 point, out float distance, out float radius)
    {
        WeakPoint weakPoint = PickWeakPoint(out float weakPointDistance);
        if (weakPoint != null)
        {
            LogTarget(weakPoint, "weak point");
            currentEnemy = null;
            point = weakPoint.GetWorldCenter();
            distance = weakPointDistance;
            radius = aimAssistRadius;
            return true;
        }

        Collider enemy = PickEnemy(out float enemyDistance);
        if (enemy != null)
        {
            LogTarget(enemy, "enemy");
            point = enemy.bounds.center;
            distance = enemyDistance;
            radius = enemyAssistRadius;
            return true;
        }

        point = Vector3.zero;
        distance = 0f;
        radius = 1f;
        return false;
    }

    private WeakPoint PickWeakPoint(out float distance)
    {
        float currentDistance = 0f;

        //keeps current weakpoint unless invalid (ie out of range, blocked, or not the closest)
        bool currentValid = currentWeakPoint != null
            && IsCandidate(currentWeakPoint)
            && TryGetScreenDistance(currentWeakPoint.GetWorldCenter(), out currentDistance)
            && currentDistance <= aimAssistRadius
            && HasLineOfSight(currentWeakPoint.GetWorldCenter());
        if (!currentValid) currentWeakPoint = null;

        WeakPoint closest = ClosestWeakPoint(out float closestDistance);

        if (currentValid && (closest == null || closestDistance >= currentDistance * stealMargin))
        {
            distance = currentDistance;
            return currentWeakPoint;
        }

        currentWeakPoint = closest;
        distance = closestDistance;
        return currentWeakPoint;
    }

    private Collider PickEnemy(out float distance)
    {
        float currentDistance = 0f;

        //keeps current enemy unless invalid
        bool currentValid = IsEnemyCandidate(currentEnemy)
            && TryGetScreenDistance(currentEnemy.bounds.center, out currentDistance)
            && currentDistance <= enemyAssistRadius
            && HasLineOfSight(currentEnemy.bounds.center);
        if (!currentValid) currentEnemy = null;

        //apply the same steal margin logic as weakpoints
        Collider closest = ClosestEnemy(out float closestDistance);

        if (currentValid && (closest == null || closestDistance >= currentDistance * stealMargin))
        {
            distance = currentDistance;
            return currentEnemy;
        }

        currentEnemy = closest;
        distance = closestDistance;
        return currentEnemy;
    }

    //closest weakpoint to center of screen
    private WeakPoint ClosestWeakPoint(out float bestDistance)
    {
        WeakPoint best = null;
        bestDistance = aimAssistRadius;

        foreach (WeakPoint weakPoint in WeakPointRegistry.All)
        {
            if (!IsCandidate(weakPoint)) continue;
            if (!TryGetScreenDistance(weakPoint.GetWorldCenter(), out float distance)) continue;
            if (distance >= bestDistance) continue;
            if (!HasLineOfSight(weakPoint.GetWorldCenter())) continue;

            bestDistance = distance;
            best = weakPoint;
        }

        return best;
    }

    private Collider ClosestEnemy(out float bestDistance)
    {
        Collider best = null;
        bestDistance = enemyAssistRadius;

        //get all enemies in range, then pick closest
        int count = Physics.OverlapSphereNonAlloc(playerCamera.transform.position, enemyRange, enemyBuffer, enemyMask, QueryTriggerInteraction.Collide);
        for (int i = 0; i < count; i++)
        {
            Collider enemy = enemyBuffer[i];
            if (!IsEnemyCandidate(enemy)) continue;
            if (!TryGetScreenDistance(enemy.bounds.center, out float distance)) continue;
            if (distance >= bestDistance) continue;
            if (!HasLineOfSight(enemy.bounds.center)) continue;

            bestDistance = distance;
            best = enemy;
        }

        return best;
    }

    //checks if a weakpoint is valid for aim assist
    private bool IsCandidate(WeakPoint weakPoint)
    {
        return weakPoint != null && weakPoint.isActiveAndEnabled && weakPoint.IsShown;
    }

    //same as above, but for enemies instead
    private bool IsEnemyCandidate(Collider enemy)
    {
        if (enemy == null || !enemy.enabled || !enemy.gameObject.activeInHierarchy) return false;
        if ((enemyMask.value & (1 << enemy.gameObject.layer)) == 0) return false;
        return (enemy.bounds.center - playerCamera.transform.position).sqrMagnitude <= enemyRange * enemyRange;
    }

    private bool TryGetScreenDistance(Vector3 worldPoint, out float distance)
    {
        distance = 0f;

        //check if the point is in front of the camera
        Vector3 viewport = playerCamera.WorldToViewportPoint(worldPoint);
        if (viewport.z <= 0f) return false;

        //calc distance from center of screen
        float dx = (viewport.x - 0.5f) * playerCamera.aspect;
        float dy = (viewport.y - 0.5f);
        distance = Mathf.Sqrt(dx * dx + dy * dy);
        return true;
    }

    private bool HasLineOfSight(Vector3 point)
    {
        //check if the point is blocked by an obstruction
        return !Physics.Linecast(playerCamera.transform.position, point, obstructionMask, QueryTriggerInteraction.Ignore);
    }

    private void LogTarget(Component target, string kind)
    {
        if (!debugLogs || target == lastLoggedTarget) return;
        lastLoggedTarget = target;
        Debug.Log($"[AimAssist] Locked {kind}: {target.name} (layer: {LayerMask.LayerToName(target.gameObject.layer)}, parent: {(target.transform.parent != null ? target.transform.parent.name : "none")})", target);
    }

    //clears the current target if not meeting certain criteria
    private void ClearTarget()
    {
        currentWeakPoint = null;
        currentEnemy = null;
    }
}