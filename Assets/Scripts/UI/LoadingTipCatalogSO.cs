using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>加载提示的可编辑目录；空正文不会参与轮播。</summary>
[CreateAssetMenu(menuName = "战斗/加载提示目录", fileName = "LoadingTipCatalog")]
public sealed class LoadingTipCatalogSO : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        public string title;
        [TextArea(2, 8)] public string body;
    }

    public List<Entry> tips = new List<Entry>();
}
