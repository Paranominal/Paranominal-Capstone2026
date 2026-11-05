using Unity.VisualScripting;
using UnityEngine;
using Unity.Mathematics;

public class DrudeWall : MonoBehaviour
{
    [SerializeField] private EnemySpawnPoint drudeSpawner;
    [SerializeField] private bool3 follow = new bool3(true, false, false);
    private Enemy drude;
    private bool active = false;
    private Vector3 origin;

    void Awake()
    {
        origin = transform.position;
        drudeSpawner.EnemySpawned += AttachDrude;
    }

    void AttachDrude(Enemy target)
    {
        drude = target;
        active = true;
        drudeSpawner.EnemySpawned -= AttachDrude;
    }

    void Update()
    {
        if (!active && drude == null) return;
        else if (active && drude == null)
        {
            active = false;
            gameObject.SetActive(false);
        }
        else FollowDrude();
    }

    void FollowDrude()
    {
        if (drude != null)
        {
            float targetX = origin.x;
            if (follow.x) targetX = drude.transform.position.x;
            float targetY = origin.y; 
            if (follow.y) targetY = drude.transform.position.y;
            float targetZ = origin.z; 
            if (follow.z) targetZ = drude.transform.position.z;
            Vector3 targetPos = new Vector3(targetX, targetY, targetZ);
            transform.position = targetPos;
        }
    }
}
