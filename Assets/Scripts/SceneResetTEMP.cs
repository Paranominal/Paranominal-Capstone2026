using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SceneResetTEMP : MonoBehaviour
{
    [SerializeField] private string reset = "Reset";
    InputAction resetInput;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
         resetInput = InputSystem.actions.FindAction(reset);
    }

    // Update is called once per frame
    void Update()
    {
        if (resetInput.WasReleasedThisFrame()) SceneManager.LoadScene(0);    }
}
