using UnityEngine;

public class ToTough : MonoBehaviour, IDamageable
{
    [Header("Stagger Settings")]
    [SerializeField] private int hitsToStagger = 3;
    private int currentHits;
    private bool isStaggered = false;

    private WeakPointManager wpManager;
    private EnemyStagger stagger;
    private EnemyKnockback knockback;

    private void Awake()
    {
        currentHits = hitsToStagger;
        wpManager = GetComponentInChildren<WeakPointManager>(true);
        stagger = GetComponentInChildren<EnemyStagger>();

        knockback = GetComponent<EnemyKnockback>();

        HideWeakpoints();

        //monitors stagger, if it ends, hide weakpoints
        if (stagger != null)
        {
            stagger.OnStaggerEnd += StaggerTimeout;
        }
    }

    //prevent leakage 
    private void OnDestroy()
    {
        if (stagger != null)
        {
            stagger.OnStaggerEnd -= StaggerTimeout;
        }
    }

    private void HideWeakpoints()
    {
        if (wpManager != null)
        {
            wpManager.enabled = false;
            WeakPoint[] points = wpManager.GetComponentsInChildren<WeakPoint>(true);
            foreach (WeakPoint p in points)
            {
                p.Hide();
            }
        }
    }

    public void TakeDamage(DamageInfo info)
    {
        //ignore regular damage, only weakpoints can be shot
        if (isStaggered) return; 

        if (knockback != null)
        {
            knockback.ApplyKnockback();
        }

        currentHits--;
        if (currentHits <= 0)
        {
            Stagger();
        }
    }

    private void Stagger()
    {
        isStaggered = true;
        Debug.Log($"Tough Enemy staggered! Weakpoints exposed.");

        gameObject.Trigger();

        if (wpManager != null)
        {
            wpManager.enabled = true;
            //add to wpm next
            wpManager.ResetWeakpoints();
        }
    }

    private void StaggerTimeout()
    {
        //handles the stagger disappearing,
        if (isStaggered)
        {
            isStaggered = false;
            currentHits = hitsToStagger;
            HideWeakpoints();
        }
    }
}