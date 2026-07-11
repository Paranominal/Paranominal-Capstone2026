using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BulletSpray : MonoBehaviour
{
    [SerializeField] private Camera playerCam;
    [SerializeField] private Transform origin;
    [SerializeField] private InputActionReference shoot;
    [SerializeField] private List<GameObject> bulletPrefabs;
    [SerializeField] private float shotsPerSecond = 5;
    [SerializeField] private int bulletsPerShot = 1;
    private bool shotCooldown;
    [SerializeField] private float bulletSpeed = 10;
    [SerializeField] private int maxBulletCount = 100;
    [SerializeField] private int maxDistance = 30;
    public float speedVariability = 0;
    public float scaleVariability = 0;
    public float angleInaccuracy = 0;
    public float width = 0;
    [SerializeField] private bool randomiseSpin;
    public List<Bullet> bullets;
    [SerializeField] private BulletSprayFX fx;
    public bool debugMode;

    void Update()
    {
        //the shooting
        if (shoot.action.IsPressed() && !shotCooldown) for (int i = 0; i < bulletsPerShot; i++)
        {
            StartCoroutine(FireRateCD());
            Shoot();
        }
        //activate the visual fx script
        if (fx != null) fx.DoFX();
        //check for bullets that are out of range
        if (bullets.Count > 0) DistanceCheck();

        CleanUp();
    }
    Quaternion AimDir()
    {
        Quaternion aimDir = Quaternion.LookRotation(transform.forward + Inaccuracy());
        // if (randomiseSpin) aimDir.eulerAngles = aimDir.eulerAngles + RandomSpin();
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
        Bullet bullet = InstanceBullet();
        bullets.Add(bullet);
        bullet.speed = bulletSpeed + Random.Range(-speedVariability, speedVariability);
        bullet.SetSpeed();
        float newScale = Random.Range(1, 1 + scaleVariability);
        bullet.transform.localScale = Vector3.one * newScale;

        if (bullets.Count > maxBulletCount)
        {
            Destroy(bullets[0].gameObject);
            bullets.RemoveAt(0);
        }
    }
    IEnumerator FireRateCD()
    {
        shotCooldown = true;
        if (debugMode) Debug.Log($"{this} | Cooling down for [{1/shotsPerSecond}] seconds");
        yield return new WaitForSeconds(1/shotsPerSecond);
        if (debugMode) Debug.Log($"{this} | Cool down complete");
        shotCooldown = false;
    }
    Bullet InstanceBullet()
    {
        return Instantiate(bulletPrefabs[Random.Range(0,bulletPrefabs.Count)], AimPos(), AimDir()).GetComponent<Bullet>();
    }
    Vector3 Inaccuracy()
    {
        Vector3 inaccuracy = new Vector3(Random.Range(-angleInaccuracy, angleInaccuracy), Random.Range(-angleInaccuracy, angleInaccuracy), Random.Range(-angleInaccuracy, angleInaccuracy)) * 0.01f;
        if (debugMode) Debug.Log($"{this} | Innaccuracy():{inaccuracy}");
        return inaccuracy;
    }
    // Vector3 RandomSpin()
    // {
    //     Vector3 randomSpin = new Vector3(0, 0, Random.Range(0, 360));
    //     if (debugMode) Debug.Log($"{this} | RandomSpin():{randomSpin}");
    //     return randomSpin;
    // }
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
    
    void CleanUp() //currently not working
    {
        // foreach (Bullet bullet in bullets)
        // {
        //     if (bullet == null)
        //     {
        //         bullets.RemoveAt(bullets.IndexOf(bullet));
        //     }
        // }

        for (var i = bullets.Count -1; i > -1; i--)
        {
            if (bullets[i] == null) bullets.RemoveAt(i);
        }
    }
}
