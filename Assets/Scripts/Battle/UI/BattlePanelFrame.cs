using UnityEngine;
using UnityEngine.UI;

/// <summary>按实际矩形绘制固定线宽与切角的面板外框，避免九宫格栅格描边在伸缩后模糊。</summary>
[AddComponentMenu("UI/Battle Panel Frame")]
public sealed class BattlePanelFrame : Image
{
    [SerializeField, Min(0)] private float cornerCut = 16f; // Canvas 单位，保持原外框的右上/左下切角。
    [SerializeField, Min(0.1f)] private float strokeWidth = 2f;
    [SerializeField, Min(0)] private float frameInset = 2f;

    public override Texture mainTexture => Texture2D.whiteTexture;

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        var rect = GetPixelAdjustedRect();
        float half = strokeWidth * 0.5f;
        float inset = frameInset + half;
        if (rect.width <= inset * 2 || rect.height <= inset * 2) return;
        float cut = Mathf.Min(cornerCut, (Mathf.Min(rect.width, rect.height) - inset * 2) * 0.5f);
        float left = rect.xMin + inset, right = rect.xMax - inset;
        float bottom = rect.yMin + inset, top = rect.yMax - inset;
        Vector2[] contour = cut > 0.01f ? new[]
        {
            new Vector2(left, top), new Vector2(right - cut, top), new Vector2(right, top - cut),
            new Vector2(right, bottom), new Vector2(left + cut, bottom), new Vector2(left, bottom + cut)
        } : new[] { new Vector2(left, top), new Vector2(right, top), new Vector2(right, bottom), new Vector2(left, bottom) };
        float scale = canvas != null ? canvas.scaleFactor : 1f;
        float smoothing = 0.75f / Mathf.Max(0.01f, scale); // 羽化约占0.75个屏幕像素，不随面板宽高拉伸。
        float[] offsets = { -half - smoothing, -half, half, half + smoothing };
        int count = contour.Length;
        for (int ring = 0; ring < offsets.Length; ring++)
        {
            Color tint = color;
            if (ring == 0 || ring == offsets.Length - 1) tint.a = 0;
            for (int i = 0; i < count; i++)
            {
                var previous = (contour[i] - contour[(i + count - 1) % count]).normalized;
                var next = (contour[(i + 1) % count] - contour[i]).normalized;
                var n1 = new Vector2(previous.y, -previous.x);
                var n2 = new Vector2(next.y, -next.x);
                // 相邻边偏移线求交，保证斜角和水平/竖直边的描边厚度一致。
                Vector2 miter = (n1 + n2) / Mathf.Max(0.01f, 1 + Vector2.Dot(n1, n2));
                mesh.AddVert(contour[i] + miter * offsets[ring], tint, Vector2.zero);
            }
        }
        for (int ring = 0; ring < offsets.Length - 1; ring++)
            for (int i = 0; i < count; i++)
            {
                int next = (i + 1) % count;
                mesh.AddTriangle(ring * count + i, ring * count + next, (ring + 1) * count + next);
                mesh.AddTriangle(ring * count + i, (ring + 1) * count + next, (ring + 1) * count + i);
            }
    }
}
