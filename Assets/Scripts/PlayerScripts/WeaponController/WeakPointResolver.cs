using UnityEngine;

public class WeakPointResolver : MonoBehaviour
{
    public bool ResolveWeakPointHit(WeakPoint weakPoint, WeakPointType shotType, string colliderName)
    {
        if (weakPoint == null)
            return false;

        bool correctType = weakPoint.weakPointType == shotType;
        weakPoint.OnHit(shotType);

        bool isTough = weakPoint.IsTough;
        bool isWarded = weakPoint.IsWarded;

        if (!weakPoint.IsWarded)
        {
            if (correctType && isTough)
            {
                Debug.Log("Successful tough weakpoint hit (ammo preserved)! " + colliderName +
                    " | Remaining shots: " + weakPoint.RemainingShotsToDestroy);
                return true;
            }

            if (correctType)
            {
                Debug.Log("Successful weakpoint hit (ammo preserved)! " + colliderName);
                return true;
            }
        }
        else if (correctType && isWarded)
        {
            Debug.Log("Weakpoint hit with correct type, but is warded. (ammo consumed)! " + colliderName);
            return false;

        }

        Debug.Log("Weakpoint hit, wrong shot type (ammo consumed). " + colliderName);
        return false;
    }
}
