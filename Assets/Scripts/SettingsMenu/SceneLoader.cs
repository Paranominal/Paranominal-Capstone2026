using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
public class SceneLoader : MonoBehaviour
{
    [SerializeField] private string sceneName;
    public void ChangeScene()
    {
        SceneManager.LoadScene(sceneName);
    }
}
