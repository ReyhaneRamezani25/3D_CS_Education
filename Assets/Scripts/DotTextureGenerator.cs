using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public class DotTextureGenerator : MonoBehaviour
{
    public Material targetMaterial;
    public int size = 64;
    public int dotRadius = 3;
    public Color dotColor = Color.white;
    public Color backgroundColor;
    public Vector2 tiling = new Vector2(100, 100);

    void OnEnable() { Generate(); }
#if UNITY_EDITOR
    void OnValidate() { Generate(); }
#endif

    void Generate()
    {
        var rend = GetComponent<Renderer>();
        var mat = targetMaterial != null ? targetMaterial : rend.sharedMaterial;
        if (mat == null) return;

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = x - size / 2f;
            float dy = y - size / 2f;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            tex.SetPixel(x, y, dist < dotRadius ? dotColor : backgroundColor);
        }
        tex.Apply();
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;

        mat.mainTexture = tex;
        mat.mainTextureScale = tiling;
    }
}
