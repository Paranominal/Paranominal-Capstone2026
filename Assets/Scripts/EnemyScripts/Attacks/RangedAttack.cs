using System.Collections.Generic;
using UnityEngine;

public class RangedAttack : MonoBehaviour
{
    [Header("Projectile Properties")]
    [SerializeField] private List<GameObject> attackProjectile = new List<GameObject>();
    [SerializeField] private float baseDamage = 1.0f;
    [SerializeField] private float initialVelocity = 0f;
    [SerializeField] private float maxVelocity = 20.0f;
    [SerializeField] private float acceleration = 1.0f;
    [SerializeField] private float lifetime = 5.0f;

    public void PerformAttack(Transform launchPoint)
    {
        PerformAttack(launchPoint, baseDamage, initialVelocity, maxVelocity, acceleration, lifetime);
    }

    public void PerformAttack(Transform launchPoint, float baseDamage, float initialVelocity, float maxVelocity, float acceleration, float lifetime)
    {
        if (launchPoint == null)
        {
            Debug.LogError("RangedAttack: Launch point is null. Cannot perform attack.");
            return;
        }

        if (attackProjectile == null || attackProjectile.Count == 0 || attackProjectile[0] == null)
        {
            Debug.LogError("RangedAttack: No projectile prefab assigned in attackProjectile list.");
            return;
        }

        Camera playerCamera = Camera.main;
        if (playerCamera == null)
        {
            Debug.LogError("RangedAttack: No main camera found. Cannot aim at Player Camera.");
            return;
        }

        Vector3 launchDirection = (playerCamera.transform.position - launchPoint.position).normalized;

        GameObject prefab = attackProjectile[0];
        GameObject projectileInstance = Instantiate(
            prefab,
            launchPoint.position,
            Quaternion.LookRotation(launchDirection, Vector3.up)
        );

        Projectile proj = projectileInstance.GetComponent<Projectile>();
        if (proj == null)
        {
            Debug.LogError($"RangedAttack: Instantiated prefab '{prefab.name}' does not contain a Projectile component.");
            Destroy(projectileInstance);
            return;
        }

        proj.Initialize(
            gameObject,
            launchDirection,
            baseDamage,
            initialVelocity,
            maxVelocity,
            acceleration,
            lifetime);
    }
}