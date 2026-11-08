using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

/// <summary>将方尖碑草稿接入正式棋子目录，使用用户授权的临时战斗数值。</summary>
public static class ObeliskCombatAuthoring
{
    public const string PrefabPath = "Assets/Prefab/Battle/Enemy/Enemy10.prefab";
    public const string SkillsPath = "Assets/ScriptableObjects/ObeliskSkills.asset";
    public const string CatalogPath = "Assets/ScriptableObjects/PieceDataListSO.asset";

    /// <summary>首次创建方尖碑战斗配置与独立技能库；已有配置不覆盖，部分缺失时停止以便人工处理。</summary>
    [MenuItem("Tools/Battle/Obelisk/Create Combat Configuration If Missing")]
    public static void CreateIfMissing()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请在编辑模式创建方尖碑。");
        var catalog = AssetDatabase.LoadAssetAtPath<PieceDataListSO>(CatalogPath);
        if (catalog == null) throw new InvalidOperationException("缺少正式棋子目录。");
        var found = catalog.pieceDataList.Where(p => p != null && p.pieceId == 1010).ToArray();
        bool hasPrefab = File.Exists(PrefabPath), hasSkills = File.Exists(SkillsPath);
        if (found.Length == 1 && found[0].pieceName == "方尖碑" && hasPrefab && hasSkills)
        { Debug.Log("方尖碑战斗配置已存在；手动配置不会被覆盖。"); return; }
        if (found.Length > 0 || hasPrefab || hasSkills) throw new InvalidOperationException("方尖碑编号1010或产出路径已被占用/配置不完整，请先检查。");
        ObeliskDraftAuthoring.CreateIfMissing();
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(ObeliskDraftAuthoring.PrefabPath);
        var root = (GameObject)PrefabUtility.InstantiatePrefab(source);
        try
        {
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
            var skills = ScriptableObject.CreateInstance<SkillPackListSO>();
            var melee = Attack("obelisk_melee", "方尖碑·机械撞击", DamageType.Melee, 8, 1.8f, SkillReachKind.Melee);
            var ranged = Attack("obelisk_ranged", "方尖碑·热能脉冲", DamageType.Ranged, 12, 7, SkillReachKind.Ranged);
            skills.Register(melee); skills.Register(ranged);
            AssetDatabase.CreateAsset(skills, SkillsPath);
            var data = new PieceData
            {
                pieceId = 1010, pieceName = "方尖碑", maxHealth = 60,
                maxMovePoint = 4, moveRange = 3, speed = 8, maxAmmoCount = 6,
                critDamageRate = 150, memeBodyType = MemeBodyType.Mechanical,
                memeTargetTags = new List<string> { "黑衣人" },
                permanentMemeImmunities = new List<int> { 1005 },
                skillLibrary = skills, meleeSkillId = melee.skillId, rangedSkillId = ranged.skillId,
                levelGrowth = new Dictionary<EnemyGrowthAttribute, float>
                {
                    { EnemyGrowthAttribute.MaxHealth, 2 },
                    { EnemyGrowthAttribute.KineticAttack, .1f }, { EnemyGrowthAttribute.ThermalAttack, .15f },
                    { EnemyGrowthAttribute.KineticArmor, .05f }, { EnemyGrowthAttribute.ThermalArmor, .1f }
                }
            };
            data._armorDic[DamageType.Ranged] = 10; data._armorDic[DamageType.Melee] = 5; data._armorDic[DamageType.Electric] = 0;
            var enemy = root.GetComponent<EnemyController>();
            enemy.enabled = true; enemy.isActived = true; enemy.navigate = false;
            enemy.enemyAIType = EnemyAIType.WeakestDefenseShooter;
            enemy.level = 1; enemy.turnSpeedOverride = 0; enemy.deadNotDelete = false;
            enemy.availableActions = new List<ActionType> { ActionType.Move, ActionType.MeleeAttack, ActionType.RangedAttack, ActionType.Reload, ActionType.Wait };
            typeof(PieceController).GetField("_pieceData", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(enemy, data);
            var attrs = enemy.unitAttrCenter;
            attrs.ATK = 0; attrs.CON = 2; attrs.critRate = 0; attrs.critDamageRate = data.critDamageRate;
            var serialized = new SerializedObject(attrs);
            serialized.FindProperty("_maxMovePoint").intValue = data.maxMovePoint;
            serialized.FindProperty("_curMovePoint").intValue = data.maxMovePoint;
            serialized.FindProperty("_moveRange").floatValue = data.moveRange;
            serialized.FindProperty("_maxAmmoCount").intValue = data.maxAmmoCount;
            serialized.FindProperty("_ammoCount").intValue = data.maxAmmoCount;
            serialized.FindProperty("_maxManaPoint").intValue = 0;
            serialized.FindProperty("_manaPoint").intValue = 0;
            serialized.FindProperty("_tauntValue").intValue = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            // 临时普攻共用现有施放序列，不保留来源黑衣人的攻击帧。
            var display = enemy.pieceDisplay;
            display.meleeSprites = new List<Sprite>(display.skillSpriteList[0]);
            display.rangeSprites = new List<Sprite>(display.skillSpriteList[0]);
            MatchBlackSuitHeight(display.pieceSpriteRenderer);
            data.turnPortrait = display.idleSprite[0];
            var agent = root.GetComponent<NavMeshAgent>();
            agent.enabled = true; agent.speed = 3.5f;
            root.name = "Enemy10_Obelisk"; root.SetActive(true);
            root.AddComponent<ObeliskAura>();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            catalog.pieceDataList.Add(data);
            EditorUtility.SetDirty(catalog); EditorUtility.SetDirty(skills);
            AssetDatabase.SaveAssets();
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    /// <summary>将正式方尖碑的可见主体调为黑衣人等高，按主体底边落地，保留根节点与战斗配置。</summary>
    [MenuItem("Tools/Battle/Obelisk/Match Black Suit Height")]
    public static void ApplyBlackSuitHeight()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请在编辑模式调整方尖碑体型。");
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            MatchBlackSuitHeight(root.GetComponent<PieceDisplay>().pieceSpriteRenderer);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void MatchBlackSuitHeight(SpriteRenderer renderer)
    {
        var reference = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Battle/Enemy/Enemy05.prefab")
            .GetComponent<PieceDisplay>().pieceSpriteRenderer;
        var referenceBounds = VisibleBounds(reference.sprite);
        var bounds = VisibleBounds(renderer.sprite);
        float height = referenceBounds.height * Mathf.Abs(reference.transform.lossyScale.y); // 黑衣人非透明主体高度。
        float scale = height / bounds.height;
        // 两张序列帧的透明留白不同，以可见主体而非整张画布校准体型和落脚点。
        renderer.transform.localScale = Vector3.one * scale;
        renderer.transform.localPosition = -(renderer.transform.localRotation *
            new Vector3(bounds.center.x, bounds.yMin, 0) * scale);
    }

    private static Rect VisibleBounds(Sprite sprite)
    {
        var texture = new Texture2D(2, 2);
        try
        {
            if (!texture.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite))))
                throw new InvalidOperationException("无法读取精灵像素：" + sprite.name);
            var pixels = texture.GetPixels32();
            int left = texture.width, bottom = texture.height, right = -1, top = -1;
            var rect = sprite.rect;
            for (int y = (int)rect.yMin; y < (int)rect.yMax; y++)
                for (int x = (int)rect.xMin; x < (int)rect.xMax; x++)
                    if (pixels[y * texture.width + x].a > 0)
                    { left = Mathf.Min(left, x); bottom = Mathf.Min(bottom, y); right = Mathf.Max(right, x); top = Mathf.Max(top, y); }
            if (right < left) throw new InvalidOperationException("精灵没有可见像素：" + sprite.name);
            return new Rect((left - rect.x - sprite.pivot.x) / sprite.pixelsPerUnit,
                (bottom - rect.y - sprite.pivot.y) / sprite.pixelsPerUnit,
                (right - left + 1) / sprite.pixelsPerUnit, (top - bottom + 1) / sprite.pixelsPerUnit);
        }
        finally { UnityEngine.Object.DestroyImmediate(texture); }
    }

    /// <summary>为已存在的正式方尖碑补入逆转敌我永久免疫，不覆盖其他手动棋子配置。</summary>
    [MenuItem("Tools/Battle/Obelisk/Apply Permanent Inversion Immunity")]
    public static void ApplyPermanentInversionImmunity()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请在编辑模式配置方尖碑免疫。");
        var catalog = AssetDatabase.LoadAssetAtPath<PieceDataListSO>(CatalogPath);
        var data = catalog?.pieceDataList.SingleOrDefault(p => p != null && p.pieceId == 1010);
        if (data == null || data.pieceName != "方尖碑") throw new InvalidOperationException("缺少唯一的正式方尖碑配置。");
        data.permanentMemeImmunities ??= new List<int>();
        if (data.permanentMemeImmunities.Contains(1005)) return;
        Undo.RecordObject(catalog, "配置方尖碑永久免疫");
        data.permanentMemeImmunities.Add(1005);
        EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
    }

    /// <summary>为已存在的方尖碑补入确认的被动光环和永久免疫，保留已有光环的手动参数。</summary>
    [MenuItem("Tools/Battle/Obelisk/Apply Confirmed Support Configuration")]
    public static void ApplySupportConfiguration()
    {
        ApplyPermanentInversionImmunity();
        var memes = AssetDatabase.LoadAssetAtPath<MemeCatalogSO>("Assets/Resources/Battle/MemeCatalog.asset");
        var dragon = memes?.GetDefinition(1006);
        if (dragon != null && !(dragon.description ?? "").Contains("方尖碑"))
        {
            dragon.description += " 对方尖碑施加成功时立即秒杀，并向半径8内的黑衣人一次传播；其他方尖碑也被击杀但不再扩散。";
            EditorUtility.SetDirty(memes); AssetDatabase.SaveAssets();
        }
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            if (root.GetComponent<ObeliskAura>() != null) return;
            root.AddComponent<ObeliskAura>();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    /// <summary>向指定关卡AI登记一只初始方尖碑；重复调用复用已有实例，不改动原迷雾与增援配置。</summary>
    public static EnemyController AddToSceneIfMissing(AIController ai)
    {
        if (ai == null || ai.pieces == null) throw new ArgumentNullException(nameof(ai));
        var existing = ai.pieces.OfType<EnemyController>().FirstOrDefault(p => p != null && p.pieceID == 1010);
        if (existing != null) return existing;
        var anchor = ai.pieces.FirstOrDefault(p => p != null);
        if (anchor == null) throw new InvalidOperationException("关卡缺少用于定位的原有敌人。");
        Vector3 position = Vector3.zero; bool placed = false;
        // 沿原敌人附近的烘焙导航网格选空位，避免新增敌人与现有棋子重叠。
        foreach (var offset in new[] { new Vector3(3, 0, 0), new Vector3(-3, 0, 0), new Vector3(0, 0, 3), new Vector3(0, 0, -3) })
            if (NavMesh.SamplePosition(anchor.transform.position + offset, out var hit, 2, NavMesh.AllAreas) &&
                ai.pieces.Where(p => p != null).All(p => Vector3.Distance(p.transform.position, hit.position) > 1.8f))
            { position = hit.position; placed = true; break; }
        if (!placed) throw new InvalidOperationException("原敌人附近没有适合方尖碑的导航空位。");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var root = (GameObject)PrefabUtility.InstantiatePrefab(prefab, ai.gameObject.scene);
        root.transform.SetParent(anchor.transform.parent, true); root.transform.position = position;
        var enemy = root.GetComponent<EnemyController>();
        ai.pieces.Add(enemy); EditorUtility.SetDirty(ai);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(ai.gameObject.scene);
        return enemy;
    }

    private static SkillPack Attack(string id, string name, DamageType damage, int amount, float range, SkillReachKind reach)
    {
        return new SkillPack { skillId = id, skillName = name, description = "临时战斗配置；后续平衡调整以棋子目录和技能库为准。",
            target = SkillTarget.Enemy, rangeType = RangeType.Circle, rangeValue = range, reachKind = reach,
            attackPacks = new List<AttackPack> { new AttackPack(amount, damage) } };
    }
}
