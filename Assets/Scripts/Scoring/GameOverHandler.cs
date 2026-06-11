using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpiritBar spiritBar;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private int endScreenBuildIndex;

    public static int FinalScore { get; private set; }

    private void Awake()
    {
        spiritBar.OnSpiritDepleted += HandleSpiritDepleted;
    }

    private void OnDestroy()
    {   
        if (spiritBar != null)
        {
            spiritBar.OnSpiritDepleted -= HandleSpiritDepleted;
        }
    }

    public void HandleSpiritDepleted()
    {
        Debug.Log($"Game over!!! Final score: {scoreManager.currentScore}");

        if (scoreManager != null)
        {
            FinalScore = scoreManager.currentScore;
        }

        SceneManager.LoadScene(endScreenBuildIndex);
    }
}