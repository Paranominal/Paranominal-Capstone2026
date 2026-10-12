using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NameEntryController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InitialsEntry initialsEntry;
    [SerializeField] private int leaderboardSceneBuildIndex;

    private bool hasSubmitted;

    //initialise with empty field with max length setup
    private void Awake()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        initialsEntry.OnConfirmed += SubmitName;
    }

    private void OnDestroy()
    {
        if (initialsEntry != null)
            initialsEntry.OnConfirmed -= SubmitName;
    }

    //hooks with the submit button on inspector
    public void SubmitName()
    {
        if (hasSubmitted) return; // a second press during the loading screen would add a duplicate entry

        string enteredName = initialsEntry.CurrentName;
        if (string.IsNullOrWhiteSpace(enteredName))
        {
            // enteredName = defaultName;
            return;
        }

        hasSubmitted = true;

        if (LeaderboardManager.Instance != null)
        {
            LeaderboardManager.Instance.AddEntry(enteredName, GameOverHandler.FinalScore, GameOverHandler.FinalRank);
        } 
        else
        {
            Debug.LogWarning($"No LeaderboardManager in scene.");
        }

        LoadingManager.Instance?.LoadScene(leaderboardSceneBuildIndex);
    }
}