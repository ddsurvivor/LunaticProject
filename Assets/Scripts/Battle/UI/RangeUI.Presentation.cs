using System.Collections.Generic;
using UnityEngine;

/// <summary>范围与路径共用的颜色和深度材质；运行时实例由调用者释放。</summary>
public static class BattleRangeStyle
{
    public static readonly Color Move = new Color(0.18f, 0.82f, 1f, 0.9f);
    public static readonly Color Attack = new Color(1f, 0.43f, 0.18f, 0.95f);
    public static readonly Color Skill = new Color(0.75f, 0.46f, 1f, 0.95f);
    public static readonly Color Support = new Color(0.3f, 1f, 0.55f, 0.95f);
    public static readonly Color Unavailable = new Color(0.65f, 0.69f, 0.73f, 0.65f);

    /// <summary>创建范围与路径共用样式的独占材质，由调用者在销毁时释放。</summary>
    public static Material CreateMaterial()
    {
        return new Material(Resources.Load<Shader>("Battle/RangeOutline")) { name = "BattleRangeRuntime" };
    }

    /// <summary>按技能的支援和进攻语义选择颜色，不改写目标关系。</summary>
    public static Color ForSkill(SkillPack skill)
    {
        if (skill.target == SkillTarget.Ally || skill.target == SkillTarget.AllyBody || skill.target == SkillTarget.Self)
            return Support;
        return BattleRelations.IsOffensive(skill) ? Attack : Skill;
    }

    /// <summary>为世界坐标范围线、路径和标记设置相同宽度及深度材质。</summary>
    public static void Configure(LineRenderer line, Material material, Color color)
    {
        line.sharedMaterial = material;
        line.useWorldSpace = true;
        line.widthMultiplier = 0.045f;
        line.startColor = line.endColor = color;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.numCornerVertices = 3;
        line.numCapVertices = 3;
    }
}

public partial class RangeUI
{
    private Material rangeMaterial; // 此范围实例独占，销毁时释放。
    private readonly List<LineRenderer> outlines = new List<LineRenderer>();
    private int outlineCount;
    private LineRenderer destinationMarker;
    private readonly List<Vector3> outlinePoints = new List<Vector3>(100);
    private const float SurfaceOffset = 0.08f; // 米；与路径、准星使用相同离地高度。

    private void PreparePresentation()
    {
        if (rangeMaterial != null) return;
        rangeMaterial = BattleRangeStyle.CreateMaterial();
        // 运行时复制模板时不继承模板的绘线实例。
        foreach (var line in GetComponentsInChildren<LineRenderer>(true))
            if (line.name.StartsWith("RangeOutline") || line.name == "MoveDestination") { line.gameObject.SetActive(false); Destroy(line.gameObject); }
        // 保留旧节点供警戒方向与落点接口使用，关闭旧填充，避免图片尺寸改变实际几何。
        foreach (var root in new[] { circle, attackCircle, skillCircle, grenadeCircle, fanRoot, arcRoot, moveIcon, attackIcon, skillIcon })
        {
            if (root == null) continue;
            foreach (var graphic in root.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
            { graphic.enabled = false; graphic.raycastTarget = false; }
            foreach (var sprite in root.GetComponentsInChildren<SpriteRenderer>(true)) sprite.enabled = false;
        }
        foreach (var root in new[] { moveIcon, attackIcon, skillIcon, highlightCircle, selectCircle })
        {
            if (root == null) continue;
            foreach (var graphic in root.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
            { graphic.raycastTarget = false; graphic.material = rangeMaterial; }
            foreach (var sprite in root.GetComponentsInChildren<SpriteRenderer>(true)) sprite.sharedMaterial = rangeMaterial;
        }
        Tint(moveIcon, BattleRangeStyle.Move);
        Tint(selectCircle, BattleRangeStyle.Move);
    }

    private static void Tint(GameObject root, Color color)
    {
        if (root == null) return;
        foreach (var sprite in root.GetComponentsInChildren<SpriteRenderer>(true)) sprite.color = color;
        foreach (var graphic in root.GetComponentsInChildren<UnityEngine.UI.Graphic>(true)) graphic.color = color;
    }

    private void RefreshTargets()
    {
        if (_curSkillPack == null) return;
        if (_owner == null || _owner.isDead || !_owner.gameObject.activeInHierarchy)
        { CloseRange(); return; }
        _selectionPosition = BattleRangeRules.ClampPosition(_owner, _curSkillPack, _selectionPosition);
        _curRange = BattleRangeRules.CastRange(_owner, _curSkillPack, _selectionPosition);
        ApplyTargets(SkillTargeting.Query(_owner, _curSkillPack, _selectionPosition));
    }

    private void LateUpdate()
    {
        if (_curSkillPack == null) return;
        RefreshTargets();
        DrawRangePresentation();
    }

    private void DrawRangePresentation()
    {
        PreparePresentation();
        outlineCount = 0;
        Vector3 origin = _owner != null ? _owner.transform.position : transform.position - Vector3.up * 0.1f;
        if (_curSkillPack == null)
        {
            if (circle.activeSelf || attackCircle.activeSelf)
                Ring(origin, _curRange, circle.activeSelf ? BattleRangeStyle.Move : BattleRangeStyle.Attack);
        }
        else
        {
            var skill = _curSkillPack;
            Color color = BattleRangeStyle.ForSkill(skill);
            bool valid = _curTargets.Count > 0 || skill.target == SkillTarget.Area;
            Color aimColor = valid ? color : BattleRangeStyle.Unavailable;
            Tint(skillIcon, aimColor);
            Tint(attackIcon, aimColor);
            foreach (var target in _curTargets)
            {
                if (target == null) continue;
                if (target.hightlightEffect != null) target.hightlightEffect.SetActive(false);
                if (target.rangeUI != null) target.rangeUI.highlightCircle.SetActive(false);
            }
            Vector3 forward = _selectionPosition - origin;
            forward.y = 0;
            if (forward.sqrMagnitude < 0.0001f) forward = _owner.transform.forward;
            forward.Normalize();
            Color boundary = color; boundary.a = 0.4f;
            if (skill.target == SkillTarget.Self) Ring(origin, SkillTargeting.SingleTargetRadius, aimColor);
            else switch (skill.rangeType)
            {
                case RangeType.Circle:
                    Ring(origin, _curRange, boundary);
                    Ring(_selectionPosition, SkillTargeting.SingleTargetRadius, aimColor);
                    break;
                case RangeType.Grenade:
                    Ring(origin, _curRange, boundary);
                    Ring(_selectionPosition, skill.explodeRadius, aimColor);
                    break;
                case RangeType.Nova:
                    Ring(origin, skill.explodeRadius > 0 ? skill.explodeRadius : skill.rangeValue, aimColor);
                    break;
                case RangeType.Fan:
                    Sector(origin, forward, Mathf.Min(1f, skill.rangeValue), skill.rangeValue, skill.rangeAgle / 2f, color);
                    break;
                case RangeType.Arc:
                    float radius = SkillTargeting.GetArcRadius(skill);
                    Sector(SkillTargeting.GetArcCenter(origin, forward, skill), Vector3.Cross(Vector3.up, forward),
                        Mathf.Max(0, radius - skill.arcWeight / 2f), Mathf.Max(0, radius + skill.arcWeight / 2f),
                        SkillTargeting.GetArcHalfAngle(skill), color);
                    break;
            }
            if (skill.target != SkillTarget.Self && skill.rangeType != RangeType.Nova)
                Ring(_selectionPosition, 0.18f, aimColor);
            foreach (var target in _curTargets)
                if (target != null) Ring(target.transform.position, 0.55f, color);
        }
        for (int i = outlineCount; i < outlines.Count; i++) outlines[i].gameObject.SetActive(false);
    }

    private void Ring(Vector3 center, float radius, Color color)
    {
        Arc(center, Vector3.forward, Mathf.Max(0, radius), 180, color);
    }

    private void Sector(Vector3 center, Vector3 forward, float inner, float outer, float halfAngle, Color color)
    {
        Arc(center, forward, outer, halfAngle, color);
        if (inner > 0) Arc(center, forward, inner, halfAngle, color);
        foreach (float angle in new[] { -halfAngle, halfAngle })
        {
            Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * forward;
            outlinePoints.Clear();
            outlinePoints.Add(center + dir * inner + Vector3.up * SurfaceOffset);
            outlinePoints.Add(center + dir * outer + Vector3.up * SurfaceOffset);
            DrawOutline(color);
        }
    }

    private void Arc(Vector3 center, Vector3 forward, float radius, float halfAngle, Color color)
    {
        int steps = Mathf.Max(2, Mathf.CeilToInt(halfAngle * 2 / 4f));
        outlinePoints.Clear();
        for (int i = 0; i <= steps; i++)
            outlinePoints.Add(center + Quaternion.AngleAxis(Mathf.Lerp(-halfAngle, halfAngle, i / (float)steps),
                Vector3.up) * forward * radius + Vector3.up * SurfaceOffset);
        DrawOutline(color);
    }

    private void DrawOutline(Color color)
    {
        if (outlineCount == outlines.Count)
        {
            var node = new GameObject("RangeOutline" + outlineCount);
            node.transform.SetParent(transform, false);
            var line = node.AddComponent<LineRenderer>();
            BattleRangeStyle.Configure(line, rangeMaterial, color);
            outlines.Add(line);
        }
        var outline = outlines[outlineCount++];
        outline.gameObject.SetActive(true);
        outline.startColor = outline.endColor = color;
        outline.positionCount = outlinePoints.Count;
        for (int i = 0; i < outlinePoints.Count; i++) outline.SetPosition(i, outlinePoints[i]);
    }

    private void DrawDestinationMarker(Vector3 position)
    {
        PreparePresentation();
        if (destinationMarker == null)
        {
            var node = new GameObject("MoveDestination");
            node.transform.SetParent(transform, false);
            destinationMarker = node.AddComponent<LineRenderer>();
            BattleRangeStyle.Configure(destinationMarker, rangeMaterial, BattleRangeStyle.Move);
        }
        destinationMarker.gameObject.SetActive(_isShowMoveIcon);
        destinationMarker.positionCount = 5;
        for (int i = 0; i < 5; i++)
            destinationMarker.SetPosition(i, position + Vector3.up * SurfaceOffset +
                Quaternion.AngleAxis(i * 90, Vector3.up) * Vector3.forward * 0.35f);
    }

    private void HideRangePresentation()
    {
        foreach (var line in outlines) if (line != null) line.gameObject.SetActive(false);
        if (destinationMarker != null) destinationMarker.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        ApplyTargets(new List<PieceController>());
        if (rangeMaterial != null) Destroy(rangeMaterial);
    }
}
