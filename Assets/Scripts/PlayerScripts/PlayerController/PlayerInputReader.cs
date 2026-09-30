using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference sprintAction;
    [SerializeField] private InputActionReference slowWalkAction;
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private InputActionReference dashAction;
    [SerializeField] private InputActionReference lookActionMouse;
    [SerializeField] private InputActionReference lookActionGamepad;
    [SerializeField] private float gamepadLookSens = 100f;

    //for future glyph icon implementation
    //need to assign instance in Awake() when continuing this
    public static PlayerInputReader Instance { get; private set; }
    public static event Action<bool> OnActiveDeviceChanged;
    public bool IsGamepadActive { get; private set; }
    //threshold for detecting mouse movement
    private const float DeviceMouseMoveThreshold = 0.01f; 

    private InputAction interactAction;
    public InputActionReference MoveAction => moveAction;
    public InputActionReference SprintAction => sprintAction;
    public InputActionReference SlowWalkActon => slowWalkAction;
    public InputActionReference JumpAction => jumpAction;
    public InputActionReference DashAction => dashAction;
    public InputActionReference LookAction => lookActionMouse.action.ReadValue<Vector2>().sqrMagnitude >= lookActionGamepad.action.ReadValue<Vector2>().sqrMagnitude 
        ? lookActionMouse
        : lookActionGamepad;

    private bool canMove = true;
    public bool CanMove => canMove;
    private bool canLook = true;
    public bool CanLook => canLook;

    [Header("Cursor")]
    [SerializeField] private CursorLockMode startLockMode = CursorLockMode.Locked;
    [SerializeField] private bool startCursorVisible = false;
    public bool debugMode;

    [Header("Aim Assist")]
    [SerializeField] private float gamepadActiveThreshold = 0.05f;

    public Vector2 MoveInput => moveAction != null && moveAction.action != null  && CanMove
        ? moveAction.action.ReadValue<Vector2>()
        : Vector2.zero;
    public bool SprintInput => sprintAction != null && sprintAction.action != null && CanMove
        ? sprintAction.action.IsPressed()
        : false;
    public bool SlowWalkInput => slowWalkAction != null && slowWalkAction.action != null && CanMove
        ? slowWalkAction.action.IsPressed()
        : false;
    public bool jumpInput => jumpAction != null && jumpAction.action != null && CanMove
        ? jumpAction.action.IsPressed()
        : false;

    public bool dashInput => dashAction != null && dashAction.action != null && CanMove
        ? dashAction.action.IsPressed()
        : false;

    // public Vector2 LookInput => lookActionMouse != null && lookActionMouse.action != null
    //     ? lookActionMouse.action.ReadValue<Vector2>()
    //     : Vector2.zero;

    public bool IsUsingGamepad => LookInputGamepad.sqrMagnitude > gamepadActiveThreshold * gamepadActiveThreshold;

    public float GamepadLookMagnitude => LookInputGamepad.magnitude;
    public Vector2 GamepadLookVector => LookInputGamepad;

    private Vector2 LookInputMouse => lookActionMouse != null && lookActionMouse.action != null && CanLook
        ? lookActionMouse.action.ReadValue<Vector2>()
        : Vector2.zero;

    private Vector2 LookInputGamepad => lookActionGamepad != null && lookActionGamepad.action != null && CanLook
        ? lookActionGamepad.action.ReadValue<Vector2>()
        : Vector2.zero;

    public Vector2 LookInput => (Math.Abs(LookInputGamepad.x) > Math.Abs(LookInputMouse.x) || Math.Abs(LookInputGamepad.y) > Math.Abs(LookInputMouse.y)) && CanLook
        ? LookInputGamepad * gamepadLookSens
        : LookInputMouse;

    private void Start()
    {
        SetCursorState(startLockMode, startCursorVisible);
    }

    public void InputLock(bool enabled) // if InputLock(true) is called, it disables all movement from the player reader
    {
        canMove = !enabled;
        if (interactAction != null)
        {
            if (enabled) interactAction.Disable();
            else interactAction.Enable();
        }
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
        DetectActiveDevice();
    }

    //from here on up till setactivedevice, is for detecting if the player is 
    //using a gamepad or keyboard/mouse, and firing an event when it changes
    //this is for icon glyphs if needed in the future
    private void DetectActiveDevice()
    {
        if (Gamepad.current != null && IsGamepadInUseRaw(Gamepad.current)) SetActiveDevice(true);
        if (IsKeyboardOrMouseInUseRaw()) SetActiveDevice(false);
    }

    private bool IsGamepadInUseRaw(Gamepad gp)
    {
        return gp.buttonSouth.wasPressedThisFrame
            || gp.buttonNorth.wasPressedThisFrame
            || gp.buttonEast.wasPressedThisFrame
            || gp.buttonWest.wasPressedThisFrame
            || gp.startButton.wasPressedThisFrame
            || gp.selectButton.wasPressedThisFrame
            || gp.leftShoulder.wasPressedThisFrame
            || gp.rightShoulder.wasPressedThisFrame
            || gp.leftTrigger.wasPressedThisFrame
            || gp.rightTrigger.wasPressedThisFrame
            || gp.dpad.ReadValue() != Vector2.zero
            || gp.leftStick.ReadValue().sqrMagnitude > gamepadActiveThreshold * gamepadActiveThreshold
            || gp.rightStick.ReadValue().sqrMagnitude > gamepadActiveThreshold * gamepadActiveThreshold;
    }

    private bool IsKeyboardOrMouseInUseRaw()
    {
        bool keyboard = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
        bool mouseButton = Mouse.current != null &&
            (Mouse.current.leftButton.wasPressedThisFrame ||
             Mouse.current.rightButton.wasPressedThisFrame ||
             Mouse.current.middleButton.wasPressedThisFrame);
        bool mouseMoved = Mouse.current != null &&
            Mouse.current.delta.ReadValue().sqrMagnitude > DeviceMouseMoveThreshold * DeviceMouseMoveThreshold;

        return keyboard || mouseButton || mouseMoved;
    }
    
    //changes input device
    private void SetActiveDevice(bool gamepad)
    {
        if (IsGamepadActive == gamepad) return;
        IsGamepadActive = gamepad;
        OnActiveDeviceChanged?.Invoke(gamepad);
    }

    public bool AnyInput()
    {
        if (moveAction.action.IsPressed()) return true;
        else if (sprintAction.action.IsPressed()) return true;
        else if (slowWalkAction.action.IsPressed()) return true;
        else if (jumpAction.action.IsPressed()) return true;
        else if (lookActionMouse.action.IsPressed()) return true;
        else if (dashAction != null && dashAction.action != null && dashAction.action.IsPressed()) return true;
        else return false;
    }
    void DoDebug()
    {
        Debug.Log($"[{this}] LookInputMouse: {LookInputMouse}");
        Debug.Log($"[{this}] LookInputGamepad: {LookInputGamepad}");
        Debug.Log($"[{this}] LookInput: {LookInput}");
    }
}