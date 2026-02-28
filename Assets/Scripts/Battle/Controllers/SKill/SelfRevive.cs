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
    [ReadOnly] public int reviveCount = 1; // 可复活次数，默认为1次
    
    public List<Sprite> deadSpriteList; // 死亡时的图片列表
    public List<Sprite> reviveSpriteList; // 复活时的图片列表

    public void OnDead()
    {
        if (reviveCount > 0)
        {
            isFakeDead = true;
            piece.unitAttrCenter.SetHealth(1);
        }
    }
    

    public void Revive()
    {
        if (piece != null && isFakeDead)
        {
            // 满血满状态复活
            piece.Init(piece.player);
            piece.isActived = true;
            piece.unitAttrCenter.SetHealth(revivePercent);
            isFakeDead = false;
            //reviveCount--;
        }
    }

    public void TrueDeath()
    {
        DOVirtual.DelayedCall(0.5f, () =>
        {
            piece.pieceDisplay.PlayFrame(deadSpriteList, () =>
            {
                piece.pieceDisplay.pieceSpriteRenderer.DOFade(0f, 0.8f).OnComplete(() =>
                {
                    this.gameObject.SetActive(false);
                    piece.unitAttrCenter.SetHealth(0);
                    BattleScene.Ins.BM.PlayerCheckWin();
                });
            });
        });
    }
}