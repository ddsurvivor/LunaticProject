using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 特殊电浆爆炸桶：多段生命值，每次受击释放脉冲，死亡播放序列帧动画
/// </summary>
[RequireComponent(typeof(UnitAttrCenter))]
public class PlasmaExplosiveBarrel : MonoBehaviour
{
    [Header("基础配置")] public string barrelName = "电浆爆炸桶";
    [SerializeField] private int maxHealth = 4;
    private int currentHealth;

    [Header("爆炸设置")] [SerializeField] private float explosionRadius = 6f;
    [SerializeField] private float offset = 1.1f;
    [SerializeField] private float delay = 0.8f;
    [SerializeField] private LayerMask effectLayer;
    public SkillPack skillPack;

    [Header("视觉与动画")] public GameObject highlightEffect;
    [SerializeField] private Animator plasmaAnimator; // 用于播放死亡序列帧的动画机
    [SerializeField] private string deathAnimationState = "Death";

    public PieceController pieceController;
    private bool _isDead = false;

    private void Start()
    {
        // 1. 初始化生命系统，设置为 4 格血
        pieceController.Init(null, new PieceData()
        {
            pieceName = barrelName, maxHealth = maxHealth,
        });
        currentHealth = maxHealth;

        GetComponent<UnitAttrCenter>().Init();

        // 2. 初始化范围显示
        if (highlightEffect != null)
        {
            highlightEffect.transform.localScale = explosionRadius * offset * Vector3.one;
        }

        // 3. 订阅伤害事件 (假设你的 PieceController 有 OnTakenDamage 委托)
        // 如果没有委托，需要在 PieceController 的 TakeDamage 方法中反向调用此脚本
        //pieceController.OnHurt += TriggerPlasmaPulse;
    }

    /// <summary>
    /// 每次被打掉血时触发的电浆脉冲
    /// </summary>
    public void TriggerPlasmaPulse()
    {
        if (currentHealth > 1)
        {
            currentHealth--;
            pieceController.unitAttrCenter.SetHealth(1);
            Debug.Log($"{barrelName} 受到伤害，释放电浆脉冲！");
            DOVirtual.DelayedCall(delay, ExecuteExplosionLogic, false);
        }
        else
        {
            DOVirtual.DelayedCall(delay, ()=>
            {
                ExecuteExplosionLogic();
                OnDead();
            }, false);
            
        }
    }

    /// <summary>
    /// 死亡逻辑：停止脉冲监听，播放死亡动画
    /// </summary>
    private void OnDead()
    {
        if (_isDead) return;
        _isDead = true;

        // 取消事件订阅，防止死亡瞬间多次触发
        //pieceController.OnTakenDamage -= TriggerPlasmaPulse;

        // 播放死亡序列帧动画
        if (plasmaAnimator != null)
        {
            plasmaAnimator.SetTrigger(deathAnimationState);

            // 获取动画长度并延迟销毁/失活
            float animLength = plasmaAnimator.GetCurrentAnimatorStateInfo(0).length;
            DOVirtual.DelayedCall(animLength, () => gameObject.SetActive(false));
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void ExecuteExplosionLogic()
    {
        // 1. 生成特效
        ObjectPool.Ins.GenerateObject(
            skillPack.skillVFXType,
            transform.position + Vector3.up * 1.5f,
            Quaternion.identity);

        // 2. 范围检测
        Collider[] colliders =
            Physics.OverlapSphere(transform.position, explosionRadius, effectLayer);
        List<PieceController> targetPieces = new List<PieceController>();

        foreach (Collider hit in colliders)
        {
            var piece = hit.GetComponent<PieceController>();
            // 排除自己
            if (piece != null && piece != pieceController && !targetPieces.Contains(piece))
            {
                targetPieces.Add(piece);
            }
        }

        // 3. 执行战斗后端逻辑
        BattleScene.Ins.BM.PieceSkill(pieceController, targetPieces, skillPack);

        // 4. 音效
        if (skillPack.skillSound != null)
        {
            AudioSource.PlayClipAtPoint(skillPack.skillSound, Camera.main.transform.position);
        }
        
        pieceController.ShowHighlight(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }

    private void OnDestroy()
    {
        // 良好的习惯：在销毁时取消订阅
        if (pieceController != null)
        {
            //pieceController.OnTakenDamage -= TriggerPlasmaPulse;
        }
    }
}