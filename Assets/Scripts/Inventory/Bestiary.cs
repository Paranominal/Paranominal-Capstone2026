using System.Collections.Generic;
using UnityEngine;

// Summary: Tracks which enemies the player has discovered via scanning.
// Lives on GameSystems alongside Inventory and DiscoveryLog.
// Keyed by EnemyDefinition; stores runtime data like snapshots and kill counts.
public class Bestiary : MonoBehaviour
{
    public class BestiaryRecord
    {
        public EnemyDefinition definition;
        public Texture2D snapshot;
        public int killCount;
    }

    private Dictionary<EnemyDefinition, BestiaryRecord> records
        = new Dictionary<EnemyDefinition, BestiaryRecord>();

    public event System.Action OnBestiaryChanged;

    // Summary: Register an enemy as discovered. If already discovered, does nothing.
    public BestiaryRecord Add(EnemyDefinition def, Texture2D snapshot = null)
    {
        if (def == null) return null;

        if (records.ContainsKey(def))
            return records[def];

        BestiaryRecord record = new BestiaryRecord
        {
            definition = def,
            snapshot = snapshot,
            killCount = 0,
        };
        records[def] = record;

        OnBestiaryChanged?.Invoke();
        return record;
    }

    public bool HasDiscovered(EnemyDefinition def)
    {
        return def != null && records.ContainsKey(def);
    }

    public BestiaryRecord GetEntry(EnemyDefinition def)
    {
        if (def == null) return null;
        return records.TryGetValue(def, out BestiaryRecord record) ? record : null;
    }

    public void RecordKill(EnemyDefinition def)
    {
        if (def == null) return;

        if (!records.ContainsKey(def))
            Add(def);

        records[def].killCount++;
        OnBestiaryChanged?.Invoke();
    }

    public List<BestiaryRecord> GetAllEntries()
    {
        return new List<BestiaryRecord>(records.Values);
    }
}
