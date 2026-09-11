using TMPro;
using UnityEngine.UI;

// 界面通用工具，放各个界面都用得到的小操作
public static class UiTool
{
    // 改按钮上的文字
    public static void SetButtonText(Button button, string value)
    {
        if (button == null) return;
        TMP_Text tmpText = button.GetComponentInChildren<TMP_Text>(true);
        if (tmpText != null)
        {
            tmpText.text = value;
            return;
        }

        Text legacyText = button.GetComponentInChildren<Text>(true);
        if (legacyText != null) legacyText.text = value;
    }

    // 显示 / 隐藏按钮
    public static void SetButtonVisible(Button button, bool visible)
    {
        if (button != null) button.gameObject.SetActive(visible);
    }

    // 设置按钮可不可点
    public static void SetButtonInteractable(Button button, bool interactable)
    {
        if (button != null) button.interactable = interactable;
    }
}
