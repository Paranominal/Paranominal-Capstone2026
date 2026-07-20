using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BulletSpray : MonoBehaviour
{
    [SerializeField] private Camera playerCam;
    [SerializeField] private Transform shotOrigin;
    [SerializeField] private InputActionReference shootInput;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private BulletSprayFX fx;
    public bool debugMode;
    [SerializeField] private float shotsPerSecond = 5;
    private bool shotCooldown;
    [SerializeField] private float bulletSpeed = 20;
    [SerializeField] private int maxBulletCount = 50;
    [SerializeField] private int maxDistance = 30;
    public float speedVariability = 0;
    public float scaleVariability = 0;
    public float angleInaccuracy = 0;
    public float bulletSpread = 0;
    [Header("Bullets")]
    public List<Bullet> bullets;
    void Update()
    {
        if (shootInput.action.IsPressed()) Shoot(); // Shoot on hold
        if (fx != null) fx.DoFX(); // Start up the FX script
    }
    void LateUpdate()
    {
        if (bullets.Count > 0) DistanceCheck(); // Check for bullets out of range
    }
    private Bullet InstanceBullet()
    {
        return Instantiate(bulletPrefab).GetComponent<Bullet>();
    }
    void Shoot()
    {
        if (shotCooldown) return;
        StartCoroutine(ShotCooldown());
        if (bullets.Count >= maxBulletCount) RemoveBullet(bullets[0]);

        Bullet bullet = InstanceBullet();
        PrepareBullet(bullet);
        bullet.gameObject.SetActive(true); //set active
    }
    private void PrepareBullet(Bullet bullet)
    {
        bullet.speed = bulletSpeed + Random.Range(-speedVariability, speedVariability); //set speed
        bullet.transform.localPosition = AimPos(); // set position
        bullet.transform.localRotation = AimDir(); // set rotation / accuracy
        bullet.transform.localScale = Vector3.one * Random.Range(1, 1 + scaleVariability); // set scale randomness

        bullets.Add(bullet);
    }
    void RemoveBullet(Bullet bullet)
    {
        bullets.Remove(bullet);
        Destroy(bullet.gameObject);
    }
    void DistanceCheck()
    {
        foreach (Bullet bullet in bullets)
        {
            float distance = Vector3.Distance(bullet.transform.position, transform.position);
            if (distance > maxDistance) bullet.gameObject.SetActive(false);
        }
    }
    Vector3 AimPos()
    {        
        Vector3 aimPos = shotOrigin.position + OriginWidth();
        if (debugMode) Debug.Log($"{this} | AimPos():{aimPos}");
        return aimPos;
    }
    Vector3 OriginWidth()
    {
        Vector3 originWidth = new Vector3 (Random.Range(-bulletSpread, bulletSpread), Random.Range(-bulletSpread, bulletSpread), Random.Range(-bulletSpread, bulletSpread)) * 0.01f;
        if (debugMode) Debug.Log($"{this} | OriginWidth():{originWidth}");
        return originWidth;
    }
    Quaternion AimDir()
    {
        Quaternion aimDir = Quaternion.LookRotation(transform.forward + Inaccuracy());
        if (debugMode) Debug.Log($"{this} | AimDir():{aimDir}");
        return aimDir;
    }
    Vector3 Inaccuracy()
    {
        Vector3 inaccuracy = new Vector3(Random.Range(-angleInaccuracy, angleInaccuracy), Random.Range(-angleInaccuracy, angleInaccuracy), Random.Range(-angleInaccuracy, angleInaccuracy)) * 0.01f;
        if (debugMode) Debug.Log($"{this} | Innaccuracy():{inaccuracy}");
        return inaccuracy;
    }
    IEnumerator ShotCooldown()
    {
        shotCooldown = true;
        if (debugMode) Debug.Log($"{this} | Cooling down for [{1 / shotsPerSecond}] seconds");
        yield return new WaitForSeconds(1 / shotsPerSecond);
        if (debugMode) Debug.Log($"{this} | Cool down complete");
        shotCooldown = false;
    }
}
