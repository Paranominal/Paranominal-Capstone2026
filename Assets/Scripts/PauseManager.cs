using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Summary: Scene singleton that centralises pausing. Anything that pauses the game (Grimoire, dialogue) goes through here.
// Handles timescale, cursor state and action map switching. Registered persistent actions stay enabled while paused.
// EDIT (grimoire-pause): added Instance, UI map handling, persistent actions and ResumedThisFrame. IsPaused is now actually set on pause.
public class PauseManager : MonoBehaviour
{
    // EDIT (grimoire-pause): scene singleton access.
    public static PauseManager Instance { get; private set; }

    private bool isPaused;
    public bool IsPaused => isPaused;

    // EDIT (grimoire-pause): true on the frame the game was resumed. Stops a key that closes one pausing system
    // (e.g. Escape closing dialogue) from reopening another (the Grimoire) on the same frame.
    private int resumeFrame = -1;
    public bool ResumedThisFrame => resumeFrame == Time.frameCount;

    [SerializeField] private string playerActionMapName = "Player";
    [SerializeField] private string grimoireActionMapName = "GrimoireUI";
    // EDIT (grimoire-pause): UI map handling moved here from ALTGrimoire.
    [SerializeField] private string uiActionMapName = "UI";
    [SerializeField] private PlayerInputReader playerInputReader;

    // EDIT (grimoire-pause): actions re-enabled after every map switch so they work paused or unpaused.
    private readonly HashSet<InputAction> persistentActions = new HashSet<InputAction>();

    // EDIT (grimoire-pause): singleton setup, and reset to an unpaused state on scene load (previously done by PauseMenu.OnEnable).
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Found more than one PauseManager in the scene");
            return;
        }
        Instance = this;

        if (playerInputReader == null)
            playerInputReader = FindAnyObjectByType<PlayerInputReader>();

        ApplyUnpausedState();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void PauseGame()
    {
        // Set Time.timeScale to 0 to pause gameplay
        Time.timeScale = 0;
        SetMapEnabled(playerActionMapName, false);
        SetMapEnabled(grimoireActionMapName, false);
        SetMapEnabled(uiActionMapName, true);
        EnablePersistentActions();
        SetCursorModeLocked(false);
        isPaused = true;
    }

    public void ResumeGame()
    {
        ApplyUnpausedState();
        resumeFrame = Time.frameCount;
    }

    // EDIT (grimoire-pause): registers an action that must keep working while paused (e.g. the Grimoire toggles).
    public void RegisterPersistentAction(InputAction action)
    {
        if (action == null) return;

        persistentActions.Add(action);
        action.Enable();
    }

    // Summary: Shared by ResumeGame and scene start. Separate so the scene start doesn't count as a resume frame.
    private void ApplyUnpausedState()
    {
        // Set Time.timeScale back to 1 to resume gameplay
        Time.timeScale = 1;
        SetMapEnabled(playerActionMapName, true);
        SetMapEnabled(grimoireActionMapName, true);
        SetMapEnabled(uiActionMapName, false);
        EnablePersistentActions();
        SetCursorModeLocked(true);
        isPaused = false;
    }

    private void SetMapEnabled(string mapName, bool enabled)
    {
        InputActionMap map = InputSystem.actions.FindActionMap(mapName);
        if (map == null) return;

        if (enabled) map.Enable();
        else map.Disable();
    }

    // Disabling a map disables its actions, so persistent ones are re-enabled after every switch.
    private void EnablePersistentActions()
    {
        foreach (InputAction action in persistentActions)
            action.Enable();
    }

    void SetCursorModeLocked(bool mode) //true for locked, false for unlocked
    {
        if (mode) {
            if (playerInputReader != null) playerInputReader.SetCursorState(CursorLockMode.Locked, false);
            else {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked; } }
        else {
            if (playerInputReader != null) playerInputReader.SetCursorState(CursorLockMode.None, true);
            else {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None; } }
    }
}
