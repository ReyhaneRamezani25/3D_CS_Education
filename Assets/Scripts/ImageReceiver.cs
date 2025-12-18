using UnityEngine;
using UnityEngine.UI;

public class ImageReceiver : MonoBehaviour
{
    public Image targetImage;

    private void Start()
    {
        if (ImageTransfer.SelectedSprite != null)
        {
            targetImage.sprite = ImageTransfer.SelectedSprite;
        }
    }
}
