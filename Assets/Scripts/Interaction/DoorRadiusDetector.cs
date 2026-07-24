using UnityEngine;

// Summary: Placed on a door GameObject alongside a sphere collider.
// Notifies the encounter manager when the player exits the door's radius,
// satisfying the distance condition needed to start a door-gated encounter.
[RequireComponent(typeof(SphereCollider))]
public class DoorRadiusDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyEncounterManager encounterManager;
    [SerializeField] private Door door;

    // Attempts to find references on the parent if not manually assigned.
    private void Awake()
    {
        if (encounterManager == null)
        {
            encounterManager = GetComponentInParent<EnemyEncounterManager>();
        }

        if (door == null)
        {
            door = GetComponentInParent<Door>();
        }
    }

    // Notifies the encounter manager when the player exits this door's radius.
    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        if (encounterManager != null)
        {
            encounterManager.NotifyDoorRadiusExited();
        }
    }

    // Draws the sphere collider radius in the Scene view for designer reference.
    private void OnDrawGizmosSelected()
    {
        SphereCollider sphereCollider = GetComponent<SphereCollider>();

        if (sphereCollider == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sphereCollider.radius);
    }
}
