using System;using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public class PieceDisplay : MonoBehaviour
{
    
    // 棋子图片控制脚本，有一个SpriteRenderer用于显示棋子图片
    public SpriteRenderer pieceSpriteRenderer;
    // 有一系列按照规定命名的Sprite资源用于显示不同的棋子图片，分别为idel、move、attack、shoot
    public Sprite idleSprite;
    public Sprite moveSprite;
    public Sprite attackSprite;
    public List<Sprite> shootSprite;
    // 闪避
    public Sprite dodgeSprite;
    public Sprite hitSprite;
    public List<Sprite> deathSprite;
    
    // 每种图片还有背面图
    public Sprite idleBackSprite;
    public Sprite moveBackSprite;
    public Sprite attackBackSprite;
    public Sprite shootBackSprite;
    public Sprite dodgeBackSprite;
    public Sprite hitBackSprite;

    private UnityAction finishAction;
    // 更改显示状态脚本，传入一个状态，和一个持续时间，如果是-1则表示永久更改，否则持续时间结束后恢复到idle状态
    // 如果传入back则显示背面图片
    public void ChangeDisplayState(PieceDisplayState state, bool back = false, float duration = -1f, UnityAction finish = null)
    {
        Debug.Log($"ChangeDisplayState: {state}, back: {back}, duration: {duration}");
        if(pieceSpriteRenderer== null) return;
        finishAction = finish;
        StopAllCoroutines();
        switch (state)
        {
            case PieceDisplayState.Idle:
                pieceSpriteRenderer.sprite = back ? idleBackSprite : idleSprite;
                break;
            case PieceDisplayState.Move:
                pieceSpriteRenderer.sprite = back ? moveBackSprite : moveSprite;
                break;
            case PieceDisplayState.Attack:
                pieceSpriteRenderer.sprite = back ? attackBackSprite : attackSprite;
                break;
            case PieceDisplayState.Shoot:
                StartCoroutine(PlaySpriteAnimation(shootSprite, 0.3f));
                //pieceSpriteRenderer.sprite = back ? shootBackSprite : shootSprite;
                break;
            case PieceDisplayState.Dodge:
                pieceSpriteRenderer.sprite = back ? dodgeBackSprite : dodgeSprite;
                break;
            case PieceDisplayState.Death:
                // 按顺序按固定时间间隔播放死亡动画
                StartCoroutine(PlaySpriteAnimation(deathSprite, 0.2f));
                break;
            case PieceDisplayState.Hit:
                pieceSpriteRenderer.sprite = back ? hitBackSprite : hitSprite;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
        if (duration > 0)
        {
            StartCoroutine(RevertToIdleAfterDelay(duration));
        }
    }
    
    private IEnumerator RevertToIdleAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        pieceSpriteRenderer.sprite = idleSprite;
        finishAction?.Invoke();
    }
    
    public void Dead()
    {
        // 死亡后隐藏棋子
        pieceSpriteRenderer.DOFade(0f, 0.5f);
    }

    private IEnumerator PlaySpriteAnimation(List<Sprite> sprites, float frameDuration)
    {
        foreach (var sprite in sprites)
        {
            pieceSpriteRenderer.sprite = sprite;
            yield return new WaitForSeconds(frameDuration);
        }
        finishAction?.Invoke();
    }
}

public enum PieceDisplayState
{
    Idle,
    Move,
    Attack,
    Shoot,
    Dodge,
    Hit,
    Death
}
