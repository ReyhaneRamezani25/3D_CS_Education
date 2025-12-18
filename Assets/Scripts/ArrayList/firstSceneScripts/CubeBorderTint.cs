using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class CubeEdgeLines : MonoBehaviour
{
    public Color lineColor = Color.black;
    public float lineWidth = 0.02f;

    private static Material lineMat;

    private readonly int[,] edges = new int[,]
    {
        {0,1},{1,2},{2,3},{3,0},
        {4,5},{5,6},{6,7},{7,4},
        {0,4},{1,5},{2,6},{3,7}
    };

    void OnEnable()
    {
        if (lineMat == null)
        {
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            lineMat = new Material(shader);
            lineMat.hideFlags = HideFlags.HideAndDontSave;
            lineMat.SetInt("_ZWrite", 1);
            lineMat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            lineMat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.LessEqual);
        }
    }

    void OnRenderObject()
    {
        if (!lineMat) return;

        lineMat.SetPass(0);

        GL.PushMatrix();
        GL.MultMatrix(transform.localToWorldMatrix);

        GL.Begin(GL.LINES);
        GL.Color(lineColor);

        Vector3[] v = {
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3( 0.5f, -0.5f, -0.5f),
            new Vector3( 0.5f, -0.5f,  0.5f),
            new Vector3(-0.5f, -0.5f,  0.5f),
            new Vector3(-0.5f,  0.5f, -0.5f),
            new Vector3( 0.5f,  0.5f, -0.5f),
            new Vector3( 0.5f,  0.5f,  0.5f),
            new Vector3(-0.5f,  0.5f,  0.5f)
        };

        for (int i = 0; i < edges.GetLength(0); i++)
        {
            GL.Vertex(v[edges[i, 0]]);
            GL.Vertex(v[edges[i, 1]]);
        }

        GL.End();
        GL.PopMatrix();
    }
}
