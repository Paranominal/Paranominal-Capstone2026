using UnityEngine;

public class ToMiniBoss : MonoBehaviour, IDamageable
{
    [SerializeField, Min(0.01f)] private float scaleMultiplier = 1.5f;

    [Header("Stagger & Cycle Settings")]
    [SerializeField] private int hitsToStagger = 5;
    [SerializeField] private int cyclesRequired = 3;

    private int currentHits;
    private int currentCycle = 0;
    private bool isStaggered = false;

    private WeakPointManager wpManager;
    private EnemyKnockback knockback;

    private void Awake()
    {
        //scaling
        transform.localScale = transform.localScale * scaleMultiplier;

        currentHits = hitsToStagger;
        wpManager = GetComponentInChildren<WeakPointManager>(true);

        knockback = GetComponent<EnemyKnockback>();

        HideWeakpoints();
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
        Debug.Log($"Miniboss staggered! Weakpoints exposed.");

        if (wpManager != null)
        {
            wpManager.enabled = true;
            //add to wpm next
            wpManager.ResetWeakpoints();
        }
    }

    //call wpm when cycles are destroyed
    public void OnCycleComplete()
    {
        currentCycle++;
        if (currentCycle >= cyclesRequired)
        {
            //death logic
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
        else
        {
            //reset next cycle if still alive
            isStaggered = false;
            currentHits = hitsToStagger;
            HideWeakpoints();
            Debug.Log($"Cycle {currentCycle} complete.");
        }
    }
}