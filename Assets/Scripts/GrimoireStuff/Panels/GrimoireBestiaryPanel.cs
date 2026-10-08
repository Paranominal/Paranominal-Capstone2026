using System.Collections.Generic;
using UnityEngine;

// Summary: Bestiary tab of the Full Interface. Lists killed enemies from the Bestiary in the order they were first killed.
// Selecting an entry instantiates that enemy's page prefab on the right page, replacing the previous one.
// Shares the ScrollView content and DetailView with the Inventory tab. The DetailView is cleared while this tab is showing.
// EDIT (bestiary-pages): DetailView filling replaced with page prefabs. "???" unknown state removed.
public class GrimoireBestiaryPanel : MonoBehaviour
{
    [Header("Shared References")]
    [SerializeField] private GrimoireDetailView detailView;
    [Tooltip("The Content transform inside BookL's ScrollView. Shared by all list panels.")]
    [SerializeField] private Transform listParent;
    [SerializeField] private GameObject entryPrefab;

    // EDIT (bestiary-pages): where page prefabs are spawned.
    [Header("Pages")]
    [Tooltip("RectTransform on the right page canvas that page prefabs are spawned under. Stretch it over the area the pages should fill.")]
    [SerializeField] private RectTransform pageParent;

    [Header("External Systems")]
    [SerializeField] private Bestiary bestiary;

    private List<Bestiary.BestiaryRecord> currentRecords = new List<Bestiary.BestiaryRecord>();
    private List<GrimoireEntryButton> entryButtons = new List<GrimoireEntryButton>();
    // EDIT (bestiary-pages): the page currently on screen.
    private BestiaryPage currentPage;

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
        // EDIT (bestiary-pages): the page shouldn't stay on top of other tabs.
        ClearPage();
    }

    private void Rebuild()
    {
        ClearList();

        // EDIT (bestiary-pages): DetailView is never used by this tab, so it's blanked up front.
        if (detailView != null)
            detailView.Clear();

        if (bestiary == null)
        {
            ClearPage();
            return;
        }

        currentRecords = bestiary.GetAllRecords();

        for (int i = 0; i < currentRecords.Count; i++)
        {
            GameObject entryObject = Instantiate(entryPrefab, listParent);
            GrimoireEntryButton entry = entryObject.GetComponent<GrimoireEntryButton>();

            if (entry != null)
            {
                entry.Setup(i, currentRecords[i].definition.displayName, SelectEntry);
                entryButtons.Add(entry);
            }
        }

        if (currentRecords.Count > 0)
            SelectEntry(0);
        else
            ClearPage();
    }

    private void SelectEntry(int index)
    {
        for (int i = 0; i < entryButtons.Count; i++)
            entryButtons[i].SetSelected(i == index);

        // EDIT (bestiary-pages): swap the page instead of filling the DetailView.
        ClearPage();

        if (index >= currentRecords.Count) return;

        Bestiary.BestiaryRecord record = currentRecords[index];
        BestiaryPage pagePrefab = record.definition.pagePrefab;

        if (pagePrefab == null)
        {
            Debug.LogWarning($"[{this}] {record.definition.name} has no page prefab assigned.");
            return;
        }
        if (pageParent == null)
        {
            Debug.LogWarning($"[{this}] No page parent assigned, can't show Bestiary pages.");
            return;
        }

        // false keeps the prefab's own RectTransform layout relative to the parent.
        currentPage = Instantiate(pagePrefab, pageParent, false);
        currentPage.SetKillCount(record.killCount);
    }

    // EDIT (bestiary-pages): destroys the page on screen, if there is one.
    private void ClearPage()
    {
        if (currentPage != null)
            Destroy(currentPage.gameObject);
        currentPage = null;
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
