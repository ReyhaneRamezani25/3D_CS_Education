using UnityEngine;
using UnityEngine.EventSystems;

public class CameraYawDrag : MonoBehaviour
{
    [Header("Drag → Yaw")]
    [Tooltip("Degrees of rotation for dragging across the full screen width.")]
    public float degreesPerFullSwipe = 120f;
    [Tooltip("Maximum left yaw (negative).")]
    public float maxYawLeft  = 10f;
    [Tooltip("Maximum right yaw (positive).")]
    public float maxYawRight = 15f;

    [Header("Smoothing")]
    [Tooltip("0 means no smoothing. 8–12 feels smooth and responsive.")]
    public float smooth = 10f;

    [Header("UI")]
    [Tooltip("If the pointer is over UI, do not start rotation.")]
    public bool ignoreWhenPointerOverUI = true;

    Quaternion baseRot;
    float targetOffset;
    float currentOffset;
    float offsetVel;
    bool dragging;
    Vector2 lastPos;

    void Start()
    {
        baseRot = transform.localRotation;
        targetOffset = currentOffset = 0f;
    }

    void Update()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonDown(1))
        {
            if (!ignoreWhenPointerOverUI || !IsPointerOverUI())
            {
                dragging = true;
                lastPos = Input.mousePosition;
            }
        }
        if (Input.GetMouseButton(1) && dragging)
        {
            float dx = (Input.mousePosition.x - lastPos.x);
            lastPos = Input.mousePosition;
            ApplyDelta(dx);
        }
        if (Input.GetMouseButtonUp(1)) dragging = false;
#else
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);

            if (t.phase == TouchPhase.Began)
            {
                if (!ignoreWhenPointerOverUI || !EventSystem.current.IsPointerOverGameObject(t.fingerId))
                {
                    dragging = true;
                    lastPos = t.position;
                }
            }
            else if ((t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary) && dragging)
            {
                float dx = (t.position.x - lastPos.x);
                lastPos = t.position;
                ApplyDelta(dx);
            }
            else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            {
                dragging = false;
            }
        }
#endif

        if (smooth <= 0f) currentOffset = targetOffset;
        else currentOffset = Mathf.SmoothDampAngle(currentOffset, targetOffset, ref offsetVel, 1f / smooth);

        transform.localRotation = baseRot * Quaternion.Euler(0f, currentOffset, 0f);
    }

    void ApplyDelta(float dxPixels)
    {
        float degPerPixel = (degreesPerFullSwipe / Mathf.Max(1f, Screen.width));
        targetOffset += dxPixels * degPerPixel;
        targetOffset = Mathf.Clamp(targetOffset, -maxYawLeft, maxYawRight);
    }

    bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
        return EventSystem.current.IsPointerOverGameObject();
    }
}
