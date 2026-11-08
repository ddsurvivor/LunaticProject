using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

/// <summary>制作尚未定稿数值的方尖碑结构草稿，不登记到棋子目录或战斗关卡。</summary>
public static class ObeliskDraftAuthoring
{
    public const string PrefabPath = "Assets/Prefab/Battle/Enemy/Drafts/Obelisk_Draft.prefab";
    private const string SpriteFolder = "Assets/A美术/BattleSprites/INST1方尖碑";

    /// <summary>首次复制黑衣人结构并绑定方尖碑帧；草稿保持停用，已有文件不覆盖。</summary>
    [MenuItem("Tools/Battle/Obelisk/Create Draft If Missing")]
    public static void CreateIfMissing()
    {
        if (File.Exists(PrefabPath)) { Debug.Log("方尖碑草稿已存在，请手动编辑；不会覆盖。"); return; }
        var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Battle/Enemy/Enemy05.prefab");
        if (source == null) throw new InvalidOperationException("缺少黑衣人来源预制体。");
        var root = (GameObject)PrefabUtility.InstantiatePrefab(source);
        root.SetActive(false);
        try
        {
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
            root.name = "Obelisk_Draft"; root.tag = "Piece"; root.layer = 11;
            var enemy = root.GetComponent<EnemyController>();
            var display = root.GetComponent<PieceDisplay>();
            var attrs = root.GetComponent<UnitAttrCenter>();
            if (enemy == null || display == null || attrs == null || display.pieceSpriteRenderer == null)
                throw new InvalidOperationException("来源缺少核心棋子绑定。");
            // 数值未定的草稿不能参与调度，保留控制器供后续正式接入。
            enemy.enabled = false; enemy.isActived = false; enemy.turnSpeedOverride = 0;
            enemy.pieceID = 1010; enemy.level = 1;
            enemy.OnInit = new UnityEvent(); enemy.OnTurnStart = new UnityEvent();
            enemy.OnTurnEnd = new UnityEvent(); enemy.OnHurt = new UnityEvent(); enemy.OnDead = new UnityEvent();
            enemy.availableSkills.Clear(); enemy.availableActions.Clear(); enemy.availablePassives.Clear();
            foreach (var component in root.GetComponentsInChildren<SelfHeal>(true)) UnityEngine.Object.DestroyImmediate(component);
            foreach (var component in root.GetComponentsInChildren<SelfExploreSummonPiece>(true)) UnityEngine.Object.DestroyImmediate(component);
            var data = new PieceData { pieceId = 1010, pieceName = "方尖碑（草稿）", maxHealth = 60,
                speed = 0, memeBodyType = MemeBodyType.Mechanical, memeTargetTags = new List<string> { "黑衣人" } };
            data._armorDic[DamageType.Ranged] = 10; data._armorDic[DamageType.Melee] = 5; data._armorDic[DamageType.Electric] = 0;
            typeof(PieceController).GetField("_pieceData", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(enemy, data);
            var attr = new AttrCenter();
            attr.SetArmor(DamageType.Ranged, 10); attr.SetArmor(DamageType.Melee, 5); attr.SetArmor(DamageType.Electric, 0);
            typeof(UnitAttrCenter).GetField("_attr", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(attrs, attr);
            attrs.ATK = 0; attrs.CON = 0;
            attrs.OnDead = new UnityEvent();
            var serialized = new SerializedObject(attrs);
            serialized.FindProperty("_maxHealth").intValue = 60; serialized.FindProperty("_curHealth").intValue = 60;
            serialized.FindProperty("_maxMovePoint").intValue = 0; serialized.FindProperty("_curMovePoint").intValue = 0;
            serialized.FindProperty("_moveRange").floatValue = 0; serialized.ApplyModifiedPropertiesWithoutUndo();
            display.idleSprite = Frames("idle"); display.moveSprite = Frames("move");
            display.hitSprite = Frames("hit"); display.deathSprites = Frames("death");
            display.skillSpriteList = new List<List<Sprite>> { Frames("skill") };
            display.meleeSprites = new List<Sprite>(); display.rangeSprites = new List<Sprite>(); display.dodgeSprite = new List<Sprite>();
            display.pieceSpriteRenderer.sprite = display.idleSprite[0];
            var spriteTransform = display.pieceSpriteRenderer.transform;
            float scale = 2.4f / display.idleSprite[0].bounds.size.y;
            spriteTransform.localScale = Vector3.one * scale;
            var bounds = display.idleSprite[0].bounds;
            spriteTransform.localPosition = -(spriteTransform.localRotation * new Vector3(bounds.center.x, bounds.min.y, 0) * scale);
            var box = root.GetComponent<BoxCollider>();
            if (box != null) { box.center = new Vector3(0, 1, 0); box.size = new Vector3(1.4f, 2, 1.4f); }
            if (enemy.enemyCanvas != null) enemy.enemyCanvas.transform.localPosition = new Vector3(0, 2.2f, 0);
            var agent = root.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null) { agent.enabled = false; agent.radius = .7f; agent.height = 2; }
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath); AssetDatabase.SaveAssets();
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static List<Sprite> Frames(string action)
    {
        var files = Directory.GetFiles(SpriteFolder, "INST1" + action + "*.png")
            .Where(path => System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileNameWithoutExtension(path), "^INST1" + action + "[0-9]{4}$"))
            .OrderBy(path => path, StringComparer.Ordinal).ToArray();
        var frames = files.Select(path => AssetDatabase.LoadAssetAtPath<Sprite>(path.Replace('\\', '/'))).ToList();
        if (frames.Count == 0 || frames.Any(sprite => sprite == null)) throw new InvalidOperationException("方尖碑序列帧缺失：" + action);
        return frames;
    }
}
