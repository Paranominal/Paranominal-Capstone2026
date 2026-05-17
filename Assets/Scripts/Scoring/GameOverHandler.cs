using UnityEngine;

public class GameOverHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpiritBar spiritBar;

    private void Awake()
    {
        spiritBar.OnSpiritDepleted += HandleSpiritDepleted;
    }

    private void HandleSpiritDepleted()
    {
        Debug.Log("[GameOverHandler] Spirit depleted — Game Over."); // TODO: actual game over
        UnityEditor.EditorApplication.isPlaying = false;
        Application.Quit();
    }
}