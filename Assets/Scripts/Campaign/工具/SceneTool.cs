using UnityEngine;

// 场景查找通用工具，按名字片段找场景里的组件
public static class SceneTool
{
    // 按名字片段找场景里的组件，隐藏的也算
    public static T Find<T>(string namePart) where T : Component
    {
        foreach (T item in Object.FindObjectsOfType<T>(true))
            if (item.gameObject.name.Contains(namePart)) return item;
        return null;
    }
}
