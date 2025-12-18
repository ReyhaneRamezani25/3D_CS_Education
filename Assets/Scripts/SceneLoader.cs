using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadNextScene(string sceneName)
    {
        
        SceneHistory.PushCurrent();

        
        SceneManager.LoadScene(sceneName);
    }
}
