using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*/// <summary>
/// Mono单例模板
/// </summary>
/// <typeparam name="T">单例类型</typeparam>
public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;
    public static T Ins
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<T>();
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (Ins != this)
        {
            Destroy(gameObject);
        }
    }

}*/
// 基于Odin插件，序列化的Mono单例

using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Mono单例模板
/// </summary>
/// <typeparam name="T">单例类型</typeparam>
public class MonoSingleton<T> : SerializedMonoBehaviour where T : SerializedMonoBehaviour
{
    private static T instance;
    public static T Ins
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<T>();
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (Ins != this)
        {
            Destroy(gameObject);
        }
    }

}
