using UnityEngine;
using UnityEngine.EventSystems;

public class Joystick : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    public RectTransform background;
    public RectTransform handle;
    [Range(0f, 1f)] public float handleRange = 1f;

    private Vector2 input = Vector2.zero;
    private Camera uiCam;

    void Awake()
    {
        var canvas = GetComponentInParent<Canvas>();
        uiCam = (canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;
    }

    public void OnPointerDown(PointerEventData e) => OnDrag(e);

    public void OnDrag(PointerEventData e)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(background, e.position, uiCam, out var local))
        {
            var size = background.sizeDelta;
            Vector2 norm = new Vector2(local.x / (size.x * 0.5f), local.y / (size.y * 0.5f));
            input = (norm.magnitude > 1f) ? norm.normalized : norm;
            handle.anchoredPosition = input * (size * 0.5f) * handleRange;
        }
    }

    public void OnPointerUp(PointerEventData e)
    {
        input = Vector2.zero;
        handle.anchoredPosition = Vector2.zero;
    }

    public float Horizontal => input.x;
    public float Vertical   => input.y;
    public Vector2 Direction => input;
}
