using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

/// <summary>首次生成群体护盾正式资产；再次执行仅补缺失配置，保留手动参数与预制体覆盖。</summary>
public static class RangeBarrierAuthoring
{
    public const string BodyPath = "Assets/Prefab/Battle/Enemy/ObeliskRangeBarrier.prefab";
    public const string StatusPath = "Assets/Resources/UI/RangeBarrierStatus.prefab";
    public const string SkillId = "obelisk_range_barrier";

    /// <summary>在编辑模式补入已确认的B38技能、发射器、护盾实体及共用血条变体。</summary>
    [MenuItem("Tools/Battle/Obelisk/Apply Confirmed Range Barrier")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请在编辑模式配置护盾。");
        CreateBody();
        CreateStatus();
        var library = AssetDatabase.LoadAssetAtPath<SkillPackListSO>(ObeliskCombatAuthoring.SkillsPath);
        var catalog = AssetDatabase.LoadAssetAtPath<PieceDataListSO>(ObeliskCombatAuthoring.CatalogPath);
        var data = catalog?.pieceDataList.Single(p => p != null && p.pieceId == 1010);
        if (library == null || data == null) throw new InvalidOperationException("缺少方尖碑战斗配置。");
        if (library.GetById(SkillId) == null)
        {
            library.Register(new SkillPack { skillId = SkillId, skillName = "群体护盾", target = SkillTarget.Self,
                rangeType = RangeType.Circle, enemySkillRole = EnemySkillRole.RangeBarrier,
                skillEffects = new List<SkillEffect> { SkillEffect.RangeBarrier },
                description = "消耗2AP，无冷却。生成随身半径8球形护盾，耐久40；仅拦截外到内攻击，破盾溢出穿透。已有护盾时不能重复施放；不阻挡持续伤害。护盾被击破后，自身停机2个普通回合，期间不能行动且治疗光环暂停。" });
            EditorUtility.SetDirty(library);
        }
        data.initialSkillIds ??= new List<string>();
        if (!data.initialSkillIds.Contains(SkillId)) { data.initialSkillIds.Add(SkillId); EditorUtility.SetDirty(catalog); }
        var root = PrefabUtility.LoadPrefabContents(ObeliskCombatAuthoring.PrefabPath);
        try
        {
            bool changed = false;
            var emitter = root.GetComponent<BarrierEmitter>();
            if (emitter == null) { emitter = root.AddComponent<BarrierEmitter>(); changed = true; }
            var serialized = new SerializedObject(emitter);
            var reference = serialized.FindProperty("barrierPrefab");
            if (reference.objectReferenceValue == null)
            {
                reference.objectReferenceValue = AssetDatabase.LoadAssetAtPath<RangeBarrier>(BodyPath);
                serialized.ApplyModifiedPropertiesWithoutUndo(); changed = true;
            }
            var enemy = root.GetComponent<EnemyController>();
            if (!enemy.availableActions.Contains(ActionType.Skill)) { enemy.availableActions.Add(ActionType.Skill); changed = true; }
            if (changed) PrefabUtility.SaveAsPrefabAsset(root, ObeliskCombatAuthoring.PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
    }

    private static List<Sprite> Frames(string prefix) => Enumerable.Range(0, 5)
        .Select(i => AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/A美术/VFX/{prefix}{i:0000}.png"))
        .Select(s => s != null ? s : throw new InvalidOperationException("缺少护盾序列帧。")).ToList();

    private static void CreateBody()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(BodyPath) != null) return;
        var root = new GameObject("ObeliskRangeBarrier");
        root.layer = 11; root.tag = "Piece";
        try
        {
            var barrier = root.AddComponent<RangeBarrier>();
            barrier.unitAttrCenter = root.AddComponent<UnitAttrCenter>();
            barrier.unitAttrCenter.pc = barrier;
            barrier.pieceDisplay = root.AddComponent<PieceDisplay>();
            barrier.OnInit = new UnityEvent(); barrier.OnDead = new UnityEvent(); barrier.OnHurt = new UnityEvent();
            barrier.OnTurnStart = new UnityEvent(); barrier.OnTurnEnd = new UnityEvent();
            barrier.ableMove = false;
            barrier.hightlightEffect = new GameObject("Highlight");
            barrier.hightlightEffect.transform.SetParent(root.transform, false);
            barrier.hightlightEffect.SetActive(false);
            var display = barrier.pieceDisplay;
            display.idleSprite = Frames("FIRESEEDbarrier");
            display.deathSprites = Frames("FIRESEEDbarrierD");
            var visual = new GameObject("ShieldVisual");
            visual.transform.SetParent(root.transform, false);
            var renderer = visual.AddComponent<SpriteRenderer>();
            var sourceRenderer = AssetDatabase.LoadAssetAtPath<GameObject>(ObeliskCombatAuthoring.PrefabPath)
                .GetComponent<EnemyController>().pieceDisplay.pieceSpriteRenderer;
            visual.transform.localRotation = sourceRenderer.transform.localRotation;
            renderer.sortingLayerID = sourceRenderer.sortingLayerID; renderer.sortingOrder = sourceRenderer.sortingOrder + 1;
            renderer.sprite = display.idleSprite[0]; renderer.color = new Color(1, 1, 1, .35f);
            float scale = 16 / renderer.sprite.bounds.size.x;
            visual.transform.localScale = Vector3.one * scale;
            visual.transform.localPosition = new Vector3(-2.2f, 3.5f, 0);
            display.pieceSpriteRenderer = renderer;
            // 独立选择节点避免整球碰撞遮挡内部棋子；球形拦截使用逻辑半径。
            var node = new GameObject("TargetNode"); node.transform.SetParent(root.transform, false);
            node.layer = 11; node.tag = "Piece";
            node.transform.localPosition = new Vector3(0, .6f, 0);
            var nodeRenderer = node.AddComponent<SpriteRenderer>(); nodeRenderer.sprite = display.idleSprite[0];
            node.transform.localRotation = sourceRenderer.transform.localRotation;
            nodeRenderer.sortingLayerID = renderer.sortingLayerID; nodeRenderer.sortingOrder = renderer.sortingOrder + 1;
            node.transform.localScale = Vector3.one * (1.1f / renderer.sprite.bounds.size.x);
            var collider = node.AddComponent<SphereCollider>(); collider.radius = .65f / node.transform.localScale.x;
            PrefabUtility.SaveAsPrefabAsset(root, BodyPath);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static void CreateStatus()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(StatusPath) != null) return;
        var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/UI/PieceOverheadStatus.prefab");
        if (source == null) throw new InvalidOperationException("缺少共用头顶模板。");
        var root = (GameObject)PrefabUtility.InstantiatePrefab(source);
        try
        {
            root.name = "RangeBarrierStatus";
            root.transform.Find("ActionPoints").gameObject.SetActive(false);
            root.transform.Find("Resources").gameObject.SetActive(false);
            var view = new SerializedObject(root.GetComponent<PieceOverheadStatusUI>());
            view.FindProperty("showFullHealth").boolValue = true;
            view.FindProperty("emptyHeight").floatValue = 64;
            view.FindProperty("headHeightOverride").floatValue = 5.5f;
            view.FindProperty("headOffset").vector2Value = new Vector2(200, 12);
            view.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, StatusPath);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
}
