using UnityEngine;
using System.Collections.Generic;

public class MultiSnapper : MonoBehaviour
{
    [System.Serializable]
    public class Pair
    {
        public Transform objectA;
        public Transform objectB;
        public Side snapSide = Side.Right;
        public bool matchRotation = true;
    }

    public enum Side { Right, Left, Front, Back, Top, Bottom }
    public Pair[] pairs;

    void Start()
    {
        foreach (var p in pairs)
        {
            if (p.objectA == null || p.objectB == null) continue;

            if (p.matchRotation) p.objectB.rotation = p.objectA.rotation;

            Vector3 n = GetNormal(p.objectA, p.snapSide).normalized;

            var vertsA = GetWorldVertices(p.objectA);
            var vertsB = GetWorldVertices(p.objectB);
            if (vertsA.Count == 0 || vertsB.Count == 0) continue;

            float maxA = float.NegativeInfinity;
            for (int i = 0; i < vertsA.Count; i++)
            {
                float d = Vector3.Dot(vertsA[i], n);
                if (d > maxA) maxA = d;
            }

            float minB = float.PositiveInfinity;
            for (int i = 0; i < vertsB.Count; i++)
            {
                float d = Vector3.Dot(vertsB[i], n);
                if (d < minB) minB = d;
            }

            float move = maxA - minB;
            p.objectB.position += n * move;
        }
    }

    static Vector3 GetNormal(Transform t, Side side)
    {
        switch (side)
        {
            case Side.Right: return t.right;
            case Side.Left:  return -t.right;
            case Side.Front: return t.forward;
            case Side.Back:  return -t.forward;
            case Side.Top:   return t.up;
            default:         return -t.up;
        }
    }

    static List<Vector3> GetWorldVertices(Transform root)
    {
        var result = new List<Vector3>(256);
        var mfs = root.GetComponentsInChildren<MeshFilter>(true);
        for (int i = 0; i < mfs.Length; i++)
        {
            var mesh = mfs[i].sharedMesh;
            if (!mesh) continue;
            var vs = mesh.vertices;
            var tf = mfs[i].transform;
            for (int j = 0; j < vs.Length; j++)
                result.Add(tf.TransformPoint(vs[j]));
        }
        if (result.Count == 0)
        {
            var rends = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rends.Length; i++)
            {
                Bounds b = rends[i].bounds;
                Vector3 c = b.center, e = b.extents;
                result.Add(c + new Vector3( e.x,  e.y,  e.z));
                result.Add(c + new Vector3( e.x,  e.y, -e.z));
                result.Add(c + new Vector3( e.x, -e.y,  e.z));
                result.Add(c + new Vector3( e.x, -e.y, -e.z));
                result.Add(c + new Vector3(-e.x,  e.y,  e.z));
                result.Add(c + new Vector3(-e.x,  e.y, -e.z));
                result.Add(c + new Vector3(-e.x, -e.y,  e.z));
                result.Add(c + new Vector3(-e.x, -e.y, -e.z));
            }
        }
        return result;
    }
}
