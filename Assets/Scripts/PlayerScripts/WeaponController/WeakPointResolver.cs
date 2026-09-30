using UnityEngine;

public class WeakPointResolver : MonoBehaviour
{
    public ShotOutcome ResolveWeakPointHit(WeakPoint weakPoint, WeakPointType shotType, string colliderName)
    {
        if (weakPoint == null)
            return ShotOutcome.Miss;

        bool correctType = weakPoint.weakPointType == shotType;
        weakPoint.OnHit(shotType);

        if (!correctType)
        {
            Debug.Log("Weakpoint hit, wrong shot type (ammo consumed). " + colliderName);
            return ShotOutcome.WrongAmmo;
        }

        if (weakPoint.IsWarded)
        {
            Debug.Log("Weakpoint hit with correct type, but is warded. (ammo consumed)! " + colliderName);
            return ShotOutcome.EnemyHit; // neutral, doesn't break the combo
        }

        if (weakPoint.IsTough)
        {
            Debug.Log("Successful tough weakpoint hit (ammo preserved)! " + colliderName +
                " | Remaining shots: " + weakPoint.RemainingShotsToDestroy);
        }
        else
        {
            Debug.Log("Successful weakpoint hit (ammo preserved)! " + colliderName);
        }

        return ShotOutcome.WeakPointHit;
    }
}
