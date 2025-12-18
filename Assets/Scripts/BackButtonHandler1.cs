using UnityEngine;

public class BackButton : MonoBehaviour
{
    public string fallbackScene;

    void Update()
    {
       
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            GoBack();
        }
    }

    public void GoBack()
    {
        SceneHistory.GoBack(fallbackScene);
    }
}
