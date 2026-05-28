using UnityEngine;

public class GameOverHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpiritBar spiritBar;
    [SerializeField] private ScoreManager scoreManager;

    private void Awake()
    {
        spiritBar.OnSpiritDepleted += HandleSpiritDepleted;
    }

    private void HandleSpiritDepleted()
    {
        Debug.Log("Game over!!! Final score: {scoreManager.currentScore}");
        // TODO: actual game over

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}