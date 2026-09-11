using System.IO;
using UnityEngine;
using Newtonsoft.Json;

// JSON 读写工具，存档和卡组文件都走这里
public static class JsonTool
{
    // 把对象序列化成 JSON 写到磁盘
    public static void SaveJson<T>(T data, string filepath)
    {
        // 获取目录路径
        string directory = Path.GetDirectoryName(filepath);
        // 如果目录不存在则创建
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json = JsonConvert.SerializeObject(data, Formatting.Indented);

        using (StreamWriter sw = new StreamWriter(filepath))
        {
            sw.WriteLine(json);
            sw.Close();
            sw.Dispose();
        }
    }

    // 从磁盘读 JSON，文件缺失时返回默认值
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

    // 从 Resources 里读 TextAsset 并反序列化
    public static T LoadResource<T>(string filepath)
    {
        string json = "";
        TextAsset text = Resources.Load<TextAsset>(filepath);
        json = text.text;

        return JsonConvert.DeserializeObject<T>(json);
    }
}
