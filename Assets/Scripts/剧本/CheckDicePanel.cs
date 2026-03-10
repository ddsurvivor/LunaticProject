using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class CheckDicePanel : MonoBehaviour
{
    [Header("骰子六面图片")]
    public Sprite[] diceSprites; // 0-5分别代表1-6点

    [Header("判定结果图片")]
    public Sprite successSprite;
    public Sprite failSprite;

    
   

    [Header("判定结果UI")]
    public Image resultImage;

    [Header("滚动动画设置")]
    public float rollDuration = 1.0f; // 滚动总时长
    public float rollSpeed = 0.1f;    // 每次切换图片间隔
    public float closeDelay = 3f; // 显示结果后自动关闭的延迟时间

    [Header("骰子UI对象（场景预放置）")]
    public List<Image> diceImages = new List<Image>();

    /// <summary>
    /// 展示骰子检定结果
    /// </summary>
    /// <param name="diceCount">骰子数量</param>
    /// <param name="diceResult">每个骰子的结果（1-6）</param>
    /// <param name="isSuccess">是否判定成功</param>
    public void ShowResult(int diceCount, int[] diceResult, bool isSuccess)
    {
        gameObject.SetActive(true);
        // 隐藏所有骰子对象
        for (int i = 0; i < diceImages.Count; i++)
        {
            diceImages[i].gameObject.SetActive(i < diceCount);
        }
        

        // 开始滚动动画
        StartCoroutine(RollDiceCoroutine(diceCount, diceResult, isSuccess));
    }

    private IEnumerator RollDiceCoroutine(int diceCount, int[] diceResult, bool isSuccess)
    {
        float timer = 0f;

        // 滚动动画
        while (timer < rollDuration)
        {
            for (int i = 0; i < diceCount; i++)
            {
                int randomFace = Random.Range(0, diceSprites.Length);
                diceImages[i].sprite = diceSprites[randomFace];
            }
            timer += rollSpeed;
            yield return new WaitForSeconds(rollSpeed);
        }

        // 显示最终结果
        for (int i = 0; i < diceCount; i++)
        {
            int face = Mathf.Clamp(diceResult[i] - 1, 0, diceSprites.Length - 1);
            diceImages[i].sprite = diceSprites[face];
        }

        // 显示判定结果图片
        if (resultImage != null)
        {
            resultImage.sprite = isSuccess ? successSprite : failSprite;
            resultImage.gameObject.SetActive(true);
        }
        yield return new WaitForSeconds(closeDelay);
        // 关闭界面
        gameObject.SetActive(false);
    }
}