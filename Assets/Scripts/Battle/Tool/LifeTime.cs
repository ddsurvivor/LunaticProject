using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 存活时间自动隐藏
/// 用于粒子特效回收
/// </summary>
public class LifeTime : MonoBehaviour
{
    public float lifeTime;
    // Start is called before the first frame update
    void OnEnable()
    {
        if(lifeTime <= 0) return;
        DOVirtual.DelayedCall(lifeTime, () =>
        {
            gameObject.SetActive(false);
        }, false);
    }

}
