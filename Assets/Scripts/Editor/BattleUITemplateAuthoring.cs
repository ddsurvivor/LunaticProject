using UnityEditor;
using UnityEngine;

/// <summary>战斗与设施模板共用的 AVG 模块创建入口，仅供编辑器首次生成资产。</summary>
internal static class BattleUITemplateAuthoring
{
    /// <summary>保留嵌套预制体连接，并记录模块名称与布局覆盖。</summary>
    internal static RectTransform Module(string module, string name, Transform parent, Vector2 size, Vector2 position)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/UI/AvgCommon/" + module + ".prefab");
        if (prefab == null) throw new System.InvalidOperationException("缺少 AVG 模块：" + module);
        var node = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        node.name = name;
        var rect = (RectTransform)node.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = size; rect.anchoredPosition = position;
        PrefabUtility.RecordPrefabInstancePropertyModifications(node);
        PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
        return rect;
    }

    /// <summary>设置初始文字外观；嵌套模块记录覆盖，普通新建文本直接保存。</summary>
    internal static void SetText(UnityEngine.UI.Text text, string value, Font font, int size)
    {
        if (text == null) throw new System.InvalidOperationException("AVG 模块缺少文字。");
        text.text = value; text.font = font; text.fontSize = size;
        text.color = GameConst.UIColors.Text; text.raycastTarget = false;
        if (PrefabUtility.IsPartOfPrefabInstance(text)) PrefabUtility.RecordPrefabInstancePropertyModifications(text);
    }
}
