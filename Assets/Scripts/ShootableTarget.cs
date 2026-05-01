using System;
using UnityEngine;

public enum TargetBulletType
{
    Any,
    Iron,
    Silver
}

public class ShootableTarget : MonoBehaviour
{
    [SerializeField] private bool destroyOnHit = true;
    [SerializeField] private TargetBulletType validBulletType = TargetBulletType.Any;
    [SerializeField] private EnemyHP enemyHP;
    [Tooltip("Leave empty if target has no HP count.")]

    public bool ResolveHit(WeakPointType shotType)
    {
        bool isValid = false;

        if (validBulletType == TargetBulletType.Any)
        {
            isValid = true;
        }
        else if (validBulletType == TargetBulletType.Iron && shotType == WeakPointType.Iron)
        {
            isValid = true;
        }
        else if (validBulletType == TargetBulletType.Silver && shotType == WeakPointType.Silver)
        {
            isValid = true;
        }

        if (isValid)
        {
            Debug.Log($"ShootableTarget hit successfully with {shotType} round! (ammo preserved)");
            if (destroyOnHit)
            {
                enemyHP.IsShot();
                isValid = true;
                if (enemyHP.hp == 0) Destroy(gameObject);
            }
        }
        else
        {
            Debug.Log($"ShootableTarget hit, but wrong shot type {shotType} (ammo consumed)");
        }

        return isValid;
    }
}