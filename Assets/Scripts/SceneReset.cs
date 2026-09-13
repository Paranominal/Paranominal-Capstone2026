using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SceneReset : MonoBehaviour
{
    [SerializeField] private InputActionReference resetInput;
    [Tooltip("the Build Index of the scene you want to load. Typically 0")]
    [SerializeField] private int sceneBuildIndex;
    [SerializeField] private bool doTimeout = true;
    [SerializeField] private float timeOutSeconds = 30;
    [SerializeField] private WeaponInputReader weaponInput;
    [SerializeField] private PlayerInputReader playerInput;
    [SerializeField] private TextMeshProUGUI resettingText;
    [SerializeField] private bool logTimer;
    private float time;
    private Color cachedResetTextColor;
    void Start()
    {
        cachedResetTextColor = resettingText.color;
        TimerText(); // trigger once to turn off at start
        CheckForInputReaders();
    }
    void Update()
    {
        PressReset();
        if (doTimeout) TimeoutTimer();

        if (logTimer) Debug.Log(time);
    }

    void PressReset()
    {
        if (resetInput.action.WasReleasedThisFrame()) DoReset();
    }
    void TimeoutTimer()
    {   
        if (AnyInput()) time = 0f;

        if (time >= timeOutSeconds) DoReset(); // timer
        else time += Time.unscaledDeltaTime;
        TimerText();
    }
    private bool AnyInput()
    {
        if (playerInput != null && playerInput.AnyInput()) return true;
        else if (weaponInput != null &&weaponInput.AnyInput()) return true;
        else return false;
    }
    void TimerText()
    {
        if (doTimeout && time > timeOutSeconds - 5f) cachedResetTextColor.a += Time.unscaledDeltaTime / 5f;
        else cachedResetTextColor.a = 0;
        resettingText.color = cachedResetTextColor;
    }
    void DoReset()
    {
        Debug.Log($"Resetting Scene to [Scene: {sceneBuildIndex}]!");
        SceneManager.LoadScene(sceneBuildIndex);
    }
    public void ToggleTimeout(bool toggle)
    {
        doTimeout = toggle;
        // reset timer when enabling so users get the full timeout window
        if (doTimeout) time = 0f;
        TimerText();
    }
    void CheckForInputReaders()
    {
        if (playerInput == null)
        {
            Debug.LogWarning($"[{this}] No playerInput was set! This may be a mistake. Attempting to find one...");
            playerInput = FindAnyObjectByType<PlayerInputReader>();
            if (playerInput == null) Debug.LogWarning("[Reset Manager] No playerInput found.");
            else Debug.LogWarning($"[Reset Manager] playerInput found! [{playerInput}]");
        }
        if (weaponInput == null)
        {
            Debug.LogWarning($"[{this}] No weaponInput was set! This may be a mistake. Attempting to find one...");
            weaponInput = FindAnyObjectByType<WeaponInputReader>();
            if (weaponInput == null) Debug.LogWarning("[Reset Manager] No weaponInput found.");
            else Debug.LogWarning($"[Reset Manager] weaponInput found! [{weaponInput}]");
        }
    }
}
