using UnityEngine;

// 保存一段对话的说话者和按顺序显示的句子。
[System.Serializable]
public class Dialogue
{
    public string name; // 对话中的说话者名字
    [TextArea(3, 10)]
    public string[] sentences; // 按顺序显示的对话句子
}
