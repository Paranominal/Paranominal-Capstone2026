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
    public List<Bullet> bulletsAll;
    public List<Bullet> bulletsActive;
    public List<Bullet> bulletsInactive;

    private void Awake()
    {
        WarnBulletsInList(); // a debug warning if there are bullets in lists before starting
    }
    private void Start()
    {
        LoadBullets(); // instantiate bullets as cached inactive game objects
    }
    void Update()
    {
        if (shootInput.action.IsPressed()) Shoot(); // Shoot on hold
        if (fx != null) fx.DoFX(); // Start up the FX script
    }
    void LateUpdate()
    {
        if (bulletsActive.Count > 0) DistanceCheck(); // Check for bullets out of range
    }
    private void LoadBullets()
    {
        for (int i = 0; i < maxBulletCount; i++)
        {
            Bullet bullet = InstanceBullet();
            bulletsAll.Add(bullet);
            bulletsInactive.AddRange(bulletsAll);
        }
    }
    private Bullet InstanceBullet()
    {
        return Instantiate(bulletPrefab).GetComponent<Bullet>();
    }
    void Shoot()
    {
        if (shotCooldown) return;
        StartCoroutine(ShotCooldown());
        if (BulletsEmpty()) RetrieveBullet(bulletsActive[0]); // if empty, retrieve oldest bullet

        Bullet bullet = bulletsInactive[0];
        PrepareBullet(bullet);
        bullet.gameObject.SetActive(true); //set active and fire
    }
    private void PrepareBullet(Bullet bullet)
    {
        bullet.speed = bulletSpeed + Random.Range(-speedVariability, speedVariability); //set speed
        bullet.transform.localPosition = AimPos(); // set position
        bullet.transform.localRotation = AimDir(); // set rotation / accuracy
        bullet.transform.localScale = Vector3.one * Random.Range(1, 1 + scaleVariability); // set scale randomness

        bulletsActive.Add(bullet);
        bulletsInactive.Remove(bullet);
    }
    private bool BulletsEmpty()
    {
        if (bulletsInactive.Count == 0) return true;
        else return false;
    }
    private void RetrieveBullet(Bullet bullet)
    {
        bullet.gameObject.SetActive(false); // deactivate it
        bulletsActive.Remove(bullet); // remove it from active list
        bulletsInactive.Add(bullet); // add it to inactive list
        bullet.transform.localPosition = Vector3.zero; // reset position
        bullet.transform.localRotation = Quaternion.identity; // reset rotation
    }
    void DistanceCheck()
    {
        foreach (Bullet bullet in bulletsAll)
        {
            if (bulletsInactive.Contains(bullet)) return;
            float distance = Vector3.Distance(bullet.transform.position, transform.position);
            if (distance > maxDistance) RetrieveBullet(bullet);
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
    void WarnBulletsInList()
    {
        if (bulletsActive.Count > 0)
        {
            Debug.LogWarning("Bullets Active list should start empty! clearing it now!");
            bulletsActive.Clear();
        }
        if (bulletsInactive.Count > 0)
        {
            Debug.LogWarning("Bullets Inctive list should start empty! clearing it now!");
            bulletsInactive.Clear();
        }
    }
}
