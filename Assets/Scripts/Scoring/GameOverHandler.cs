using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FearBar fearBar;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private int endScreenBuildIndex;

    public static int FinalScore { get; private set; }

    // EDIT (auto-resolve): fallback for cross-prefab references.
    private void Awake()
    {
        if (fearBar == null)
            fearBar = FindAnyObjectByType<FearBar>();
        if (scoreManager == null)
            scoreManager = FindAnyObjectByType<ScoreManager>();

        if (fearBar != null)
            fearBar.OnFearMaxed += HandleFearDepleted;
    }

    private void OnDestroy()
    {   
        if (fearBar != null)
            fearBar.OnFearMaxed -= HandleFearDepleted;
    }

    public void HandleFearDepleted()
    {
        Debug.Log($"Game over!!! Final score: {scoreManager.currentScore}");

        if (scoreManager != null)
        {
            FinalScore = scoreManager.currentScore;
        }

        SceneManager.LoadScene(endScreenBuildIndex);
    }
}
