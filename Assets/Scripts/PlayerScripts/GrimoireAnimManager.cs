using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using NUnit.Framework;
using Unity.VisualScripting;

public class GrimoireAnimManager : MonoBehaviour
{
    [SerializeField] private Animator grimoireAnimator;
    private bool isAnimating;
    [SerializeField] private InputActionReference grimoireScrollInput;
    private InputAction grimoireScroll;
    [SerializeField] private InputActionReference playerMoveInput;
    private InputAction playerMove;
    [SerializeField] private InputActionReference scanActionInput;
    private InputAction scanAction;
    private bool playerIsMoving;
    private bool isOpen;
    private bool isCasting;
    public bool debugMode;

    void Start()
    {
        grimoireScroll = grimoireScrollInput;
        playerMove = playerMoveInput;
        scanAction = scanActionInput;
    }

    void Update()
    {
        CheckPlayerMovement();
        OpenOnScroll();
        CheckCast();

        if (debugMode) DoDebugLog();
    }
    private void OpenOnScroll()
    {
        if (Mathf.Abs(grimoireScroll.ReadValue<Vector2>().y) > 0) OpenGrimoire();
    }
    void OpenGrimoire()
    {
        StartCoroutine(DoOpen());
    }
    private IEnumerator DoOpen()
    {
        if (isAnimating) yield break;
        if (isOpen) yield break;
        grimoireAnimator.SetTrigger("open");
    }
    void CloseGrimoire()
    {
        StartCoroutine(DoClose());
    }
    private IEnumerator DoClose()
    {
        if (isAnimating) yield break;
        if (!isOpen) yield break;
        if (scanAction.IsPressed()) yield break;
        grimoireAnimator.SetTrigger("close");
    }
    private void CheckCast()
    {
        grimoireAnimator.SetBool("isCasting", isCasting);

        if (scanAction.IsPressed()) Cast();
        else isCasting = false;
    }
    private void Cast()
    {
        StartCoroutine(DoCasting());
    }
    private IEnumerator DoCasting()
    {
        if (!scanAction.IsPressed()) yield break;
        if (!isOpen)
        {
            OpenGrimoire();
            yield break;
        }

        isCasting = true;
    }
    private void CheckPlayerMovement()
    {
        if (playerMove.ReadValue<Vector2>() != new Vector2(0, 0)) playerIsMoving = true;
        else playerIsMoving = false;

        if (playerIsMoving && !isCasting) CloseGrimoire();
    }
    private void DoDebugLog()
    {
        Debug.Log("isOpen: " + isOpen);
        Debug.Log("isAnimating: " + isAnimating);
        Debug.Log("Casting is pressed: " + scanAction.IsPressed());
        Debug.Log("isCasting: " + isCasting);
    }
    private void StartAnimation()
    {
        isAnimating = true;
    }
    private void EndAnimation()
    {
        isAnimating = false;
    }
    private void SetOpenTrue()
    {
        isOpen = true;
    }
    private void SetOpenFalse()
    {
        isOpen = false;
    }
    private void SetCastingTrue()
    {
        isCasting = true;
    }
    private void SetCastingFalse()
    {
        isCasting = false;
    }
}