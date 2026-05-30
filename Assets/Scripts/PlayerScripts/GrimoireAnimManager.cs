using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using NUnit.Framework;
using Unity.VisualScripting;
using UnityEditor.EditorTools;

public class GrimoireAnimManager : MonoBehaviour
{
    [SerializeField] private Animator grimoireAnimator;
    private bool isAnimating;
    [SerializeField] private ALTGrimoire altGrimoire;
    [SerializeField] private InputActionReference grimoireScroll;
    [SerializeField] private InputActionReference playerMove;
    [SerializeField] private InputActionReference scanAction;
    [SerializeField] private InputActionReference collectAction;
    [Tooltip("The duration in seconds, that the grimoire ignores the Close function after being opened")]
    [SerializeField] float stayOpenDuration = 1;
    private bool stayOpen;
    private bool playerIsMoving;
    private bool isOpen;
    private bool isCasting;
    public bool debugMode;

    void Start()
    {
        
    }

    void Update()
    {
        CheckPlayerMovement();
        OpenOnScroll();
        CheckCast();
        CheckIsMenuing();
        CheckScan();
        CheckCollect();

        if (debugMode) DoDebugLog();
    }
    private void OpenOnScroll()
    {
        if (Mathf.Abs(grimoireScroll.action.ReadValue<Vector2>().y) > 0) OpenGrimoire();
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
        StartCoroutine(StayOpen());
    }
    void CloseGrimoire()
    {
        StartCoroutine(DoClose());
    }
    private IEnumerator DoClose()
    {
        if (isAnimating) yield break;
        if (!isOpen) yield break;
        if (stayOpen) yield break;
        if (isCasting) yield break;

        grimoireAnimator.SetTrigger("close");
    }
    private IEnumerator StayOpen()
    {
        stayOpen = true;
        yield return new WaitForSeconds(stayOpenDuration);
        stayOpen = false;
    }
    private void CheckCast()
    {
        grimoireAnimator.SetBool("isCasting", isCasting);

        if (scanAction.action.IsPressed()) Cast();
        else isCasting = false;
    }
    private void CheckScan()
    {
        if (scanAction.action.WasPressedThisFrame()) OpenGrimoire();
    }
    private void CheckCollect()
    {
        if (collectAction.action.WasPressedThisFrame()) OpenGrimoire();
    }
    private void Cast()
    {
        StartCoroutine(DoCasting());
    }
    private IEnumerator DoCasting()
    {
        if (!scanAction.action.IsPressed()) yield break;
        if (!isOpen)
        {
            OpenGrimoire();
            yield break;
        }

        isCasting = true;
    }
    private void CheckPlayerMovement()
    {
        if (playerMove.action.ReadValue<Vector2>() != new Vector2(0, 0)) playerIsMoving = true;
        else playerIsMoving = false;

        if (playerIsMoving && !isCasting) CloseGrimoire();
    }
    private void CheckIsMenuing()
    {
        if (altGrimoire.grimoireActive && !isOpen) OpenGrimoire();
        //Debug.Log(altGrimoire.grimoireActive);
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
    private void DoDebugLog()
    {
        Debug.Log("isOpen: " + isOpen);
        Debug.Log("isAnimating: " + isAnimating);
        Debug.Log("Casting is pressed: " + scanAction.action.IsPressed());
        Debug.Log("isCasting: " + isCasting);
        Debug.Log("stayOpen: " + stayOpen);
    }
}