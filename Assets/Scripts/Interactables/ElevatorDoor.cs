using UnityEngine;

public class ElevatorDoor : MonoBehaviour
{
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private string playerTag = "Player";

    void Start()
    {
        if (doorAnimator == null)
            doorAnimator = GetComponent<Animator>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            doorAnimator.SetTrigger("open");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            doorAnimator.SetTrigger("close");
        }
    }
}