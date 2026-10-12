using System.Collections.Generic;
using UnityEngine;

// Summary: Tracks which enemy types the player has killed, and how many of each. Keyed by EnemyDefinition.
// An enemy type is added on its first kill. Records are kept in discovery order for the UI.
// EDIT (bestiary-pages): sightings and snapshots removed. Records are only created by kills, so there's no hidden state.
public class Bestiary : MonoBehaviour
{
    public class BestiaryRecord
    {
        public EnemyDefinition definition;
        public int killCount;
    }

    private readonly Dictionary<EnemyDefinition, BestiaryRecord> records = new Dictionary<EnemyDefinition, BestiaryRecord>();
    private readonly List<BestiaryRecord> discoveryOrder = new List<BestiaryRecord>();

    public event System.Action OnBestiaryChanged;

    // EDIT (bestiary-pages): replaces Add. Creates the record on the first kill.
    public void RecordKill(EnemyDefinition definition)
    {
        if (definition == null) return;

        if (!records.TryGetValue(definition, out BestiaryRecord record))
        {
            record = new BestiaryRecord { definition = definition, killCount = 0 };
            records[definition] = record;
            discoveryOrder.Add(record);
        }

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
