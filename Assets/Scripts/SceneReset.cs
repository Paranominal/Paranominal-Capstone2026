using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SceneReset : MonoBehaviour
{
    [SerializeField] private InputActionReference resetInput;
    [Tooltip("the Build Index of the scene you want to load. Typically 0")]
    [SerializeField] private int sceneBuildIndex;
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
        weaponInput = FindAnyObjectByType<WeaponInputReader>();
        playerInput = FindAnyObjectByType<PlayerInputReader>();
        Debug.Log(weaponInput, playerInput);
    }
    void Update()
    {
        PressReset();
        TimeoutTimer();

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
        if (playerInput.AnyInput()) return true;
        else if (weaponInput.AnyInput()) return true;
        else return false;
    }
    void TimerText()
    {
        if (time > timeOutSeconds - 5f) cachedResetTextColor.a += Time.unscaledDeltaTime / 5f;
        else cachedResetTextColor.a = 0;
        resettingText.color = cachedResetTextColor;
    }
    void DoReset()
    {
        Debug.Log($"Resetting Scene to [Scene: {sceneBuildIndex}]!");
        SceneManager.LoadScene(sceneBuildIndex);
    }
}
