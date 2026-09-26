using UnityEngine;

public class PlayerAimAssist : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInputReader inputReader;
    [SerializeField] private Camera playerCamera;

    [Header("FOV Settings")]
    [SerializeField] private float aimAssistRadius = 0.2f;

    [Header("Strength Settings")]
    [SerializeField] private float aimAssistStrength = 3f;

    [Header("Override Settings")]
    [SerializeField] private float overrideStrength = 0.5f;

    private void Awake()
    {
        if (inputReader == null) inputReader = GetComponent<PlayerInputReader>();

        if (playerCamera == null) playerCamera = Camera.main;
    }

    public Vector2 AssistDelta()
    {
        if (inputReader == null || playerCamera == null || !inputReader.IsUsingGamepad) return Vector2.zero;
        
        if (inputReader.GamepadLookMagnitude > overrideStrength) return Vector2.zero;

        WeakPoint target = ClosestWeakPoint();
        
        if (target == null) return Vector2.zero;
        Vector3 toTarget = target.GetWorldCenter() - playerCamera.transform.position;
        if (toTarget.sqrMagnitude < 0.01f) return Vector2.zero;

        Quaternion currentRotation = playerCamera.transform.rotation;
        Quaternion targetRotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
        Quaternion slerp = Quaternion.Slerp(currentRotation, targetRotation, aimAssistStrength * Time.deltaTime);
        Quaternion deltaRotation = Quaternion.Inverse(currentRotation) * slerp;

        Vector3 deltaEuler = deltaRotation.eulerAngles;
        return new Vector2(Mathf.DeltaAngle(0f, deltaEuler.y), Mathf.DeltaAngle(0f, deltaEuler.x));
    }

    private WeakPoint ClosestWeakPoint()
    {
        WeakPoint best = null;
        float bestDistance = aimAssistRadius;

        foreach (WeakPoint weakPoint in WeakPointRegistry.All)
        {
            if (weakPoint == null || !weakPoint.IsShown || weakPoint.hasBeenHit) continue;

            Vector3 viewport = playerCamera.WorldToViewportPoint(weakPoint.GetWorldCenter());
            if (viewport.z <= 0f) continue;

            float dx = (viewport.x - 0.5f) * playerCamera.aspect;
            float dy = (viewport.y - 0.5f);
            float distance = Mathf.Sqrt(dx * dx + dy * dy);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = weakPoint;
            }
        }

        return best;
    }
}