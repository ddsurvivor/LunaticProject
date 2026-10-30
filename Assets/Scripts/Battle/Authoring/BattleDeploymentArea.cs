using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("战斗搭建/角色出生与部署区域")]
public sealed class BattleDeploymentArea : BattlePlaceable
{
    [LabelText("部署尺寸（宽 / 深）")] public Vector2 size = new Vector2(8, 6);
    [LabelText("区域图片")] [PreviewField] public Sprite sprite;
    [LabelText("显示颜色")] public Color color = new Color(0.15f, 0.9f, 0.65f, 0.3f);
    [LabelText("地面高度容差")] [MinValue(0.05f)] public float heightTolerance = 0.4f;
    private SpriteRenderer visual;
    private bool deploymentVisible;
    private LineRenderer fallbackOutline;
    private Material fallbackMaterial; // 无区域图片的运行时部署边界，由本组件释放。

    protected override void ApplyParameters()
    {
        size = new Vector2(Dimension(size.x, 0.5f), Dimension(size.y, 0.5f));
        heightTolerance = Dimension(heightTolerance, 0.05f);
        // 只显示范围，不添加阻挡射线和导航的实体碰撞。
        visual = SetSprite("DeploymentRange", sprite, Vector3.up * 0.035f,
            Quaternion.Euler(90, 0, 0), size, color);
        visual.enabled = !Application.isPlaying || deploymentVisible;
    }

    public void SetDeploymentVisible(bool visible)
    {
        deploymentVisible = visible;
        if (visual == null) visual = GetComponentInChildren<SpriteRenderer>(true);
        if (visual != null) visual.enabled = visible;
        if (visible && sprite == null && fallbackOutline == null)
        {
            fallbackOutline = gameObject.AddComponent<LineRenderer>();
            fallbackMaterial = BattleRangeStyle.CreateMaterial();
            var outlineColor = color;
            outlineColor.a = 0.95f;
            BattleRangeStyle.Configure(fallbackOutline, fallbackMaterial, outlineColor);
            fallbackOutline.widthMultiplier = 0.12f;
            fallbackOutline.useWorldSpace = false;
            fallbackOutline.loop = true;
            fallbackOutline.positionCount = 4;
            fallbackOutline.SetPositions(new[] { new Vector3(-size.x / 2, .15f, -size.y / 2),
                new Vector3(size.x / 2, .15f, -size.y / 2), new Vector3(size.x / 2, .15f, size.y / 2),
                new Vector3(-size.x / 2, .15f, size.y / 2) });
        }
        if (fallbackOutline != null) fallbackOutline.enabled = visible && sprite == null;
    }

    private void OnDestroy() { if (fallbackMaterial != null) Destroy(fallbackMaterial); }

    public bool Contains(Vector3 worldPosition, float radius = 0)
    {
        var local = Quaternion.Inverse(transform.rotation) * (worldPosition - transform.position);
        return Mathf.Abs(local.y) <= heightTolerance &&
            Mathf.Abs(local.x) + radius <= size.x * 0.5f &&
            Mathf.Abs(local.z) + radius <= size.y * 0.5f;
    }

    public IEnumerable<Vector3> CandidatePositions(float radius)
    {
        yield return transform.position;
        float step = Mathf.Max(0.5f, radius * 2 + 0.2f);
        for (float z = -size.y * 0.5f + radius; z <= size.y * 0.5f - radius; z += step)
            for (float x = -size.x * 0.5f + radius; x <= size.x * 0.5f - radius; x += step)
                yield return transform.position + transform.rotation * new Vector3(x, 0, z);
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying && !deploymentVisible) return;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.color = new Color(color.r, color.g, color.b, 0.9f);
        Gizmos.DrawWireCube(Vector3.up * 0.035f, new Vector3(size.x, 0.025f, size.y));
    }
}
