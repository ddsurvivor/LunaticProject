using UnityEngine;

/// <summary>绘制快进双箭头或齿轮，不依赖字体是否包含特殊符号。</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class BattleTimeControlIcon : UnityEngine.UI.MaskableGraphic
{
    public bool IsGear { get; set; }

    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
    {
        mesh.Clear();
        var rect = GetPixelAdjustedRect();
        Vector2 center = rect.center;
        float radius = Mathf.Min(rect.width, rect.height) * .5f;
        if (!IsGear)
        {
            for (int i = 0; i < 2; i++)
            {
                float x = center.x - radius + i * radius;
                int start = mesh.currentVertCount;
                mesh.AddVert(new Vector3(x, center.y - radius * .65f), color, Vector2.zero);
                mesh.AddVert(new Vector3(x, center.y + radius * .65f), color, Vector2.zero);
                mesh.AddVert(new Vector3(x + radius * .9f, center.y), color, Vector2.zero);
                mesh.AddTriangle(start, start + 1, start + 2);
            }
            return;
        }
        const int count = 64;
        for (int i = 0; i < count; i++)
        {
            float angle = i * Mathf.PI * 2 / count;
            var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            float outer = i % 8 < 4 ? radius : radius * .76f;
            mesh.AddVert(center + direction * outer, color, Vector2.zero);
            mesh.AddVert(center + direction * radius * .42f, color, Vector2.zero);
        }
        for (int i = 0; i < count; i++)
        {
            int current = i * 2, next = (i + 1) % count * 2;
            mesh.AddTriangle(current, current + 1, next);
            mesh.AddTriangle(next, current + 1, next + 1);
        }
    }
}
