using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using NUnit.Framework;

public class GrimoireAnimManager : MonoBehaviour
{
    [SerializeField] private Animator grimoireAnimator;
    private bool isAnimating;
    [SerializeField] private InputActionReference grimoireScrollInput;
    private InputAction grimoireScroll;
    [SerializeField] private InputActionReference PlayerMoveInput;
    private InputAction playerMove;
    private bool playerIsMoving;
    private bool isOpen;

    void Start()
    {
        grimoireScroll = grimoireScrollInput;
        playerMove = PlayerMoveInput;
    }

    void Update()
    {
        CheckPlayerMovement();
        OpenOnScroll();

        grimoireAnimator.SetBool("isAnimating", isAnimating);

        DoDebugLog();
    }
    void OpenGrimoire()
    {
        StartCoroutine(DoOpen());
    }
    void CloseGrimoire()
    {
        StartCoroutine(DoClose());
    }
    
    private IEnumerator DoOpen()
    {
        if (isAnimating) yield break;
        if (isOpen) yield break;
        grimoireAnimator.SetTrigger("open");
    }
    
    private IEnumerator DoClose()
    {
        if (isAnimating) yield break;
        if (!isOpen) yield break;
        grimoireAnimator.SetTrigger("close");
    }
    private void OpenOnScroll()
    {
        if (Mathf.Abs(grimoireScroll.ReadValue<Vector2>().y) > 0) OpenGrimoire();
    }
    private void CheckPlayerMovement()
    {
        if (playerMove.ReadValue<Vector2>() != new Vector2(0, 0)) playerIsMoving = true;
        else playerIsMoving = false;

        if (playerIsMoving) CloseGrimoire();
    }
    private void DoDebugLog()
    {
        Debug.Log("isOpen: " + isOpen);
        Debug.Log("isAnimating: " + isAnimating);
    }
    private void StartAnimation()
    {
        isAnimating = true;
    }
    private void EndAnimation()
    {
        isAnimating = false;
    }
    private void SetOpen()
    {
        isOpen = true;
    }
    private void SetClosed()
    {
        isOpen = false;
    }
}