using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class SceneResetTEMP : MonoBehaviour
{
    [SerializeField] private string reset = "Reset";
    InputAction resetInput;
    [Tooltip("the Build Index of the scene you want to load")]
    [SerializeField] private int sceneBuildIndex;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        resetInput = InputSystem.actions.FindAction(reset);
    }

    // Update is called once per frame
    void Update()
    {
        if (resetInput.WasReleasedThisFrame()) SceneManager.LoadScene(sceneBuildIndex);    
    }
}
