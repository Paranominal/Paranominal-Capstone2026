using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class SceneResetTEMP : MonoBehaviour
{
    [SerializeField] private string reset = "Reset";
    InputAction resetInput;
    private Scene currentScene;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        resetInput = InputSystem.actions.FindAction(reset);
        currentScene = SceneManager.GetActiveScene();
    }

    // Update is called once per frame
    void Update()
    {
        if (resetInput.WasReleasedThisFrame()) SceneManager.LoadScene(currentScene.buildIndex);    
    }
}
