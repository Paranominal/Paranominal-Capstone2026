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

        //hide weakpoints
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
        Debug.Log("Enemy Staggered! Weakpoints exposed.");

        //enable wpm to setup the weakpoints
        if (wpManager != null)
        {
            if (stagger != null) stagger.TriggerStagger();
            else Debug.LogWarning($"Tough enemy [{this}] is missing EnemyStagger component!");
            wpManager.enabled = true;
        }
    }
}