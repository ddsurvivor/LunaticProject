
    using System;
    using UnityEngine;

    public class SameSprite: MonoBehaviour
    {
        public SpriteRenderer selfSpriteRenderer;
        public SpriteRenderer targetSpriteRenderer;

        public void Awake()
        {
            targetSpriteRenderer = transform.parent.GetComponent<SpriteRenderer>();
        }

        public void Update()
        {
            if (selfSpriteRenderer != null && targetSpriteRenderer != null)
            {
                selfSpriteRenderer.sprite = targetSpriteRenderer.sprite;
            }
        }
    }
