using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
public class SceneLoader : MonoBehaviour
{
    [SerializeField] private int sceneBuildIndex;
    public void ChangeScene()
    {
        SceneManager.LoadScene(sceneBuildIndex);
    }
}
