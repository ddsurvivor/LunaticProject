using UnityEngine;
using UnityEngine.UI;

/// <summary>紧凑头顶面板的固定像素斜角外框；尺寸变化只扩展直边，不缩放斜角与描边。</summary>
[AddComponentMenu("UI/Compact Overhead Frame")]
public sealed class CompactOverheadFrame : UnityEngine.UI.Image
{
    [SerializeField, Min(0)] private float cornerSize = 4f;
    [SerializeField, Min(0)] private float borderWidth = 1f;
    [SerializeField] private Color borderColor = new Color(230f / 255, 165f / 255, 51f / 255, 1f);
    [SerializeField] private Color fillColor = new Color(18f / 255, 24f / 255, 28f / 255, 1f);

    // 几何使用纯色，不采样旧大面板贴图的透明角或拉伸其描边。
    public override Texture mainTexture => Texture2D.whiteTexture;

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect rect = GetPixelAdjustedRect();
        float edge = Mathf.Clamp(borderWidth, 0, Mathf.Min(rect.width, rect.height) * .5f);
        float cut = Mathf.Clamp(cornerSize, 0, Mathf.Min(rect.width, rect.height) * .5f);
        var outer = Polygon(rect, cut);
        var innerRect = new Rect(rect.xMin + edge, rect.yMin + edge, Mathf.Max(0, rect.width - 2 * edge), Mathf.Max(0, rect.height - 2 * edge));
        // 对角线按法线内缩，保证斜边也保持相同描边宽度。
        var inner = Polygon(innerRect, Mathf.Max(0, cut + (Mathf.Sqrt(2) - 2) * edge));
        Color fill = fillColor * color;
        for (int i = 0; i < 6; i++) mesh.AddVert(inner[i], fill, Vector2.zero);
        for (int i = 1; i < 5; i++) mesh.AddTriangle(0, i, i + 1);
        for (int i = 0; i < 6; i++)
        {
            int next = (i + 1) % 6;
            int start = mesh.currentVertCount;
            mesh.AddVert(outer[i], borderColor, Vector2.zero); mesh.AddVert(outer[next], borderColor, Vector2.zero);
            mesh.AddVert(inner[next], borderColor, Vector2.zero); mesh.AddVert(inner[i], borderColor, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2); mesh.AddTriangle(start, start + 2, start + 3);
        }
    }

    private static Vector3[] Polygon(Rect rect, float cut) => new[]
    {
        new Vector3(rect.xMin, rect.yMax), new Vector3(rect.xMax - cut, rect.yMax),
        new Vector3(rect.xMax, rect.yMax - cut), new Vector3(rect.xMax, rect.yMin),
        new Vector3(rect.xMin + cut, rect.yMin), new Vector3(rect.xMin, rect.yMin + cut)
    };
}
