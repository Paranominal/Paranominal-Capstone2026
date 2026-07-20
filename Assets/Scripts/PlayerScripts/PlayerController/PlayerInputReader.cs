using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference sprintAction;
    [SerializeField] private InputActionReference lookAction;

    [Header("Cursor")]
    [SerializeField] private CursorLockMode startLockMode = CursorLockMode.Locked;
    [SerializeField] private bool startCursorVisible = false;

    public Vector2 MoveInput => moveAction != null && moveAction.action != null
        ? moveAction.action.ReadValue<Vector2>()
        : Vector2.zero;
    public bool SprintInput => sprintAction != null && sprintAction.action != null
        ? sprintAction.action.IsPressed()
        : false;

    public Vector2 LookInput => lookAction != null && lookAction.action != null
        ? lookAction.action.ReadValue<Vector2>()
        : Vector2.zero;

    private void Start()
    {
        SetCursorState(startLockMode, startCursorVisible);
    }

    public void SetCursorState(CursorLockMode lockMode, bool visible)
    {
        Cursor.lockState = lockMode;
        Cursor.visible = visible;
    }
}
