using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>战斗搭建组件：根节点只负责位置和朝向，尺寸始终由脚本驱动。</summary>
[ExecuteAlways]
public abstract class BattlePlaceable : MonoBehaviour
{
    private bool refreshPending = true;
    private Matrix4x4 lastMatrix;

    protected virtual void OnEnable() => refreshPending = true;
    protected virtual void OnValidate() => refreshPending = true;

    protected virtual void Update()
    {
        if (refreshPending || transform.localToWorldMatrix != lastMatrix) Refresh();
    }

    [Button("检查并刷新", ButtonSizes.Medium)]
    public void Refresh()
    {
        if (!gameObject.scene.IsValid()) return; // 不在资源导入线程中修改预制体资产。
        refreshPending = false;
        // 抵消父级缩放，保证 Inspector 中的尺寸是实际世界尺寸。
        transform.localScale = Vector3.one;
        Vector3 scale = transform.lossyScale;
        transform.localScale = new Vector3(Reciprocal(scale.x), Reciprocal(scale.y), Reciprocal(scale.z));
        transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        ApplyParameters();
        lastMatrix = transform.localToWorldMatrix;
    }

    [Button("归位到 Y = 0 并刷新", ButtonSizes.Medium)]
    public void SnapToGround()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) UnityEditor.Undo.RecordObject(transform, "归位战斗组件");
#endif
        var position = transform.position;
        position.y = 0;
        transform.position = position;
        Refresh();
    }

    protected abstract void ApplyParameters();

    protected Transform Child(string childName)
    {
        var child = transform.Find(childName);
        if (child != null) return child;
        var go = new GameObject(childName);
        go.layer = gameObject.layer;
        go.transform.SetParent(transform, false);
        return go.transform;
    }

    protected SpriteRenderer SetSprite(string childName, Sprite sprite, Vector3 position,
        Quaternion rotation, Vector2 size, Color tint)
    {
        var child = Child(childName);
        var renderer = child.GetComponent<SpriteRenderer>();
        if (renderer == null) renderer = child.gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = tint;
        renderer.sortingLayerName = "Default";
        child.localPosition = position;
        child.localRotation = rotation;
        renderer.drawMode = SpriteDrawMode.Simple;
        Vector2 sourceSize = sprite != null ? (Vector2)sprite.bounds.size : Vector2.one;
        child.localScale = new Vector3(size.x / Mathf.Max(0.001f, sourceSize.x),
            size.y / Mathf.Max(0.001f, sourceSize.y), 1);
        if (sprite != null)
            child.localPosition -= rotation * Vector3.Scale(sprite.bounds.center, child.localScale);
        return renderer;
    }

    protected static float Dimension(float value, float minimum = 0.1f) =>
        float.IsNaN(value) || float.IsInfinity(value) ? minimum : Mathf.Max(minimum, value);

    private static float Reciprocal(float value) => Mathf.Abs(value) < 0.0001f ? 1f : 1f / value;
}
