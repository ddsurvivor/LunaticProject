using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
[AddComponentMenu("战斗搭建/高层平台")]
public sealed class BattlePlatform : BattlePlaceable
{
    [LabelText("平台尺寸（宽 / 深）")] public Vector2 size = new Vector2(10, 8);
    [LabelText("平台高度")] [MinValue(0.25f)] public float height = 3;
    [LabelText("顶面图片")] [PreviewField] public Sprite topSprite;
    [LabelText("侧面图片")] [PreviewField] public Sprite sideSprite;
    [LabelText("侧面颜色")] public Color sideTint = new Color(0.55f, 0.6f, 0.65f);
    [LabelText("导航 Agent Type ID")] public int agentTypeID;
    [ShowInInspector, ReadOnly, LabelText("导航已生成")] public bool NavigationReady => navigationInstance.valid;
    private NavMeshData navigationData;
    private NavMeshDataInstance navigationInstance;

    protected override void ApplyParameters()
    {
        size = new Vector2(Dimension(size.x, 1), Dimension(size.y, 1));
        height = Dimension(height, 0.25f);
        gameObject.layer = LayerMask.NameToLayer("Ground");
        var box = GetComponent<BoxCollider>();
        box.isTrigger = false;
        box.center = Vector3.up * (height * 0.5f);
        box.size = new Vector3(size.x, height, size.y);
        // 美术已经包含斜视透视；和 Barrel 一样正对固定相机，不能再铺到盒子的五个面上。
        float imageWidth = (size.x + size.y) * 0.7071068f;
        SetSprite("Top", topSprite, Vector3.up * (height + 0.01f), SpriteRotation(),
            SpriteSize(topSprite, imageWidth, 0.1f), Color.white);
        bool hasSide = sideSprite != null && sideSprite != topSprite;
        var front = SetSprite("Front", sideSprite, Vector3.up * (height * 0.5f), SpriteRotation(),
            SpriteSize(sideSprite, imageWidth, height), sideTint);
        front.gameObject.SetActive(hasSide);
        // 兼容旧预制体，关闭曾经把同一张地板图重复贴出的侧面。
        foreach (string childName in new[] { "Back", "Left", "Right" })
        {
            var oldSide = transform.Find(childName);
            if (oldSide != null) oldSide.gameObject.SetActive(false);
        }

        // 只切掉实心平台下方的旧地面导航，顶部留出缝隙，不切掉自己的可走面。
        var blocker = Child("BaseNavigationBlocker");
        blocker.localPosition = Vector3.zero;
        blocker.localRotation = Quaternion.identity;
        blocker.localScale = Vector3.one;
        var obstacle = blocker.GetComponent<NavMeshObstacle>();
        if (obstacle == null) obstacle = blocker.gameObject.AddComponent<NavMeshObstacle>();
        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.center = Vector3.up * (height * 0.5f - 0.15f);
        obstacle.size = new Vector3(size.x, Mathf.Max(0.1f, height - 0.3f), size.y);
        obstacle.carving = true;
        obstacle.carveOnlyStationary = false;
        RebuildNavigation();
    }

    private void RebuildNavigation()
    {
        ClearNavigation();
        if (!isActiveAndEnabled) return;
        var settings = NavMesh.GetSettingsByID(agentTypeID);
        if (settings.agentTypeID < 0) return;
        settings.overrideVoxelSize = true;
        settings.voxelSize = Mathf.Max(0.025f, settings.agentRadius / 4);
        settings.minRegionArea = 0;
        var sources = new List<NavMeshBuildSource>
        {
            new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Box,
                transform = Matrix4x4.TRS(Vector3.up * (height - 0.1f), Quaternion.identity, Vector3.one),
                size = new Vector3(size.x, 0.2f, size.y), area = 0
            }
        };
        navigationData = NavMeshBuilder.BuildNavMeshData(settings, sources,
            new Bounds(Vector3.up * height, new Vector3(size.x + 4, settings.agentHeight * 2 + 4, size.y + 4)),
            Vector3.zero, Quaternion.identity);
        if (navigationData == null) return;
        navigationData.name = name + " Navigation";
        navigationData.hideFlags = HideFlags.DontSave;
        navigationInstance = NavMesh.AddNavMeshData(navigationData, transform.position, transform.rotation);
        navigationInstance.owner = this;
    }

    private void OnDisable() => ClearNavigation();
    private void OnDestroy() => ClearNavigation();
    private void ClearNavigation()
    {
        if (navigationInstance.valid) navigationInstance.Remove();
        navigationInstance = default;
        if (navigationData == null) return;
        if (Application.isPlaying) Destroy(navigationData); else DestroyImmediate(navigationData);
        navigationData = null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(Vector3.up * height, new Vector3(size.x, 0.02f, size.y));
    }
}
