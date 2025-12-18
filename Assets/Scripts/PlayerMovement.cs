using UnityEngine;

public class CorridorJoystickMover : MonoBehaviour
{
    [Header("Refs")]
    public Joystick joystick;
    public Transform leftWall;
    public Transform rightWall;

    [Header("Corridor Limits")]
    public float wallMargin = 0.3f;
    [Tooltip("Maximum forward/backward range from the start position")]
    public float forwardRange = 12f;

    [Header("Movement")]
    public float strafeSpeed = 6f;
    public float forwardSpeed = 3f;

    [Header("Input")]
    [Range(0f, 0.4f)] public float deadZone = 0.1f;

    [Header("Yaw (optional)")]
    public float maxYawAngle = 12f;
    public float yawTurnSpeed = 120f;

    private Plane _pL, _pR;
    private Vector3 _acrossDir;
    private Vector3 _alongDir;
    private Quaternion _baseRot;
    private float _yawOffset;
    private float _startZ;

    void Start()
    {
        if (!joystick) Debug.LogWarning("[CorridorJoystickMover] Joystick is null.");
        if (!leftWall || !rightWall)
        {
            Debug.LogError("[CorridorJoystickMover] Assign leftWall & rightWall.");
            enabled = false; return;
        }

        _baseRot = transform.rotation;

        _acrossDir = rightWall.position - leftWall.position;
        _acrossDir.y = 0f;
        if (_acrossDir.sqrMagnitude < 1e-6f) _acrossDir = Vector3.right;
        _acrossDir.Normalize();

        _alongDir = Vector3.Cross(Vector3.up, _acrossDir);
        _alongDir.y = 0f; 
        _alongDir.Normalize();
        _alongDir = -_alongDir;

        _pL = new Plane(_acrossDir, leftWall.position);
        _pR = new Plane(-_acrossDir, rightWall.position);

        _startZ = Vector3.Dot(transform.position, _alongDir);
    }

    void Update()
    {
        if (!joystick) return;

        float inX = Mathf.Abs(joystick.Horizontal) > deadZone ? joystick.Horizontal : 0f;
        float inZ = Mathf.Abs(joystick.Vertical)   > deadZone ? joystick.Vertical   : 0f;

        Vector3 delta =
              _acrossDir * (inX * strafeSpeed  * Time.deltaTime)
            + _alongDir  * (inZ * forwardSpeed * Time.deltaTime);

        Vector3 newPos = transform.position + delta;

        newPos = KeepInsideWithMargin(newPos, _pL, wallMargin);
        newPos = KeepInsideWithMargin(newPos, _pR, wallMargin);

        float currentZ = Vector3.Dot(newPos, _alongDir);
        float minZ = _startZ - forwardRange;
        float maxZ = _startZ + forwardRange;
        currentZ = Mathf.Clamp(currentZ, minZ, maxZ);
        newPos = newPos - _alongDir * Vector3.Dot(newPos, _alongDir) + _alongDir * currentZ;

        transform.position = newPos;

        _yawOffset += inX * yawTurnSpeed * Time.deltaTime;
        _yawOffset = Mathf.Clamp(_yawOffset, -maxYawAngle, +maxYawAngle);
        transform.rotation = _baseRot * Quaternion.Euler(0f, _yawOffset, 0f);
    }

    Vector3 KeepInsideWithMargin(Vector3 pos, Plane wall, float margin)
    {
        float d = wall.GetDistanceToPoint(pos);
        if (d < margin)
            pos += wall.normal * (margin - d);
        return pos;
    }
}
