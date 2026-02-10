using System.IO;
using UnityEngine;
using Newtonsoft.Json;
// com.unity.nuget.newtonsoft-json
public static class JsonTool
{
    public static void SaveJson<T>(T data, string filepath)
    {
        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        
        using (StreamWriter sw = new StreamWriter(filepath))
        {
            sw.WriteLine(json);
            sw.Close();
            sw.Dispose();
        }
    }

    public static T LoadJson<T>(string filepath)
    {
        string json = "";

        if (!File.Exists(filepath))
        {
            Debug.Log($"文件缺失：{filepath}");
            return default(T);
        }

        using (StreamReader sr = new StreamReader(filepath))
        {
            json = sr.ReadToEnd();
            sr.Close();
        }

        return JsonConvert.DeserializeObject<T>(json);
    }

    public static T LoadResource<T>(string filepath)
    {
        string json = "";
        TextAsset text = Resources.Load<TextAsset>(filepath);
        json = text.text;
        
        return JsonConvert.DeserializeObject<T>(json);
    }
}