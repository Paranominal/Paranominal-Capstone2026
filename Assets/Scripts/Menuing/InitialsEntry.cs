using UnityEngine;
using UnityEngine.InputSystem;

public class InitialsEntry : MonoBehaviour
{
    [Header("References")]
    [Tooltip("one single-column counter per letter, left to right")]
    [SerializeField] private RollingCounter[] slots;
    [Tooltip("highlight for each slot, same order as slots")]
    [SerializeField] private GameObject[] slotCursors;

    [Header("Input")]
    [SerializeField] private InputActionReference upAction;
    [SerializeField] private InputActionReference downAction;
    [SerializeField] private InputActionReference leftAction;
    [SerializeField] private InputActionReference rightAction;
    [SerializeField] private InputActionReference confirmAction;

    public event System.Action OnConfirmed;

    private int selected;

    public string CurrentName // uses each reel's target, so confirming mid-roll still submits what was picked
    {
        get
        {
            string name = "";
            foreach (RollingCounter slot in slots)
                name += slot.TargetSymbol;
            return name;
        }
    }

    private void Start()
    {
        Select(0);
    }

    private void OnEnable()
    {
        Bind(upAction, HandleUp);
        Bind(downAction, HandleDown);
        Bind(leftAction, HandleLeft);
        Bind(rightAction, HandleRight);
        Bind(confirmAction, HandleConfirm);

        if (Keyboard.current != null)
            Keyboard.current.onTextInput += HandleTextInput;
    }

    private void OnDisable()
    {
        Unbind(upAction, HandleUp);
        Unbind(downAction, HandleDown);
        Unbind(leftAction, HandleLeft);
        Unbind(rightAction, HandleRight);
        Unbind(confirmAction, HandleConfirm);

        if (Keyboard.current != null)
            Keyboard.current.onTextInput -= HandleTextInput;
    }

    private void HandleUp(InputAction.CallbackContext context) => slots[selected].RollBy(1);
    private void HandleDown(InputAction.CallbackContext context) => slots[selected].RollBy(-1);
    private void HandleLeft(InputAction.CallbackContext context) => Select(selected - 1);
    private void HandleRight(InputAction.CallbackContext context) => Select(selected + 1);

    private void HandleConfirm(InputAction.CallbackContext context)
    {
        if (selected < slots.Length - 1)
        {
            Select(selected + 1); // confirm moves along, and only submits from the last slot
            return;
        }
        OnConfirmed?.Invoke();
    }

    private void HandleTextInput(char character)
    {
        char letter = char.ToUpperInvariant(character);
        if (slots[selected].Symbols.IndexOf(letter) < 0) return; // ignores anything not on the reel, including enter and backspace

        slots[selected].RollToSymbol(letter);
        Select(selected + 1);
    }

    private void Select(int index)
    {
        selected = Mathf.Clamp(index, 0, slots.Length - 1);
        for (int i = 0; i < slotCursors.Length; i++)
            slotCursors[i].SetActive(i == selected);
    }

    private void Bind(InputActionReference reference, System.Action<InputAction.CallbackContext> handler)
    {
        reference.action.performed += handler;
        reference.action.Enable();
    }

    private void Unbind(InputActionReference reference, System.Action<InputAction.CallbackContext> handler)
    {
        reference.action.performed -= handler;
        reference.action.Disable();
    }
}
