using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using NUnit.Framework;
using Unity.VisualScripting;
using UnityEngine.UIElements;

public class GrimoireAnimManager : MonoBehaviour
{
    [SerializeField] private PlayerInputReader playerInputReader;
    [SerializeField] private Animator grimoireAnimator;
    [SerializeField] private ALTGrimoire altGrimoire;
    [SerializeField] private InputActionReference grimoireScroll;
    [SerializeField] private InputActionReference grimoireMenu;
    [SerializeField] private InputActionReference playerMove;
    [SerializeField] private InputActionReference playerShoot;
    [SerializeField] private InputActionReference scanAction;
    [SerializeField] private InputActionReference collectAction;
    [Tooltip("The duration in seconds, that the grimoire ignores the Close function after being opened")]
    [SerializeField] float stayOpenDuration = 1;
    private bool stayOpen;
    private bool isAnimating;
    private bool isPlayerMoving;
    private bool isPlayerShooting;
    private bool isOpen;
    private bool isCasting;
    private bool isMenuing;
    [SerializeField] private bool startOpen;
    [SerializeField] private GrimoirePositioner lowerGrimoire;
    public bool debugMode;

    void Start()
    {
        isOpen = startOpen;
    }
    void Update()
    {
        if (!playerInputReader.canMove) return;
        
        ControlBools();

        OpenOnScroll();
        CloseOnMove();
        CastOnHold();
        OpenIfMenuing();
        OpenOnCollect();
        MenuOpen();
        MenuClose();

        if (lowerGrimoire != null) DoLowerGrimoire();
        if (debugMode) DoDebugLog();
    }
    void ControlBools()
    {
        isMenuing = altGrimoire.grimoireActive;

        if (scanAction.action.IsPressed()) isCasting = true;
        else isCasting = false;
        grimoireAnimator.SetBool("isCasting", isCasting);

        if (playerMove.action.IsPressed()) isPlayerMoving = true;
        else isPlayerMoving = false;
        if (playerShoot.action.IsPressed()) isPlayerShooting = true;
        else isPlayerShooting = false;
    }
    // Input to Action
    private void OpenOnScroll()
    {   //return conditions
        if (isOpen) return;
        if (isAnimating) return;
        if (isMenuing) return;
        if (isPlayerMoving) return;
        if (isCasting) return;

        if (Mathf.Abs(grimoireScroll.action.ReadValue<Vector2>().y) > 0) OpenGrimoire();
    }
    private void CloseOnMove()
    {   //return conditions
        if (!isOpen) return;
        if (isAnimating) return;
        if (isMenuing) return;
        if (isCasting) return;
        if (stayOpen) return;

        if (isPlayerMoving) CloseGrimoire();
    }
    private void CastOnHold()
    {   //return conditions
        if (isAnimating) return;
        if (isMenuing) return;

        if (scanAction.action.IsPressed()) Scan();
    }
    private void OpenIfMenuing()
    {
        if (isOpen) return;

        if (isMenuing) OpenGrimoire();
    }
    private void OpenOnCollect()
    {
        if (isOpen) return;
        if (isCasting) return;
        if (isAnimating) return;

        if (collectAction.action.WasPressedThisFrame()) OpenGrimoire();
        if (collectAction.action.WasPressedThisFrame() && debugMode) Debug.Log($"[{this}] | collect action was pressed");
    }
    private void MenuOpen()
    {
        if (grimoireMenu.action.WasPressedThisFrame() && !isMenuing)
        {
            OpenGrimoire();
            OpenMenu();
        } 
    }
    private void MenuClose()
    {
        if (grimoireMenu.action.WasPressedThisFrame() && isMenuing)
        {
            CloseMenu();
        } 
    }
    // Actions
    void OpenGrimoire()
    {
        grimoireAnimator.SetTrigger("open");
        StartCoroutine(StayOpen());
    }
    void CloseGrimoire()
    {
        grimoireAnimator.SetTrigger("close");
    }
    void Scan()
    {
        if (isOpen) grimoireAnimator.SetTrigger("castFromOpen");
        else grimoireAnimator.SetTrigger("castFromClosed");
    }
    void OpenMenu()
    {
        grimoireAnimator.SetTrigger("openMenu");
    }
    void CloseMenu()
    {
        grimoireAnimator.SetTrigger("closeMenu");
    }
    private IEnumerator StayOpen()
    {
        stayOpen = true;
        yield return new WaitForSeconds(stayOpenDuration);
        stayOpen = false;
    }
    // Lower Grimoire component control
    private void DoLowerGrimoire()
    {
        if (isMenuing) lowerGrimoire.Menu();
        else if (isPlayerMoving | isPlayerShooting) lowerGrimoire.Lower();
        else if (isOpen) lowerGrimoire.Open();
        else lowerGrimoire.Raise();
    }
    // Animation Events call these
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
    // Debugging
    private void DoDebugLog()
    {
        Debug.Log("isOpen: " + isOpen);
        Debug.Log("isAnimating: " + isAnimating);
        Debug.Log("isCasting: " + isCasting);
        Debug.Log("stayOpen: " + stayOpen);
    }

    // //comment this out later. its just for checking menu alignment while making animations
    // [SerializeField] private GameObject bookRight;
    // [SerializeField] private GameObject bookLeft;
    // void OnValidate()
    // {
    //     if (startOpen)
    //     {
    //         bookRight.transform.localEulerAngles = new Vector3(0, 0, 10);
    //         bookLeft.transform.localEulerAngles = new Vector3(0, 0, -10);
    //         gameObject.transform.localPosition = new Vector3(0.1f,0,0); 
    //         gameObject.transform.localEulerAngles = Vector3.zero;
    //     }
    //     else
    //     {
    //         bookRight.transform.localEulerAngles = new Vector3(0, 0, 90);
    //         bookLeft.transform.localEulerAngles = new Vector3(0, 0, -90);
    //         gameObject.transform.localPosition = Vector3.zero; 
    //         gameObject.transform.localEulerAngles = new Vector3(0, 0, -90);
    //     }
    // }
}