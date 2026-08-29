using UnityEngine;

public class RoomEntryDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyEncounterManager encounterManager;

    public int enemiesinside;
    private bool isFirstFrame = true;

    // Automatically finds the parent encounter manager when the object is first loaded.
    private void Awake()
    {
        if (encounterManager == null)
        {
            encounterManager = GetComponentInParent<EnemyEncounterManager>();
        }
    }

    // Detects the player entering the room trigger and notifies the encounter manager.
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        if (encounterManager != null)
        {
            encounterManager.SetPlayerInRoom(true);
            Debug.Log($"{other.name} has entered the room!");
        }

        
    }

    // Detects the player leaving the room trigger and notifies the encounter manager.
    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        if (encounterManager != null)
        {
            encounterManager.SetPlayerInRoom(false);
            Debug.Log($"{other.name} has exited the room!");
        }

        if (other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            Debug.Log("die");
        }

        
    }

    void FixedUpdate()
    {
        if (isFirstFrame == false)
        {
            Debug.Log(enemiesinside + name);

            //destroy door here
        }
        
        // This ensures we only run our check during the first physics cycle
        if (Time.timeSinceLevelLoad > 0)
        {
            isFirstFrame = false;
        }

        

        enemiesinside = 0;

        
    }

    void OnTriggerStay(Collider other)
    {
        

        if (other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            enemiesinside++;
        }

    }
    



    private void Update()
    {
        
    }
}