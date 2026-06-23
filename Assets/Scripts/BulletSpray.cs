using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BulletSpray : MonoBehaviour
{
    [SerializeField] private Camera playerCam;
    [SerializeField] private Transform origin;
    [SerializeField] private InputActionReference shoot;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 10;
    [SerializeField] private int maxBulletCount = 100;
    [SerializeField] private int maxDistance = 30;
    public float speedVariability = 0;
    public float angleInaccuracy = 0;
    public float width = 0;
    [SerializeField] private bool randomiseSpin;
    public List<Bullet> bullets;

    [SerializeField] private BulletSprayFX fx;
    public bool debugMode;

    void Update()
    {
        if (shoot.action.IsPressed()) Shoot();
        if (fx != null) fx.DoFX();
        if (bullets.Count > 0) DistanceCheck();
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
        foreach (Bullet bullet in bullets)
        {
            bullet.GetComponent<Bullet>().speed = bulletSpeed + Random.Range(-speedVariability, speedVariability);
        }
        if (bullets.Count > maxBulletCount)
        {
            Destroy(bullets[0].gameObject);
            bullets.RemoveAt(0);
        }
    }
    Bullet InstanceBullet()
    {
        return Instantiate(bulletPrefab, AimPos(), AimDir()).GetComponent<Bullet>();
        
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
    void DistanceCheck()
    {
        for (int i = 0; i < bullets.Count; i++)
        {
            if (debugMode) Debug.Log($"DistanceCheck() bullet index: {i}");
            if (debugMode) Debug.Log($"DistanceCheck() distance of: [{bullets[i]}] = [{Vector3.Distance(bullets[i].transform.position, transform.position)}]");
            
            if (Vector3.Distance(bullets[i].transform.position, transform.position) > maxDistance)
            {
                Destroy(bullets[i].gameObject);
                if (debugMode) Debug.Log($"DistanceCheck() destroyed: {bullets[i]}");
                bullets.RemoveAt(i);
            }
        }
    }
}
