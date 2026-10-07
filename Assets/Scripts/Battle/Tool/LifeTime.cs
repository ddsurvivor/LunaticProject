using DG.Tweening;
using UnityEngine;

/// <summary>池对象每次启用独立计时，旧生命周期不能回收新一轮对象。</summary>
public class LifeTime : MonoBehaviour
{
    public float lifeTime;
    private Tween expiry;

    private void OnEnable()
    {
        CancelExpiry();
        if (lifeTime <= 0f) return;
        expiry = DOVirtual.DelayedCall(lifeTime, () => gameObject.SetActive(false), false);
    }

    public void CancelExpiry()
    {
        expiry?.Kill();
        expiry = null;
    }

    private void OnDisable() => CancelExpiry();
}
