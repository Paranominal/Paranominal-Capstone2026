using System.Collections.Generic;
using UnityEngine;

// Summary: Tracks which enemy types the player has seen and killed. Keyed by EnemyDefinition.
// Seeing an enemy adds a hidden record; the first kill reveals it. Records are kept in discovery order for the UI.
public class Bestiary : MonoBehaviour
{
    public class BestiaryRecord
    {
        public EnemyDefinition definition;
        public Texture snapshot;
        public int killCount;

        // Summary: Full details (name, text, kill count) are only shown once the enemy has been killed.
        public bool IsRevealed => killCount > 0;
    }

    private readonly Dictionary<EnemyDefinition, BestiaryRecord> records = new Dictionary<EnemyDefinition, BestiaryRecord>();
    private readonly List<BestiaryRecord> discoveryOrder = new List<BestiaryRecord>();

    public event System.Action OnBestiaryChanged;

    // Summary: Registers an enemy as seen. Does nothing if it's already been discovered.
    public BestiaryRecord Add(EnemyDefinition definition, Texture snapshot = null)
    {
        if (definition == null) return null;

        if (records.TryGetValue(definition, out BestiaryRecord existing))
            return existing;

        BestiaryRecord record = new BestiaryRecord
        {
            definition = definition,
            snapshot = snapshot,
            killCount = 0,
        };
        records[definition] = record;
        discoveryOrder.Add(record);

        OnBestiaryChanged?.Invoke();
        return record;
    }

    // Summary: Counts a kill. Adds the record first if the enemy was killed without being seen.
    public void RecordKill(EnemyDefinition definition)
    {
        if (definition == null) return;

        BestiaryRecord record = Add(definition);
        record.killCount++;

        OnBestiaryChanged?.Invoke();
    }

    public bool HasDiscovered(EnemyDefinition definition)
    {
        return definition != null && records.ContainsKey(definition);
    }

    public BestiaryRecord GetRecord(EnemyDefinition definition)
    {
        if (definition == null) return null;
        return records.TryGetValue(definition, out BestiaryRecord record) ? record : null;
    }

    public List<BestiaryRecord> GetAllRecords()
    {
        return new List<BestiaryRecord>(discoveryOrder);
    }
}
