using UnityEngine;

public class ForceLandscapeScene : MonoBehaviour
{
    private ScreenOrientation previousOrientation;

    void OnEnable()
    {
        previousOrientation = Screen.orientation;

        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;

        Screen.orientation = ScreenOrientation.LandscapeLeft;
    }

    void OnDisable()
    {
        Screen.orientation = previousOrientation;

        Screen.autorotateToPortrait = true;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = false;
        Screen.autorotateToLandscapeRight = false;

        Screen.orientation = ScreenOrientation.Portrait;
    }
}
