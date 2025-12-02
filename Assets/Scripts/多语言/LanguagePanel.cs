using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using XLua;

public class LanguagePanel : MonoBehaviour{

    public string Info;
    private void OnEnable(){
        if (Info==null) {
            Info = name;
        }
        
        // 检查 LanguageSYSTEM 是否已初始化
        if (LanguageSYSTEM.languageENV == null || LanguageSYSTEM.languageENV.Global == null) {
            Debug.LogWarning("LanguageSYSTEM.languageENV 尚未初始化，延迟执行");
            StartCoroutine(DelayedEnable());
            return;
        }
        
        Component component;
        TryGetComponent(typeof(Image), out component);
        if (component!=null) {
            Image imageComponent = GetComponent<Image>();
            if (imageComponent != null) {
                string path = LanguageSYSTEM.languageENV.Global.Get<string>(Info);
                if (path != null) {
                    Debug.Log(path);
                    imageComponent.sprite = GetSpriteFromAssets(path);
                }
            }
            return;
        }

        TryGetComponent(typeof(Text), out component);
        if (component!=null) {
            Text textcomponent = GetComponent<Text>();
            if (textcomponent != null) {
                string text = LanguageSYSTEM.languageENV.Global.Get<string>(Info);
                if (text != null) {
                    textcomponent.text = text;
                }
                
                // 检查 Center.instance 是否已初始化
                if (Center.instance != null) {
                    Font font = Center.instance.GetFont();
                    if (font != null) {
                        textcomponent.font = font;
                    }
                }
            }
            return;
        }

    }
    
    private IEnumerator DelayedEnable() {
        // 等待 LanguageSYSTEM 初始化完成
        while (LanguageSYSTEM.languageENV == null || LanguageSYSTEM.languageENV.Global == null) {
            yield return null;
        }
        
        // 重新执行初始化逻辑
        Component component;
        TryGetComponent(typeof(Image), out component);
        if (component!=null) {
            Image imageComponent = GetComponent<Image>();
            if (imageComponent != null) {
                string path = LanguageSYSTEM.languageENV.Global.Get<string>(Info);
                if (path != null) {
                    Debug.Log(path);
                    imageComponent.sprite = GetSpriteFromAssets(path);
                }
            }
            yield break;
        }

        TryGetComponent(typeof(Text), out component);
        if (component!=null) {
            Text textcomponent = GetComponent<Text>();
            if (textcomponent != null) {
                string text = LanguageSYSTEM.languageENV.Global.Get<string>(Info);
                if (text != null) {
                    textcomponent.text = text;
                }
                
                if (Center.instance != null) {
                    Font font = Center.instance.GetFont();
                    if (font != null) {
                        textcomponent.font = font;
                    }
                }
            }
        }
    }
    
    public static Sprite GetSpriteFromAssets(string ImgPath){
        Sprite spriteToUse;
        string spritePath = Application.streamingAssetsPath +"/"+ LanguageSYSTEM.ImagePath+"/" +ImgPath+LanguageSYSTEM.Language+".png";

        if (File.Exists(spritePath))
        {
            byte[] spriteData = File.ReadAllBytes(spritePath);

            // 创建Sprite
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(spriteData); // 加载字节数据到Texture2D

            spriteToUse = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }
        else
        {
            Debug.LogWarning($"无法找到Sprite：{spritePath}");
            spriteToUse = Resources.Load<Sprite>("DefaultSprite"); // 替换为默认的Sprite
        }

        return spriteToUse;
    }
    
}
