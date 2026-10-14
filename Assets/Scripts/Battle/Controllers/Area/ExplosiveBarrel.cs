using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class ExplosiveBarrel : MonoBehaviour, IEnemyExplosiveTarget
{
    public string barrelName = "爆炸桶";
    [Header("爆炸设置")]
    [SerializeField] private float explosionRadius = 5f;   // 爆炸半径

    [SerializeField] private float offset = 1f;
    [SerializeField] private float delayTime = 0.5f; // 爆炸延迟时间
    public SkillPack skillPack;
    //[SerializeField] private float maxDamage = 100f;      // 中心最大伤害
    [SerializeField] private LayerMask effectLayer;       // 建议设置层级，过滤掉不需要检测的物体

    [Header("视觉特效")]
    //[SerializeField] private GameObject explosionEffect;  // 爆炸粒子预制体

    //public AudioClip sound;
    public GameObject highlightEffect;

    private bool _hasExploded = false;
    public PieceController pieceController;
    public  bool needInit = true;
    
    public bool selfDestroy = true; // 是否在爆炸后销毁自身

    public PieceController Piece => pieceController;
    public bool CanBeTriggered => !_hasExploded && pieceController != null && !pieceController.isDead && gameObject.activeInHierarchy;
    public int CountThreatenedPlayers() => CanBeTriggered
        ? EnemyExplosiveTargets.CountPlayers(pieceController, transform.position, explosionRadius, effectLayer, skillPack) : 0;
    private void OnEnable() => EnemyExplosiveTargets.Active.Add(this);
    private void OnDisable() => EnemyExplosiveTargets.Active.Remove(this);

    public void Start()
    {
        if (needInit && pieceController != null)
        {
            pieceController.Init(null, new PieceData()
            {
                pieceName = barrelName,
                maxHealth = 2,
            });
            GetComponent<UnitAttrCenter>().Init();
            highlightEffect.transform.localScale = explosionRadius*offset*Vector3.one;
        }
    }
    /// <summary>
    /// 对接你的生命值系统：当生命值归零时调用
    /// </summary>
    public void OnDead()
    {
        if (_hasExploded) return;
        _hasExploded = true;

        DOVirtual.DelayedCall(delayTime, Explode, false);
        //Explode();
    }

    private void Explode()
    {
        // 1. 产生视觉特效
        /*if (explosionEffect != null)
        {
            Instantiate(explosionEffect, transform.position, Quaternion.identity);
        }*/
        ObjectPool.Ins.GenerateObject(
            skillPack.skillVFXType,
            transform.position + Vector3.up * 3f, Quaternion.identity);

        // 2. 核心：寻找爆炸范围内的所有碰撞体
        var targetPieces = EnemyExplosiveTargets.CollectTargets(pieceController, transform.position, explosionRadius, effectLayer);
        BattleScene.Ins.BM.PieceSkill(pieceController, targetPieces, skillPack.WithDeadCaster(), transform.position);
        
        // 播放音效
        AudioSource.PlayClipAtPoint(skillPack.skillSound, Camera.main.transform.position);
        
        // 6. 最后销毁桶本身（或者更换为残骸模型）
        //Destroy(gameObject);
        if(selfDestroy) DOVirtual.DelayedCall(0.8f, () => gameObject.SetActive(false));
        Debug.Log($"{barrelName} 爆炸，影响了 {targetPieces.Count} 个目标");
    }

    // 在编辑器里画出爆炸范围，方便调试
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
        highlightEffect.transform.localScale = explosionRadius*offset*Vector3.one;
    }

    private void OnMouseEnter()
    {
        if(highlightEffect!= null) highlightEffect.SetActive(true);
    }

    private void OnMouseExit()
    {
        if (highlightEffect!= null)
        {
            highlightEffect.SetActive(false);
        }
    }
}
