using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 角色复活
/// </summary>
public class SelfRevive : MonoBehaviour
{
    public EnemyController piece;
    [LabelText("复活恢复百分比")] [Range(0, 100)] public int revivePercent = 50; // 复活时恢复的生命百分比
    [ReadOnly] public bool isFakeDead = false; // 是否处于假死状态
    [ReadOnly] public int reviveCount = 5; // 剩余复活次数，正式预制体可覆盖初始值。

    public List<Sprite> deadSpriteList; // 死亡时的图片列表
    public List<Sprite> reviveSpriteList; // 复活时的图片列表

    private int compId = 5;
    public bool dropComponentOnDeath = true; // 是否在死亡时掉落插件
    private int initialReviveCount; // 外部重新初始化时恢复配置，普通复活不补充次数。
    private bool reviving; // 区分消费复活机会的Init与外部重新初始化。
    private int trueDeathLife = -1; // 真死引信与表现持有的棋子生命版本。
    private Tween deathFade; // 停用或复用时取消本组件持有的淡出。

    /// <summary>真死表现持有当前生命；期间拒绝再次受击、复活或重新进入行动队列。</summary>
    public bool IsCompletingTrueDeath => piece != null && trueDeathLife == piece.BattleLifeVersion;

    private void Awake() { initialReviveCount = reviveCount; }

    /// <summary>由棋子Init清理旧生命的表现回调；普通复活保留已经消费的复活次数。</summary>
    public void ResetForReuse()
    {
        deathFade?.Kill();
        deathFade = null;
        trueDeathLife = -1;
        isFakeDead = false;
        if (!reviving) reviveCount = initialReviveCount;
    }

    /// <summary>死亡事件进入假死；后续普通回合仍由原棋子触发复活入口。</summary>
    public void OnDead()
    {
        if (!IsCompletingTrueDeath && reviveCount > 0)
        {
            isFakeDead = true;
            piece.unitAttrCenter.SetHealth(1);
        }
    }


    /// <summary>自身普通回合开始时消费一次复活机会，按正式数据恢复指定生命比例。</summary>
    public void Revive()
    {
        if (piece != null && isFakeDead && !IsCompletingTrueDeath && piece.gameObject.activeInHierarchy)
        {
            isFakeDead = false;
            reviveCount--;
            // 沿正式目录重建属性，不能回退到预制体里的旧生命、移动和速度数值。
            reviving = true;
            try { piece.Init(piece.player, piece.pieceData); }
            finally { reviving = false; }
            piece.isActived = true;
            piece.enemyCanvas?.gameObject.SetActive(true);
            piece.unitAttrCenter.SetHealth(revivePercent);
        }
    }

    /// <summary>对假死身体只提交一次真死；保留原引信和表现，旧生命回调不能停用复用后的棋子。</summary>
    public void TrueDeath(bool isBurstHit = false)
    {
        if (piece == null || !isFakeDead || IsCompletingTrueDeath) return;
        trueDeathLife = piece.BattleLifeVersion;
        int life = trueDeathLife; // 引信、帧动画及淡出共同持有同一生命版本。
        reviveCount = 0;
        isFakeDead = false;
        piece.isActived = false;
        if (isBurstHit)
        {
            //奖励玩家一个插件
            if (dropComponentOnDeath)
            {
                // 如果是聚能杀死，则奖励玩家一个插件
                GM.Ins.PLAYERPROFILE.AddComponentToInventory(compId);
            }
        }

        BattleScene.Ins.BM.ScheduleWorldEffect(this, 0.5f, () =>
        {
            if (!IsCurrent(life)) return;
            piece.pieceDisplay.PlayFrame(deadSpriteList, () =>
            {
                if (!IsCurrent(life)) return;
                deathFade = piece.pieceDisplay.pieceSpriteRenderer.DOFade(0f, 0.8f).OnComplete(() =>
                {
                    if (!IsCurrent(life)) return;
                    // 先提交死亡，再停用对象，避免留下不可选中但仍被胜负统计为存活的身体。
                    piece.CompleteTrueDeath();
                });
            });
        });
    }

    private bool IsCurrent(int life) => isActiveAndEnabled && piece != null &&
        piece.BattleLifeVersion == life && trueDeathLife == life;

    private void OnDisable()
    {
        deathFade?.Kill();
        deathFade = null;
        BattleScene.Ins?.BM?.CancelWorldEffects(this);
    }
}
