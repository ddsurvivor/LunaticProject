using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider), typeof(NavMeshObstacle), typeof(CaverSlot))]
[AddComponentMenu("战斗搭建/障碍物与掩体")]
public sealed class BattleObstacle : BattlePlaceable
{
    [LabelText("尺寸（宽 / 高 / 深）")] public Vector3 size = new Vector3(4, 2, 2);
    [LabelText("图片")] [PreviewField] public Sprite sprite;
    [LabelText("图片颜色")] public Color tint = Color.white;
    [LabelText("图片倾角")] [Range(0, 90)] public float spriteTilt = 45;
    [LabelText("作为掩体")] public bool providesCover = true;
    [LabelText("掩体等级")] [Range(1, 3)] public int coverLevel = 1;
    [HideInInspector] public int evadeChance = 20; // 兼容旧场景，规则数值统一由 GameConstSO 提供。
    [HideInInspector] public int damageReduction = 20;

    protected override void ApplyParameters()
    {
        size = new Vector3(Dimension(size.x), Dimension(size.y), Dimension(size.z));
        gameObject.layer = LayerMask.NameToLayer("Wall");
        var box = GetComponent<BoxCollider>();
        box.isTrigger = false;
        box.size = size;
        box.center = Vector3.up * (size.y * 0.5f);
        var obstacle = GetComponent<NavMeshObstacle>();
        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.center = box.center;
        obstacle.size = size;
        obstacle.carving = true;
        obstacle.carveOnlyStationary = false;
        SetSprite("Visual", sprite, Vector3.up * size.y * 0.5f,
            SpriteRotation(spriteTilt), SpriteSize(sprite, size.x, size.y), tint);

        var cover = GetComponent<CaverSlot>();
        cover.providesCover = providesCover;
        cover.coverLevel = Mathf.Clamp(coverLevel, 1, 3);
        // 槽位仅供旧场景站位兼容，是否提供掩体由开关决定。
        var point = Child("CoverPoint");
        point.localPosition = new Vector3(0, 0, -size.z * 0.5f - 0.75f);
        cover.slotTransforms = providesCover ? new[] { point } : new Transform[0];
        var highlight = Child("Highlight");
        highlight.gameObject.SetActive(false);
        cover.highlightEffect = highlight.gameObject;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.color = providesCover ? Color.cyan : Color.yellow;
        Gizmos.DrawWireCube(Vector3.up * size.y * 0.5f, size);
    }
}
