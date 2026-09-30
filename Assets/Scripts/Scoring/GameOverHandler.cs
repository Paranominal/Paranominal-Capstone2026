using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FearBar fearBar;
    [SerializeField] private ScoreManager scoreManager;

    //was referencing end screen, now goes to name input field
    [SerializeField] private int nameEntrySceneBuildIndex;

    public static int FinalScore { get; private set; }

    public static string FinalRank {get; private set; }

    private void Awake()
    {
        fearBar.OnFearMaxed += HandleFearDepleted;
    }

    private void OnDestroy()
    {   
        if (fearBar != null)
        {
            fearBar.OnFearMaxed -= HandleFearDepleted;
        }
    }

    public void HandleFearDepleted()
    {
        Debug.Log($"Game over!!! Final score: {scoreManager.currentScore}");

        if (scoreManager != null)
        {
            FinalScore = scoreManager.currentScore;
            FinalRank = scoreManager.currentRank;
        }

        SceneManager.LoadScene(nameEntrySceneBuildIndex);
    }
}