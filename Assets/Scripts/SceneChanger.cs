using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    public string nextScene;

    public void GoToNextScene()
    {
        string current = SceneManager.GetActiveScene().name;
        SceneReturnManager.SetLastScene(current);
        SceneManager.LoadScene(nextScene);
    }
}
