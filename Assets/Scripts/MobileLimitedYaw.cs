using UnityEngine;
using UnityEngine.EventSystems;

public class MobileYawAndMoveContinuous : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Yaw (Finger #1)")]
    public float maxYawAngle = 25f;
    public bool  autoCenterFromCurrent = true;
    public float centerYaw = 200.278f;
    public float yawDegreesPerPixel = 0.15f;
    public bool  invertHorizontal = false;
    public float yawSmoothTime = 0.08f;

    [Header("Movement (Finger #2)")]
    public Space moveSpace = Space.Self;
    [Tooltip("Movement speed per second (scene units).")]
    public float moveSpeed = 10f;
    [Tooltip("Drag sensitivity (lower = faster response).")]
    public float pixelsPerUnit = 50f;
    public bool lockYToStart = true;

    [Header("UI")]
    public bool ignoreWhenPointerOverUI = true;

    private Quaternion _baseRotation;
    private float _yawOffset, _yawOffsetTarget, _yawVel;
    private Vector2 _lastPosFinger1;
    private int _finger1 = -1;

    private int _finger2 = -1;
    private Vector2 _lastPosFinger2;
    private Vector2 _moveDir;
    private float _startY;

    void Awake()
    {
        if (!target) target = transform;
        _startY = target.position.y;

        _baseRotation = target.rotation;
        if (autoCenterFromCurrent)
        {
            _yawOffset = 0f;
            _yawOffsetTarget = 0f;
        }
        else
        {
            _baseRotation = Quaternion.Euler(target.eulerAngles.x, centerYaw, target.eulerAngles.z);
            _yawOffset = 0f;
            _yawOffsetTarget = 0f;
        }
    }

    void Update()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouseAsTwoFingers();
#else
        HandleTouchesTwoFingers();
#endif

        _yawOffsetTarget = Mathf.Clamp(_yawOffsetTarget, -maxYawAngle, +maxYawAngle);
        _yawOffset = (yawSmoothTime > 0f)
            ? Mathf.SmoothDampAngle(_yawOffset, _yawOffsetTarget, ref _yawVel, yawSmoothTime)
            : _yawOffsetTarget;
    }

    void LateUpdate()
    {
        Vector3 upAxis = Vector3.up;
        target.rotation = Quaternion.AngleAxis(_yawOffset, upAxis) * _baseRotation;

        if (_moveDir.sqrMagnitude > 0.01f)
        {
            Vector3 fwd, right;
            if (moveSpace == Space.Self)
            {
                fwd = target.forward; fwd.y = 0f; fwd.Normalize();
                right = target.right; right.y = 0f; right.Normalize();
            }
            else
            {
                fwd = Vector3.forward; right = Vector3.right;
            }

            Vector3 move = (right * _moveDir.x + fwd * _moveDir.y) * moveSpeed * Time.deltaTime;
            Vector3 pos = target.position + move;
            if (lockYToStart) pos.y = _startY;
            target.position = pos;
        }
    }

    void HandleMouseAsTwoFingers()
    {
        if (Input.GetMouseButtonDown(0)) { _finger1 = 0; _lastPosFinger1 = Input.mousePosition; }
        if (Input.GetMouseButton(0) && _finger1 == 0)
        {
            Vector2 now = Input.mousePosition;
            Vector2 delta = now - _lastPosFinger1;
            _lastPosFinger1 = now;
            float sign = invertHorizontal ? -1f : 1f;
            _yawOffsetTarget += sign * delta.x * yawDegreesPerPixel;
        }
        if (Input.GetMouseButtonUp(0) && _finger1 == 0) _finger1 = -1;

        if (Input.GetMouseButtonDown(1)) { _finger2 = 1; _lastPosFinger2 = Input.mousePosition; _moveDir = Vector2.zero; }
        if (Input.GetMouseButton(1) && _finger2 == 1)
        {
            Vector2 now = Input.mousePosition;
            Vector2 delta = now - _lastPosFinger2;
            _lastPosFinger2 = now;

            _moveDir = new Vector2(delta.x / pixelsPerUnit, delta.y / pixelsPerUnit);
            _moveDir = Vector2.ClampMagnitude(_moveDir, 1f);
        }
        if (Input.GetMouseButtonUp(1) && _finger2 == 1) { _finger2 = -1; _moveDir = Vector2.zero; }
    }

    void HandleTouchesTwoFingers()
    {
        if (Input.touchCount == 0) { _finger1 = -1; _finger2 = -1; _moveDir = Vector2.zero; return; }

        int f1 = int.MaxValue, f2 = int.MaxValue;
        int i1 = -1, i2 = -1;
        for (int i = 0; i < Input.touchCount; i++)
        {
            var t = Input.GetTouch(i);
            if (t.fingerId < f1) { f2 = f1; i2 = i1; f1 = t.fingerId; i1 = i; }
            else if (t.fingerId < f2) { f2 = t.fingerId; i2 = i; }
        }

        if (i1 != -1)
        {
            var t = Input.GetTouch(i1);
            if (t.phase == TouchPhase.Began) _lastPosFinger1 = t.position;
            if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
            {
                Vector2 delta = t.position - _lastPosFinger1;
                _lastPosFinger1 = t.position;
                float sign = invertHorizontal ? -1f : 1f;
                _yawOffsetTarget += sign * delta.x * yawDegreesPerPixel;
            }
        }

        if (i2 != -1)
        {
            var t = Input.GetTouch(i2);
            if (t.phase == TouchPhase.Began) { _lastPosFinger2 = t.position; _moveDir = Vector2.zero; }
            if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
            {
                Vector2 delta = t.position - _lastPosFinger2;
                _lastPosFinger2 = t.position;
                _moveDir = new Vector2(delta.x / pixelsPerUnit, delta.y / pixelsPerUnit);
                _moveDir = Vector2.ClampMagnitude(_moveDir, 1f);
            }
            if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) { _moveDir = Vector2.zero; }
        }
    }
}
