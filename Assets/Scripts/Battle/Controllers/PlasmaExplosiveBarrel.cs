using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 特殊电浆爆炸桶：多段生命值，每次受击释放脉冲，死亡播放序列帧动画
/// </summary>
[RequireComponent(typeof(UnitAttrCenter))]
public class PlasmaExplosiveBarrel : MonoBehaviour, IEnemyExplosiveTarget
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
    private bool deathPending; // 最后一段脉冲只登记一次，防止释放前重复触发。

    public PieceController Piece => pieceController;
    public bool CanBeTriggered => !_isDead && pieceController != null && !pieceController.isDead && gameObject.activeInHierarchy;
    public int CountThreatenedPlayers() => CanBeTriggered
        ? EnemyExplosiveTargets.CountPlayers(pieceController, transform.position, explosionRadius, effectLayer, skillPack) : 0;
    private void OnEnable() { _isDead = deathPending = false; currentHealth = maxHealth; EnemyExplosiveTargets.Active.Add(this); }
    private void OnDisable()
    {
        EnemyExplosiveTargets.Active.Remove(this);
        BattleScene.Ins?.BM?.CancelWorldEffects(this);
        BattleScene.Ins?.BM?.ReleaseExplosiveBody(transform);
    }

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
        if (_isDead || deathPending) return;
        var action = new BattleActionContext(pieceController, skillPack);
        if (currentHealth > 1)
        {
            currentHealth--;
            pieceController.unitAttrCenter.SetHealth(1);
            Debug.Log($"{barrelName} 受到伤害，释放电浆脉冲！");
            BattleScene.Ins.BM.ScheduleWorldEffect(this, delay, ExecuteExplosionLogic, action);
        }
        else
        {
            deathPending = true;
            BattleScene.Ins.BM.ScheduleWorldEffect(this, delay, () =>
            {
                ExecuteExplosionLogic();
                OnDead();
            }, action);
            
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
            BattleScene.Ins.BM.ScheduleWorldEffect(this, animLength, () => gameObject.SetActive(false));
        }
        else
        {
            BattleScene.Ins.BM.ScheduleWorldEffect(this, 0, () => gameObject.SetActive(false));
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
        var targetPieces = EnemyExplosiveTargets.CollectTargets(pieceController, transform.position, explosionRadius, effectLayer);

        // 3. 执行战斗后端逻辑
        BattleScene.Ins.BM.PieceSkill(pieceController, targetPieces, skillPack.WithDeadCaster(), transform.position);

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
