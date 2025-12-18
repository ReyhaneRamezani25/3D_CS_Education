using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneReturnManager
{
    private static string lastSceneName;

    public static void SetLastScene(string sceneName)
    {
        lastSceneName = sceneName;
    }

    public static void LoadLastScene()
    {
        if (!string.IsNullOrEmpty(lastSceneName))
        {
            SceneManager.LoadScene(lastSceneName);
        }
        else
        {
            Debug.LogWarning("No last scene recorded.");
        }
    }
}
