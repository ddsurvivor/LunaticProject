
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    public class SlideImage: MonoBehaviour
    {
        public List<Sprite> sprites = new List<Sprite>();
        public Image image;
        private int currentIndex = 0;
        // 左右翻页函数，点击后按顺序切换图片
        public void Slide(bool isNext)
        {
            if (isNext)
            {
                currentIndex = (currentIndex + 1) % sprites.Count;
            }
            else
            {
                currentIndex = (currentIndex - 1 + sprites.Count) % sprites.Count;
            }
            image.sprite = sprites[currentIndex];
        }
    }
