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
    [SerializeField] private EnemyHP enemyHP = null;
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
            if (enemyHP != null)
            {
                enemyHP.IsShot();
                isValid = true;
            }
            else if (destroyOnHit)
            {
                isValid = true;
                Destroy(gameObject);
            }
        }
        else
        {
            Debug.Log($"ShootableTarget hit, but wrong shot type {shotType} (ammo consumed)");
        }

        return isValid;
    }
}