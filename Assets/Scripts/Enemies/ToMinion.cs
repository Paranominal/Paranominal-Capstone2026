using UnityEngine;

public class ToMinion : MonoBehaviour, IDamageable
{
    [Header("Scaling")]
    [SerializeField, Min(0.01f)] private float scaleMultiplier = 0.5f;

    [Header("Weakpoint Options")]
    [SerializeField] private bool disableWeakpoints = true;

    private void Awake()
    {
        //scaling
        transform.localScale = transform.localScale * scaleMultiplier;

        if (disableWeakpoints)
        {
            //disables manager and hides weakpoints
            WeakPointManager wpManager = GetComponentInChildren<WeakPointManager>(true);
            if (wpManager != null)
            {
                wpManager.enabled = false;
                WeakPoint[] points = wpManager.GetComponentsInChildren<WeakPoint>(true);
                foreach (WeakPoint p in points)
                {
                    p.Hide();
                }
            }
            else
            {
                //hide weakpoints on children
                WeakPoint[] points = GetComponentsInChildren<WeakPoint>(true);
                foreach (WeakPoint p in points)
                {
                    p.Hide();
                }
            }
        }
    }

    //die immediately when damaged
    public void TakeDamage(DamageInfo info)
    {
        EnemyBehaviourBase behaviour = GetComponentInParent<EnemyBehaviourBase>();
        if (behaviour != null)
        {
            behaviour.Die();
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
