using System.Collections.Generic;
using UnityEngine;

// Summary: Bestiary tab of the Full Interface. Lists enemies from the Bestiary in the order they were discovered.
// Seen but unkilled enemies show as "???" with only their image. Killing one reveals the full entry and kill count.
// Shares the ScrollView content and DetailView with the Inventory tab.
public class GrimoireBestiaryPanel : MonoBehaviour
{
    private const string UnknownName = "???";

    [Header("Shared References")]
    [SerializeField] private GrimoireDetailView detailView;
    [Tooltip("The Content transform inside BookL's ScrollView. Shared by all list panels.")]
    [SerializeField] private Transform listParent;
    [SerializeField] private GameObject entryPrefab;

    [Header("External Systems")]
    [SerializeField] private Bestiary bestiary;

    private List<Bestiary.BestiaryRecord> currentRecords = new List<Bestiary.BestiaryRecord>();
    private List<GrimoireEntryButton> entryButtons = new List<GrimoireEntryButton>();

    private void OnEnable()
    {
        if (bestiary == null)
            bestiary = FindAnyObjectByType<Bestiary>();

        if (bestiary != null)
            bestiary.OnBestiaryChanged += Rebuild;

        Rebuild();
    }

    private void OnDisable()
    {
        if (bestiary != null)
            bestiary.OnBestiaryChanged -= Rebuild;

        ClearList();
    }

    private void Rebuild()
    {
        ClearList();

        if (bestiary == null) return;

        currentRecords = bestiary.GetAllRecords();

        for (int i = 0; i < currentRecords.Count; i++)
        {
            Bestiary.BestiaryRecord record = currentRecords[i];

            GameObject entryObject = Instantiate(entryPrefab, listParent);
            GrimoireEntryButton entry = entryObject.GetComponent<GrimoireEntryButton>();

            if (entry != null)
            {
                string label = record.IsRevealed ? record.definition.displayName : UnknownName;
                entry.Setup(i, label, SelectEntry);
                entryButtons.Add(entry);
            }
        }

        if (currentRecords.Count > 0)
            SelectEntry(0);
        else if (detailView != null)
            detailView.Clear();
    }

    private void SelectEntry(int index)
    {
        for (int i = 0; i < entryButtons.Count; i++)
            entryButtons[i].SetSelected(i == index);

        if (detailView == null || index >= currentRecords.Count) return;

        Bestiary.BestiaryRecord record = currentRecords[index];

        if (record.IsRevealed)
        {
            EnemyDefinition definition = record.definition;
            detailView.SetDetail(
                definition.displayName,
                $"Defeated: {record.killCount}",
                definition.description,
                definition.flavourText,
                definition.hintText,
                record.snapshot,
                index
            );
        }
        else
        {
            detailView.SetDetail(UnknownName, "", "", "", "", record.snapshot, index);
        }
    }

    private void ClearList()
    {
        if (listParent != null)
        {
            foreach (Transform child in listParent)
                Destroy(child.gameObject);
        }
        entryButtons.Clear();
        currentRecords.Clear();
    }
}
