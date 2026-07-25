using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class SceneReset : MonoBehaviour
{
    [SerializeField] private InputActionReference resetInput;
    [Tooltip("the Build Index of the scene you want to load")]
    [SerializeField] private int sceneBuildIndex;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (resetInput.action.WasReleasedThisFrame()) SceneManager.LoadScene(sceneBuildIndex);    
    }
}
