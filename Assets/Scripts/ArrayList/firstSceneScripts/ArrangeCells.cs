using UnityEngine;

[ExecuteAlways]
public class DistributeAlongRail : MonoBehaviour
{
    [Header("Rail")]
    public Transform rail;
    public MeshRenderer railRenderer;
    public float railLengthOverride = 300;
    public float sideMargin = 0f;

    [Header("Items")]
    public bool alignRotationToRail = false;
    [Tooltip("Vertical offset relative to the rail (e.g. half cell height)")]
    public float fixedOffsetUp = 17.5f;

    [Header("Spacing Mode")]
    [Tooltip("If enabled, uses fixed spacing between items and calculates equal gaps on both rail ends.")]
    public bool useFixedSpacing = true;

    [Tooltip("Fixed spacing between items when useFixedSpacing is enabled.")]
    public float fixedSpacing = 50f;

    [Tooltip("If true and total spacing exceeds rail length, spacing is reduced to fit exactly. If false, spacing remains constant and edge gaps become zero.")]
    public bool shrinkSpacingToFit = true;

    [Header("Legacy (ignored when useFixedSpacing = true)")]
    [Tooltip("Used when useFixedSpacing is off: gaps fill the entire rail length.")]
    public bool includeEdgeGaps = true;

    [Header("Edge / Reveal")]
    [Tooltip("Moves cubes slightly backward along the rail to reveal rail edges (scene units).")]
    public float frontInset = 2f;

    [Tooltip("If enabled, reads the cube's depth from Renderer (or from childDepthOverride) so that the front edge aligns exactly with the rail point, then moves backward by frontInset.")]
    public bool considerChildDepth = false;

    [Tooltip("If >0, uses this value as the cube's depth along the rail instead of reading from Renderer.")]
    public float childDepthOverride = 0f;

    [ContextMenu("Distribute Now")]
    public void Distribute()
    {
        if (!rail || transform.childCount == 0) return;

        Vector3 dir = rail.right.normalized;

        float railLen;
        Vector3 startWorld, endWorld;

        if (railRenderer)
        {
            Bounds wb = railRenderer.bounds;
            Vector3 c = wb.center;
            float halfLen = Vector3.Dot(wb.size, new Vector3(Mathf.Abs(dir.x), Mathf.Abs(dir.y), Mathf.Abs(dir.z))) * 0.5f;
            startWorld = c - dir * halfLen;
            endWorld = c + dir * halfLen;
            railLen = Vector3.Distance(startWorld, endWorld);
        }
        else
        {
            railLen = railLengthOverride;
            startWorld = rail.position - dir * (railLen * 0.5f);
            endWorld = rail.position + dir * (railLen * 0.5f);
        }

        startWorld += dir * sideMargin;
        endWorld -= dir * sideMargin;
        railLen = Vector3.Distance(startWorld, endWorld);

        int n = transform.childCount;
        Vector3 up = rail.up;

        if (n == 1)
        {
            Vector3 center = (startWorld + endWorld) * 0.5f + up * fixedOffsetUp;
            Transform t0 = transform.GetChild(0);
            Vector3 backVec = ComputeBackVector(t0, dir);
            t0.position = center + backVec;
            if (alignRotationToRail) t0.rotation = Quaternion.LookRotation(dir, up);
            return;
        }

        float spacing;
        int innerGaps = n - 1;

        if (useFixedSpacing)
        {
            spacing = Mathf.Max(0f, fixedSpacing);
            float neededLength = innerGaps * spacing;

            float edgeGap = 0f;
            if (neededLength <= railLen)
            {
                edgeGap = (railLen - neededLength) * 0.5f;
            }
            else
            {
                if (shrinkSpacingToFit)
                {
                    spacing = railLen / innerGaps;
                    edgeGap = 0f;
                }
                else
                {
                    edgeGap = 0f;
                }
            }

            for (int i = 0; i < n; i++)
            {
                float dist = edgeGap + i * spacing;
                Vector3 p = Vector3.Lerp(startWorld, endWorld, dist / railLen);
                p += up * fixedOffsetUp;

                Transform t = transform.GetChild(i);
                Vector3 backVec = ComputeBackVector(t, dir);
                t.position = p + backVec;

                if (alignRotationToRail)
                    t.rotation = Quaternion.LookRotation(dir, up);
            }
        }
        else
        {
            int gaps = includeEdgeGaps ? (n + 1) : (n - 1);
            gaps = Mathf.Max(1, gaps);
            spacing = railLen / gaps;

            for (int i = 0; i < n; i++)
            {
                float dist = (includeEdgeGaps ? (i + 1) : i) * spacing;
                Vector3 p = Vector3.Lerp(startWorld, endWorld, dist / railLen);
                p += up * fixedOffsetUp;

                Transform t = transform.GetChild(i);
                Vector3 backVec = ComputeBackVector(t, dir);
                t.position = p + backVec;

                if (alignRotationToRail)
                    t.rotation = Quaternion.LookRotation(dir, up);
            }
        }
    }

    private Vector3 ComputeBackVector(Transform t, Vector3 dir)
    {
        float inset = Mathf.Max(0f, frontInset);

        if (!considerChildDepth)
            return -dir * inset;

        float depth = 0f;

        if (childDepthOverride > 0f)
        {
            depth = childDepthOverride;
        }
        else
        {
            var rend = t ? t.GetComponentInChildren<Renderer>() : null;
            if (rend)
            {
                Vector3 size = rend.bounds.size;
                depth = Mathf.Abs(size.x * Mathf.Abs(dir.x) + size.y * Mathf.Abs(dir.y) + size.z * Mathf.Abs(dir.z));
            }
        }

        float backAmount = depth * 0.5f + inset;
        return -dir * backAmount;
    }

    void OnValidate() { Distribute(); }

    void OnDrawGizmosSelected()
    {
        if (!rail) return;
        Gizmos.color = Color.cyan;
        Vector3 dir = rail.right.normalized;
        Vector3 a = rail.position - dir * (railLengthOverride * 0.5f);
        Vector3 b = rail.position + dir * (railLengthOverride * 0.5f);
        Gizmos.DrawLine(a, b);
    }
}
