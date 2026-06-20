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
    [SerializeField] private int maxBulletCount = 100;
    public float inaccuracy = 0;
    public float width = 0;
    public List<GameObject> bullets; //make this public to bug test, make private after

    void Update()
    {
        AimDir();
        Inaccuracy();
        if (shoot.action.IsPressed()) Shoot();
    }
    Quaternion AimDir()
    {
        return Quaternion.LookRotation(transform.forward + Inaccuracy());
    }
    Vector3 AimPos()
    {        
        return origin.position + OriginWidth();
    }
    void Shoot()
    {
        bullets.Add(InstanceBullet());
        foreach (GameObject x in bullets)
        {
            x.GetComponent<Bullet>().speed = bulletSpeed;
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
        return new Vector3(Random.Range(-inaccuracy, inaccuracy), Random.Range(-inaccuracy, inaccuracy), Random.Range(-inaccuracy, inaccuracy)) * 0.01f;
    }
    Vector3 OriginWidth()
    {
        return new Vector3(Random.Range(-width, width), Random.Range(-width, width), Random.Range(-width, width)) * 0.01f;
    }
}
