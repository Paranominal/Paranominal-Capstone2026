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
            if (destroyOnHit)
            {
                Destroy(gameObject);
            }
        }

        return isValid;
    }
}