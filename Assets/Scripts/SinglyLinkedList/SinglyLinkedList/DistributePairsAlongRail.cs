using UnityEngine;

[ExecuteAlways]
public class SimplePairDistributorFixed : MonoBehaviour
{
    public Transform rail;
    public float spacing = 50f;
    public bool keepFirstAtPlace = true;
    public bool reverseOrder = false;

    [ContextMenu("Distribute Now")]
    public void Distribute()
    {
        if (!rail || transform.childCount == 0) return;

        Vector3 dir = rail ? rail.right.normalized : Vector3.right;
        int n = transform.childCount;

        Vector3 startPos = transform.GetChild(0).position;

        if (reverseOrder)
            dir *= -1;

        for (int i = 0; i < n; i++)
        {
            Transform t = transform.GetChild(i);
            if (keepFirstAtPlace && i == 0) continue;
            t.position = startPos + dir * (spacing * i);
        }
    }

    void OnValidate()
    {
        if (!Application.isPlaying)
            Distribute();
    }
}
