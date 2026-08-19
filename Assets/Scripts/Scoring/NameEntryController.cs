using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NameEntryController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private int leaderboardSceneBuildIndex;

    [Header("Config")]
    [SerializeField] private int maxNameLength = 12;
    [SerializeField] private string defaultName = "Player";

    //initialise with empty field with max length setup
    private void Awake()
    {
        if (nameInputField != null)
        {
            nameInputField.characterLimit = maxNameLength;
        }
    }

    //hooks with the submit button on inspector
    private void SubmitName()
    {
        string enteredName = nameInputField != null ? nameInputField.text : string.Empty;
        if (string.IsNullOrWhiteSpace(enteredName))
        {
            enteredName = defaultName;
        }

        if (LeaderboardManager.Instance != null)
        {
            LeaderboardManager.Instance.AddEntry(enteredName, GameOverHandler.FinalScore, GameOverHandler.FinalRank);
        } 
        else
        {
            Debug.LogWarning($"No LeaderboardManager in scene.");
        }

        SceneManager.LoadScene(leaderboardSceneBuildIndex);
    }
}