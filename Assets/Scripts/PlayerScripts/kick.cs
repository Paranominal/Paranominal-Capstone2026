using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class kick : MonoBehaviour
{
    
    [SerializeField] private string kickInput = "Kick";
    private InputAction input;
    [SerializeField] private Animator kickAnimator;
    private bool isKicking = false;
    [SerializeField] LayerMask enemyLayer;
    [SerializeField] private float kickStrength = 10;

    void Start()
    {
        input = InputSystem.actions.FindAction(kickInput);
    }

    void Update()
    {
        if (input.WasPressedThisFrame()) StartCoroutine(DoKick());
    }
    private IEnumerator DoKick()
    {
        if (isKicking) yield break;
        isKicking = true;

        kickAnimator.SetTrigger("DoKick");
        yield return null; //the animator needs a frame to update before GetCurrentAnimatorStateInfo can work correctly
        float animDuration = kickAnimator.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(animDuration);
        kickAnimator.SetTrigger("ExitKick");
        isKicking = false;
    }

    void OnTriggerEnter(Collider other)
    {
        Vector3 kickDirection = other.gameObject.transform.position - transform.position;
        // if (other.gameObject.layer == enemyLayer)
        {
            Debug.Log("enemy (" + other + ") was kicked! on "+other.gameObject.layer+" layer!");
            other.GetComponent<Rigidbody>().AddForce(kickDirection * kickStrength, ForceMode.Impulse);
        }
    }
}
