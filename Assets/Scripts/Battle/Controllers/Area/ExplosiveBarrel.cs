using UnityEngine;

public class ExplosiveBarrel : MonoBehaviour
{
    [Header("爆炸设置")]
    [SerializeField] private float explosionRadius = 5f;   // 爆炸半径

    [SerializeField] private float offset = 1f;
    [SerializeField] private float maxDamage = 100f;      // 中心最大伤害
    [SerializeField] private LayerMask effectLayer;       // 建议设置层级，过滤掉不需要检测的物体

    [Header("视觉特效")]
    [SerializeField] private GameObject explosionEffect;  // 爆炸粒子预制体

    public AudioClip sound;
    public GameObject highlightEffect;

    private bool _hasExploded = false;
    
    

    public void Start()
    {
        GetComponent<UnitAttrCenter>().Init();
        highlightEffect.transform.localScale = explosionRadius*offset*Vector3.one;
    }
    /// <summary>
    /// 对接你的生命值系统：当生命值归零时调用
    /// </summary>
    public void OnDead()
    {
        if (_hasExploded) return;
        _hasExploded = true;

        Explode();
    }

    private void Explode()
    {
        // 1. 产生视觉特效
        if (explosionEffect != null)
        {
            Instantiate(explosionEffect, transform.position, Quaternion.identity);
        }

        // 2. 核心：寻找爆炸范围内的所有碰撞体
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius, effectLayer);

        foreach (Collider hit in colliders)
        {
            // 3. 尝试获取 PieceController
            // 注意：如果你的 PieceController 在父物体上，请使用 GetComponentInParent
            var piece = hit.GetComponent<PieceController>();

            if (piece != null)
            {
                // 计算伤害衰减（距离中心越近伤害越高）
                //float distance = Vector3.Distance(transform.position, hit.transform.position);
                //float damageMultiplier = 1f - Mathf.Clamp01(distance / explosionRadius);
                //float finalDamage = maxDamage;

                AttackPack explosiveDamage =
                    new AttackPack(Mathf.RoundToInt(maxDamage), DamageType.Electric);
                // 4. 调用伤害接口（假设你的 PieceController 有 TakeDamage 方法）
                piece.unitAttrCenter.TakeDamage(explosiveDamage);
            }
        }
        
        // 播放音效
        AudioSource.PlayClipAtPoint(sound, Camera.main.transform.position);

        // 6. 最后销毁桶本身（或者更换为残骸模型）
        //Destroy(gameObject);
        gameObject.SetActive(false);
    }

    // 在编辑器里画出爆炸范围，方便调试
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
        highlightEffect.transform.localScale = explosionRadius*offset*Vector3.one;
    }
}