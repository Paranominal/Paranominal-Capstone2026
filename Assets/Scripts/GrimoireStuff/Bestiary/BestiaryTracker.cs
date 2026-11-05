using UnityEngine;

// Summary: Links an enemy to its Bestiary entry. Records a kill when the enemy dies, which adds the entry on the first one.
// EDIT (bestiary-pages): sighting checks and snapshots removed.
[RequireComponent(typeof(Enemy))]
public class BestiaryTracker : MonoBehaviour
{
    [SerializeField] private EnemyDefinition definition;

    [Header("External Systems")]
    [SerializeField] private Bestiary bestiary;

    private Enemy enemy;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    private void Start()
    {
        if (bestiary == null)
            bestiary = FindAnyObjectByType<Bestiary>();

        if (definition == null)
            Debug.LogWarning($"[{this}] No EnemyDefinition assigned, this enemy won't appear in the Bestiary.");
    }

    private void OnEnable()
    {
        if (enemy != null) enemy.OnDied += HandleDied;
    }

    private void OnDisable()
    {
        if (enemy != null) enemy.OnDied -= HandleDied;
    }

    private void HandleDied(Enemy deadEnemy)
    {
        // fallback in case the Bestiary loaded after Start ran.
        if (bestiary == null)
            bestiary = FindAnyObjectByType<Bestiary>();

        if (bestiary != null && definition != null)
            bestiary.RecordKill(definition);
    }
}
