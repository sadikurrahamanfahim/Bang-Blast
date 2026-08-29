using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScene : MonoBehaviour
{
    [SerializeField]
    private string sceneName = "MainGame";

    public void changeScene()
    {
        SceneManager.LoadScene(sceneName);
    }
}
