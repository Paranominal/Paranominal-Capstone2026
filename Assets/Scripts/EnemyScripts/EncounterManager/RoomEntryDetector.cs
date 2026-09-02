using System;
using System.Threading;
using UnityEngine;

public class RoomEntryDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyEncounterManager encounterManager;

    public int enemiesinside =1;
    private bool isFirstFrame = true;

    public GameObject door;

    private bool playerinside;
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

            playerinside = true;
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

            playerinside = false;
        }

        if (other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            Debug.Log("die");
        }

        
    }

    private float buffertimer;


    

    void FixedUpdate()
    {
        if (isFirstFrame == false)
        {
            Debug.Log(enemiesinside + name);

            //destroy door here

            if (playerinside && enemiesinside == 0 && door != null)
            {
                Destroy(door);
            }
        }
        
        // This ensures we only run our check during the first physics cycle
        if ( buffertimer > 0.1f)
        {
            isFirstFrame = false;
        }

        if (playerinside)
        {
            enemiesinside = 0;
        }
        

        
    }

    void OnTriggerStay(Collider other)
    {
        

        if (other.gameObject.layer == LayerMask.NameToLayer("Enemy") && playerinside)
        {
            enemiesinside++;
        }

    }
    



    private void Update()
    {
        if (playerinside)
        {
            buffertimer += Time.deltaTime;
        }
        
    }
}