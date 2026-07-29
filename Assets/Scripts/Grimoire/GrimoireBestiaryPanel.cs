using System.Collections.Generic;
using UnityEngine;

// Summary: Bestiary tab of the Full Grimoire. Displays discovered enemies from Bestiary.
// Selected entries show their detail and scan snapshot on the right page.
// Read-only: no quick-slot assignment.
// Shares the ScrollView Content and DetailView with other panels.
public class GrimoireBestiaryPanel : MonoBehaviour
{
    [Header("Shared References")]
    [SerializeField] private GrimoireDetailView detailView;
    [Tooltip("The Content transform inside BookL's ScrollView. Shared by all panels.")]
    [SerializeField] private Transform listParent;
    [SerializeField] private GameObject entryPrefab;

    private Bestiary bestiary;
    private List<Bestiary.BestiaryRecord> currentEntries = new List<Bestiary.BestiaryRecord>();
    private List<GrimoireEntryButton> entryButtons = new List<GrimoireEntryButton>();
    private int selectedIndex = -1;

    private void Awake()
    {
        if (bestiary == null)
            bestiary = FindAnyObjectByType<Bestiary>();
    }

    private void OnEnable()
    {
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

        currentEntries = bestiary.GetAllEntries();

        for (int i = 0; i < currentEntries.Count; i++)
        {
            int index = i;
            Bestiary.BestiaryRecord record = currentEntries[i];

            GameObject entryObj = Instantiate(entryPrefab, listParent);
            GrimoireEntryButton entry = entryObj.GetComponent<GrimoireEntryButton>();

            if (entry != null)
            {
                string killInfo = record.killCount > 0
                    ? $"{record.definition.displayName} ({record.killCount})"
                    : record.definition.displayName;
                entry.Setup(index, killInfo, SelectEntry);
                entryButtons.Add(entry);
            }
        }

        if (currentEntries.Count > 0)
            SelectEntry(0);
        else if (detailView != null)
            detailView.Clear();
    }

    private void SelectEntry(int index)
    {
        selectedIndex = Mathf.Clamp(index, 0, currentEntries.Count - 1);

        for (int i = 0; i < entryButtons.Count; i++)
            entryButtons[i].SetSelected(i == selectedIndex);

        if (detailView != null && selectedIndex < currentEntries.Count)
        {
            Bestiary.BestiaryRecord record = currentEntries[selectedIndex];
            EnemyDefinition def = record.definition;

            detailView.SetBestiaryDetail(
                def.displayName,
                def.description,
                def.flavourText,
                def.hintText,
                record.snapshot
            );
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
        currentEntries.Clear();
        selectedIndex = -1;
    }
}
