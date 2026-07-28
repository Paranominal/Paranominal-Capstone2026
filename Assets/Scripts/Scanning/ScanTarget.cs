using UnityEngine;

// Summary: Scannable component for enemies. Attach to the enemy root alongside EnemyStagger.
// Scan completion writes to Bestiary and triggers EnemyStagger.OnEnemyScanned().
// Always re-scannable so the stagger mechanic keeps working after discovery.
public class ScanTarget : MonoBehaviour, IScannable
{
    [Header("Enemy")]
    public EnemyDefinition enemyDefinition;

    [Header("Scan")]
    public float scanDuration = 2f;

    private ScanOutline scanOutline;
    private Bestiary bestiary;
    private EnemyStagger stagger;

    void Awake()
    {
        scanOutline = GetComponent<ScanOutline>();
        stagger = GetComponent<EnemyStagger>();

        if (bestiary == null)
            bestiary = FindAnyObjectByType<Bestiary>();
    }

    // -- IScannable --

    public float ScanDuration => scanDuration;

    public bool IsDiscovered =>
        enemyDefinition != null && bestiary != null && bestiary.HasDiscovered(enemyDefinition);

    public bool IsRescannable => true;

    public void OnScanComplete(Texture2D snapshot)
    {
        // Write to Bestiary on first scan only.
        if (enemyDefinition != null && bestiary != null && !bestiary.HasDiscovered(enemyDefinition))
        {
            bestiary.Add(enemyDefinition, snapshot);
        }

        // Always trigger stagger, even on repeat scans.
        if (stagger != null)
        {
            stagger.OnEnemyScanned();
        }
    }

    public void SetOutlineVisible(bool visible)
    {
        if (scanOutline != null) scanOutline.SetVisible(visible);
    }

    public void SetOutlineColor(Color color)
    {
        if (scanOutline != null) scanOutline.SetColor(color);
    }
}
