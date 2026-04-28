using UnityEngine;

public class WeakPointUnlockTester : MonoBehaviour
{
    [SerializeField] private string weakPointId;
    [SerializeField] private KeyCode unlockKey = KeyCode.L;

    private void Update()
    {
        if (!Input.GetKeyDown(unlockKey))
            return;

        string requestedId = weakPointId == null ? string.Empty : weakPointId.Trim();
        if (string.IsNullOrEmpty(requestedId))
        {
            Debug.LogWarning("WeakPointUnlockTester needs a weakPointId.");
            return;
        }

        if (!WeakPointRegistry.TryGetWeakPointById(requestedId, out WeakPoint weakPoint))
        {
            Debug.LogWarning($"No weakpoint found with ID '{requestedId}'.");
            return;
        }

        if (!weakPoint.IsWarded)
        {
            Debug.LogWarning($"Weakpoint '{requestedId}' exists but is not warded.");
            return;
        }

        weakPoint.UnlockWeakPoint();

        Debug.Log($"Unlocked warded weakpoint '{requestedId}'.");
    }
}
