using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BulletSpray : MonoBehaviour
{
    [SerializeField] private Camera playerCam;
    [SerializeField] private Transform origin;
    [SerializeField] private InputActionReference shoot;
    [SerializeField] private GameObject bullet;
    [SerializeField] private float bulletSpeed = 10;
    public float speedVariability = 0;
    [SerializeField] private int maxBulletCount = 100;
    public float angleInaccuracy = 0;
    public float width = 0;
    [SerializeField] private bool randomiseSpin;
    public List<GameObject> bullets; //make this public to bug test, make private after

    [SerializeField] private BulletSprayFX fx;
    public bool debugMode;

    void Update()
    {
        if (shoot.action.IsPressed()) Shoot();
        if (fx != null) fx.DoFX();        
    }
    Quaternion AimDir()
    {
        Quaternion aimDir = Quaternion.LookRotation(transform.forward + Inaccuracy());
        if (randomiseSpin) aimDir.eulerAngles = aimDir.eulerAngles + RandomSpin();
        if (debugMode) Debug.Log($"{this} | AimDir():{aimDir}");
        return aimDir;
    }
    Vector3 AimPos()
    {        
        Vector3 aimPos = origin.position + OriginWidth();
        if (debugMode) Debug.Log($"{this} | AimPos():{aimPos}");
        return aimPos;
    }
    void Shoot()
    {
        bullets.Add(InstanceBullet());
        foreach (GameObject x in bullets)
        {
            x.GetComponent<Bullet>().speed = bulletSpeed + Random.Range(-speedVariability, speedVariability);
        }
        if (bullets.Count > maxBulletCount)
        {
            Destroy(bullets[0]);
            bullets.RemoveAt(0);
        }
    }
    GameObject InstanceBullet()
    {
        return Instantiate(bullet, AimPos(), AimDir());
    }
    Vector3 Inaccuracy()
    {
        Vector3 inaccuracy = new Vector3 (Random.Range(-angleInaccuracy, angleInaccuracy), Random.Range(-angleInaccuracy, angleInaccuracy), Random.Range(-angleInaccuracy, angleInaccuracy)) * 0.01f;
        if (debugMode) Debug.Log($"{this} | Innaccuracy():{inaccuracy}");
        return inaccuracy;
    }
    Vector3 RandomSpin()
    {
        Vector3 randomSpin = new Vector3(0, 0, Random.Range(0, 360));
        if (debugMode) Debug.Log($"{this} | RandomSpin():{randomSpin}");
        return randomSpin;
    }
    Vector3 OriginWidth()
    {
        Vector3 originWidth = new Vector3 (Random.Range(-width, width), Random.Range(-width, width), Random.Range(-width, width)) * 0.01f;
        if (debugMode) Debug.Log($"{this} | OriginWidth():{originWidth}");
        return originWidth;
    }
}
