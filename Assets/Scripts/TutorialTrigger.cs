using UnityEngine;
using System;

public class TutorialTrigger : MonoBehaviour
{
    public event Action TutorialTriggered; 
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Player") TutorialTriggered?.Invoke();
    }
}
