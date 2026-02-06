using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BuffCell : MonoBehaviour
{
    public Image icon;
    public Text countText;

    public void SetData(BuffState buffState)
    {
        // 加载图标
        //icon.sprite = buffState.buffData.icon;
        if (buffState.stacks >= 1)
        {
            countText.text = buffState.stacks.ToString();
            countText.gameObject.SetActive(true);
        }
        else
        {
            countText.gameObject.SetActive(false);
        }
    }
}
