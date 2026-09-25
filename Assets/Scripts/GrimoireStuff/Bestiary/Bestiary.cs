using System.Collections.Generic;
using UnityEngine;

// Summary: Tracks which enemy types the player has seen and killed. Keyed by EnemyDefinition.
// Seeing an enemy adds a hidden record; the first kill reveals it. Records are kept in discovery order for the UI.
// Owns the snapshot textures and releases them when destroyed.
// Snapshot backfill in Add, snapshot cleanup in OnDestroy.
public class Bestiary : MonoBehaviour
{
    public class BestiaryRecord
    {
        public EnemyDefinition definition;
        public Texture snapshot;
        public int killCount;

        // Full details (name, text, kill count) are only shown once the enemy has been killed.
        public bool IsRevealed => killCount > 0;
    }

    private readonly Dictionary<EnemyDefinition, BestiaryRecord> records = new Dictionary<EnemyDefinition, BestiaryRecord>();
    private readonly List<BestiaryRecord> discoveryOrder = new List<BestiaryRecord>();

    public event System.Action OnBestiaryChanged;

    // Registers an enemy as seen. If it's already been discovered, only fills in a missing snapshot.
    public BestiaryRecord Add(EnemyDefinition definition, Texture snapshot = null)
    {
        if (definition == null) return null;

        if (records.TryGetValue(definition, out BestiaryRecord existing))
        {
            // fills in the image for a record that was added without one (e.g. killed before being seen).
            if (snapshot != null)
            {
                if (existing.snapshot == null)
                {
                    existing.snapshot = snapshot;
                    OnBestiaryChanged?.Invoke();
                }
                else
                {
                    DestroySnapshot(snapshot); // already has one, discard the spare
                }
            }
            return existing;
        }

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

    // Counts a kill. Adds the record first if the enemy was killed without being seen.
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

    // snapshots are RenderTextures created at runtime, so they need freeing manually.
    private void OnDestroy()
    {
        foreach (BestiaryRecord record in discoveryOrder)
            DestroySnapshot(record.snapshot);
    }

    private void DestroySnapshot(Texture snapshot)
    {
        if (snapshot is RenderTexture renderTexture)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
    }
}