
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 伤害跳字（使用Update控制）
/// </summary>
public class DamageText : MonoBehaviour
{
    public Text text;
    public float duration = 0.7f;       // 动画时长
    public float jumpForce = 1.5f;      // 跳跃高度
    public float randomX = 1f;          // 水平方向随机偏移

    private float timer;
    private Vector3 startPos;
    private Vector3 targetPos;
    //private float gravity = -10f;

    private Vector3 velocity;

    private bool isRunning = false;

    public void JumpOutNum(int num)
    {
        
        text.text = num.ToString();
        

        float rand = Random.Range(-randomX, randomX);

        startPos = transform.position;
        targetPos = startPos + new Vector3(rand, 0, 0);

        // 设定初始竖直速度，简单跳跃物理曲线
        velocity = new Vector3(rand, jumpForce, 0f);

        timer = 0f;
        isRunning = true;
        gameObject.SetActive(true);
    }

    void Update()
    {
        if (!isRunning) return;

        timer += Time.deltaTime;
        float t = timer / duration;
        if (t >= 1f)
        {
            isRunning = false;
            gameObject.SetActive(false);
            return;
        }

        // 水平线性插值
        float x = Mathf.Lerp(startPos.x, targetPos.x, t);

        // 垂直方向为抛物线轨迹（最高点在中间）
        float yOffset = 4 * jumpForce * t * (1 - t);  // 标准抛物线公式：最大值在t=0.5处
        float y = startPos.y + yOffset;

        transform.position = new Vector3(x, y, startPos.z);
    }
}
