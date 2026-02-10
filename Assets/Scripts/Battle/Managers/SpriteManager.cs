using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;


/// <summary>
/// 图标管理器
/// buff图标和道具图标
/// </summary>
public class SpriteManager : SerializedMonoBehaviour
{
    public Dictionary<BuffType, Sprite> buffIconDic = new Dictionary<BuffType, Sprite>();
    
    public Sprite GetBuffIcon(BuffType buffType)
    {
        if (buffIconDic.TryGetValue(buffType, out Sprite icon))
        {
            return icon;
        }
        else
        {
            Debug.LogWarning($"未找到Buff图标: {buffType}");
            return null; // 或者返回一个默认图标
        }
    }
}