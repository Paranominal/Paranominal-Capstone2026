using UnityEngine;

public class LeaderboardUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform entryContainer;
    [SerializeField] private LeaderboardEntryUI entryPrefab;

    private void PopulateLeaderboard()
    {
        foreach (Transform child in entryContainer)
        {
            Destroy(child.gameObject);
        }

        if (LeaderboardManager.Instance == null)
        {
            Debug.LogWarning("No LeaderboardManager in scene.");
            return;
        }

        var topEntries = LeaderboardManager.Instance.GetTopEntries();
        var justAdded = LeaderboardManager.Instance.LastAddedEntry;

        int position = 1;
        foreach (var entry in topEntries)
        {
            LeaderboardEntryUI row = Instantiate(entryPrefab, entryContainer);
            row.SetData(position, entry, entry == justAdded);
            position++;
        }
    }
}