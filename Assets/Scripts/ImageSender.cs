using UnityEngine;
using UnityEngine.SceneManagement;

public class ImageSender : MonoBehaviour
{
    public Sprite spriteToSend;
    public string targetScene;

    public void SendImage()
    {
        ImageTransfer.SelectedSprite = spriteToSend;
        SceneHistory.PushCurrent();
        SceneManager.LoadScene(targetScene);
    }
}
