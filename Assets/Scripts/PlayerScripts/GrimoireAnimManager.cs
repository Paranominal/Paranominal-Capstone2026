using UnityEngine;
using System.Collections;

public class GrimoireAnimManager : MonoBehaviour
{
    [SerializeField] Animator grimoireAnimator;
    private bool isAnimating;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        grimoireAnimator.SetBool("isAnimating",isAnimating);
    }
    void OpenGrimoire()
    {
        grimoireAnimator.SetTrigger("Open");
    }
    
    private IEnumerator DoOpen()
    {
        if (isAnimating) yield break;
        isAnimating = true;

        grimoireAnimator.SetTrigger("Open");
        yield return null; //the animator needs a frame to update before GetCurrentAnimatorStateInfo can work correctly
        float animDuration = grimoireAnimator.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(animDuration);
        isAnimating = false;
    }

    void CloseGrimoire()
    {
        grimoireAnimator.SetTrigger("Close");
    }
}
