using TMPro;
using UnityEngine;

// 显示城市积分、金币和附近角色的交谈提示。
public sealed class CityHudView : MonoBehaviour
{
    [SerializeField] private TMP_Text pointsText; // 当前赛事积分
    [SerializeField] private TMP_Text goldText; // 当前金币
    [SerializeField] private TMP_Text statusText; // 城市临时消息
    [SerializeField] private GameObject interactionPrompt; // 按 E 交谈提示
    [SerializeField] private TMP_Text interactionText; // 当前可交谈角色的名字

    // 从现有存档数据刷新数值和临时消息。
    public void Refresh(int points, int gold, string status)
    {
        pointsText.text = points.ToString();
        goldText.text = gold.ToString();
        statusText.text = status;
    }

    // 只在附近有可交谈角色且允许交互时显示提示。
    public void ShowInteraction(WorldInteractionActor actor)
    {
        bool visible = actor != null;
        if (interactionPrompt.activeSelf != visible) interactionPrompt.SetActive(visible);
        if (visible) interactionText.text = "与" + actor.dialogue.displayName + "交谈";
    }
}
