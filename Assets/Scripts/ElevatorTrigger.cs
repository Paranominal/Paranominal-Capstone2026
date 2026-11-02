using UnityEngine;
using UnityEngine.SceneManagement;

public class ElevatorTrigger : MonoBehaviour
{
    [SerializeField] private GameOverHandler gameOverHandler;
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag != "Player") return;

       gameOverHandler.HandleElevatorReached();
    }
}
