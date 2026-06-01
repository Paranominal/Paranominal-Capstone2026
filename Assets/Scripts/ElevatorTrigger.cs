using UnityEngine;

public class ElevatorTrigger : MonoBehaviour
{
    [SerializeField] private GameOverHandler gameOverHandler;
    [SerializeField] private GameObject miriam;
    void OnTriggerEnter(Collider other)
    {
        if (other == miriam) gameOverHandler.HandleSpiritDepleted();
    }
}
