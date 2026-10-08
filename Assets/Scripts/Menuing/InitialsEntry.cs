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

    [Header("Hold To Repeat")]
    [Tooltip("time between steps while up/down is held")]
    [SerializeField] private float stepInterval = 0.1f;

    private float nextStepTime;

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
        upAction.action.Enable();
        downAction.action.Enable();
        Bind(leftAction, HandleLeft);
        Bind(rightAction, HandleRight);
        Bind(confirmAction, HandleConfirm);

        if (Keyboard.current != null)
            Keyboard.current.onTextInput += HandleTextInput;
    }

    private void OnDisable()
    {
        upAction.action.Disable();
        downAction.action.Disable();
        Unbind(leftAction, HandleLeft);
        Unbind(rightAction, HandleRight);
        Unbind(confirmAction, HandleConfirm);

        if (Keyboard.current != null)
            Keyboard.current.onTextInput -= HandleTextInput;
    }

    private void Update()
    {
        int direction = 0;
        if (upAction.action.IsPressed()) direction = -1;
        else if (downAction.action.IsPressed()) direction = 1;

        if (direction != 0 && Time.unscaledTime >= nextStepTime) // scaled time is borked in this scene????
            {
            slots[selected].RollBy(direction);
            nextStepTime = Time.unscaledTime + stepInterval;
        }
    }

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
