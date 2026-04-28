using UnityEngine;

public class WeakPointResolver : MonoBehaviour
{
    public bool ResolveWeakPointHit(WeakPoint weakPoint, WeakPointType shotType, string colliderName)
    {
        if (weakPoint == null)
            return false;

        bool correctType = weakPoint.weakPointType == shotType;
        weakPoint.OnHit(shotType);

        if (correctType)
        {
            Debug.Log("Successful weakpoint hit (ammo preserved)! " + colliderName);
            return true;
        }

        Debug.Log("Weakpoint hit, wrong shot type (ammo consumed). " + colliderName);
        return false;
    }
}
