using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class Openning : MonoBehaviour
{
    [Header("视频")]
    public VideoPlayer videoPlayer;

    public RenderTexture rt;

    [Header("LOGO图片")]
    public Image logoImage;
    public float logoFadeInTime = 1f;
    public float logoShowTime = 2f;
    public float logoFadeOutTime = 1f;

    [Header("背景音乐")]
    public AudioSource bgmSource;

    [Header("文本")]
    public Text[] texts; // 3行文本
    public float textFadeInTime = 2f;
    public float textShowTime = 3f;
    public float textFadeOutTime = 2f;

    [Header("场景")]
    public string nextSceneName;

    void Start()
    {
        // 初始化所有UI透明
        logoImage.color = new Color(1, 1, 1, 0);
        foreach (var t in texts)
            t.color = new Color(t.color.r, t.color.g, t.color.b, 0);
        //rt.Release();
        videoPlayer.gameObject.SetActive(true);
        StartCoroutine(PlaySequenceOpen());
    }

    IEnumerator PlaySequenceOpen()
    {
        Sequence logoSeq = DOTween.Sequence();
        logoSeq.AppendInterval(0.5f);
        logoSeq.AppendCallback(()=>
        {
            videoPlayer.Play();
        });
        logoSeq.AppendInterval((float)videoPlayer.clip.length +2f);
        logoSeq.Append(logoImage.DOFade(1, logoFadeInTime).SetEase(Ease.Linear));
        logoSeq.AppendInterval(logoShowTime);
        logoSeq.AppendCallback(()=>videoPlayer.gameObject.SetActive(false));
        logoSeq.Append(logoImage.DOFade(0, logoFadeOutTime));
        yield return logoSeq.WaitForCompletion();
        // 3. 播放背景音乐
        bgmSource.Play();
        // 4. 三行文本依次淡入
        Sequence textInSeq = DOTween.Sequence();
        textInSeq.AppendInterval(1f);
        foreach (var t in texts)
        {
            textInSeq.Append(t.DOFade(1, textFadeInTime).SetEase(Ease.Linear));
        }
        textInSeq.AppendInterval(textShowTime);
        yield return textInSeq.WaitForCompletion();

        // 5. 三行文本依次淡出
        Sequence textOutSeq = DOTween.Sequence();
        foreach (var t in texts)
            textOutSeq.Append(t.DOFade(0, textFadeOutTime));
        yield return textOutSeq.WaitForCompletion();

        // 6. 跳转场景
        SceneManager.LoadScene(nextSceneName);
    }
    IEnumerator PlaySequence()
    {
        // 1. 播放视频
        videoPlayer.Play();
        yield return new WaitUntil(() => !videoPlayer.isPlaying);

        // 2. LOGO淡入、显示、淡出
        Sequence logoSeq = DOTween.Sequence();
        logoSeq.Append(logoImage.DOFade(1, logoFadeInTime));
        logoSeq.AppendInterval(logoShowTime);
        logoSeq.Append(logoImage.DOFade(0, logoFadeOutTime));
        yield return logoSeq.WaitForCompletion();

        // 3. 播放背景音乐
        bgmSource.Play();

        // 4. 三行文本依次淡入
        Sequence textInSeq = DOTween.Sequence();
        foreach (var t in texts)
            textInSeq.Append(t.DOFade(1, textFadeInTime));
        textInSeq.AppendInterval(textShowTime);
        yield return textInSeq.WaitForCompletion();

        // 5. 三行文本依次淡出
        Sequence textOutSeq = DOTween.Sequence();
        foreach (var t in texts)
            textOutSeq.Append(t.DOFade(0, textFadeOutTime));
        yield return textOutSeq.WaitForCompletion();

        // 6. 跳转场景
        SceneManager.LoadScene(nextSceneName);
    }

    IEnumerator FadeImage(Image img, float from, float to, float duration)
    {
        float elapsed = 0;
        while (elapsed < duration)
        {
            float alpha = Mathf.Lerp(from, to, elapsed / duration);
            img.color = new Color(img.color.r, img.color.g, img.color.b, alpha);
            elapsed += Time.deltaTime;
            yield return null;
        }
        img.color = new Color(img.color.r, img.color.g, img.color.b, to);
    }

    IEnumerator FadeText(Text txt, float from, float to, float duration)
    {
        float elapsed = 0;
        while (elapsed < duration)
        {
            float alpha = Mathf.Lerp(from, to, elapsed / duration);
            txt.color = new Color(txt.color.r, txt.color.g, txt.color.b, alpha);
            elapsed += Time.deltaTime;
            yield return null;
        }
        txt.color = new Color(txt.color.r, txt.color.g, txt.color.b, to);
    }
}