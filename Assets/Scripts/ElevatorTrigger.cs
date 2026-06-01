using UnityEngine;

public class ElevatorTrigger : MonoBehaviour
{
    [SerializeField] private GameOverHandler gameOverHandler;
    [SerializeField] private GameObject miriam;
    void OnTriggerEnter(Collider other)
    {
        gameOverHandler.HandleSpiritDepleted();
        Debug.Log("game over triggered by:" + other);
    }
}
