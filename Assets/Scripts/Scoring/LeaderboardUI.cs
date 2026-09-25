using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform entryContainer;
    [SerializeField] private LeaderboardEntryUI entryPrefab;
    [SerializeField] private ScrollRect scrollRect;

    private void Start()
    {
        PopulateLeaderboard();
    }

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

        if (scrollRect != null)
        {
            StartCoroutine(ScrollToTopNextFrame());
        }
    }

    private IEnumerator ScrollToTopNextFrame()
    {
        yield return null; // wait one full frame
        LayoutRebuilder.ForceRebuildLayoutImmediate(entryContainer as RectTransform);
        scrollRect.StopMovement();
        scrollRect.verticalNormalizedPosition = 1f;
    }
}