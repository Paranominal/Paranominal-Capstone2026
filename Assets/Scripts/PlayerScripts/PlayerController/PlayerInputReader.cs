using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference sprintAction;
    [SerializeField] private InputActionReference slowWalkAction;
    [SerializeField] private InputActionReference lookActionMouse;
    [SerializeField] private InputActionReference lookActionGamepad;
    [SerializeField] private float gamepadLookSens = 100f;
    public bool canMove = true;
    public bool CanMove => canMove;

    [Header("Cursor")]
    [SerializeField] private CursorLockMode startLockMode = CursorLockMode.Locked;
    [SerializeField] private bool startCursorVisible = false;
    public bool debugMode;

    public Vector2 MoveInput => moveAction != null && moveAction.action != null
        ? moveAction.action.ReadValue<Vector2>()
        : Vector2.zero;
    public bool SprintInput => sprintAction != null && sprintAction.action != null
        ? sprintAction.action.IsPressed()
        : false;
    public bool SlowWalkInput => slowWalkAction != null && slowWalkAction.action != null
        ? slowWalkAction.action.IsPressed()
        : false;

    // public Vector2 LookInput => lookActionMouse != null && lookActionMouse.action != null
    //     ? lookActionMouse.action.ReadValue<Vector2>()
    //     : Vector2.zero;

    private Vector2 LookInputMouse => lookActionMouse != null && lookActionMouse.action != null
        ? lookActionMouse.action.ReadValue<Vector2>()
        : Vector2.zero;

    private Vector2 LookInputGamepad => lookActionGamepad != null && lookActionGamepad.action != null
        ? lookActionGamepad.action.ReadValue<Vector2>()
        : Vector2.zero;

    public Vector2 LookInput => Math.Abs(LookInputGamepad.x) > Math.Abs(LookInputMouse.x) || Math.Abs(LookInputGamepad.y) > Math.Abs(LookInputMouse.y)
        ? LookInputGamepad * gamepadLookSens
        : LookInputMouse;

    private void Start()
    {
        SetCursorState(startLockMode, startCursorVisible);
    }

    public void SetCursorState(CursorLockMode lockMode, bool visible)
    {
        Cursor.lockState = lockMode;
        Cursor.visible = visible;
    }
    private void Update()
    {
        if (debugMode) DoDebug();
        AnyInput();
    }
    public bool AnyInput()
    {
        if (moveAction.action.IsPressed()) return true;
        else if (sprintAction.action.IsPressed()) return true;
        else if (slowWalkAction.action.IsPressed()) return true;
        else if (lookActionMouse.action.IsPressed()) return true;
        else return false;
    }
    void DoDebug()
    {
        Debug.Log($"[{this}] LookInputMouse: {LookInputMouse}");
        Debug.Log($"[{this}] LookInputGamepad: {LookInputGamepad}");
        Debug.Log($"[{this}] LookInput: {LookInput}");
    }
}
