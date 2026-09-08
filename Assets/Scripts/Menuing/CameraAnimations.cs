using UnityEngine;

public class CameraAnimations : MonoBehaviour
{
    [SerializeField] private Animator cameraAnimator;

    void Start()
    {
        if (cameraAnimator == null)
            cameraAnimator = GetComponent<Animator>();
    }

    public void MenuSwapToLevel()
    {
        cameraAnimator.SetTrigger("levels");
    }

    public void MenuSwapToMain()
    {
        cameraAnimator.SetTrigger("menu");
    }
}
