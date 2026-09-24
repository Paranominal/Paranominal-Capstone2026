using System.Collections.Generic;
using UnityEngine;

// Summary: Inventory tab of the Full Interface. Lists ALTGrimoire's entries in the same order as the Minimised UI.
// Selecting an entry here also makes it the current item, so it's what the player holds when they close the Grimoire.
// Shares the ScrollView content and DetailView with the Bestiary tab.
public class GrimoireInventoryPanel : MonoBehaviour
{
    [Header("Shared References")]
    [SerializeField] private GrimoireDetailView detailView;
    [Tooltip("The Content transform inside BookL's ScrollView. Shared by all list panels.")]
    [SerializeField] private Transform listParent;
    [SerializeField] private GameObject entryPrefab;

    [Header("External Systems")]
    [SerializeField] private ALTGrimoire grimoire;

    private List<GrimoireEntryButton> entryButtons = new List<GrimoireEntryButton>();

    private void OnEnable()
    {
        if (grimoire == null)
            grimoire = ALTGrimoire.instance != null ? ALTGrimoire.instance : FindAnyObjectByType<ALTGrimoire>();

        if (grimoire != null)
            grimoire.OnEntriesChanged += Rebuild;

        Rebuild();
    }

    private void OnDisable()
    {
        if (grimoire != null)
            grimoire.OnEntriesChanged -= Rebuild;

        ClearList();
    }

    private void Rebuild()
    {
        ClearList();

        if (grimoire == null || grimoire.entries == null) return;

        for (int i = 0; i < grimoire.entries.Count; i++)
        {
            GameObject entryObject = Instantiate(entryPrefab, listParent);
            GrimoireEntryButton entry = entryObject.GetComponent<GrimoireEntryButton>();

            if (entry != null)
            {
                entry.Setup(i, grimoire.entries[i].entryName, SelectEntry);
                entryButtons.Add(entry);
            }
        }

        if (grimoire.entries.Count > 0)
            SelectEntry(Mathf.Clamp(grimoire.CurrentEntryIndex, 0, grimoire.entries.Count - 1));
        else if (detailView != null)
            detailView.Clear();
    }

    private void SelectEntry(int index)
    {
        // Sets the current item for gameplay (InteractionObject, Container, etc.).
        grimoire.SelectEntry(index);

        for (int i = 0; i < entryButtons.Count; i++)
            entryButtons[i].SetSelected(i == index);

        if (detailView != null)
        {
            ALTGrimoireEntry entry = grimoire.entries[index];
            detailView.SetDetail(
                entry.entryName,
                entry.collected ? "collected" : "",
                "",
                entry.flavourText,
                entry.hintText,
                entry.snapshotImage,
                index
            );
        }
    }

    private void ClearList()
    {
        // Destroy all children of the shared list parent for a clean slate.
        if (listParent != null)
        {
            foreach (Transform child in listParent)
                Destroy(child.gameObject);
        }
        entryButtons.Clear();
    }
}
