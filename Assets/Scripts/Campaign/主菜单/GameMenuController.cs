using UnityEngine;

// 主菜单界面，用 OnGUI 画出开始新游戏与继续游戏两个按钮。
public class GameMenuController : MonoBehaviour
{
    // 提前创建存档会话，保证进菜单时单例已就绪
    private void Start()
    {
        CampaignSession.Instance.ToString();
    }

    // 用 IMGUI 画出主菜单
    private void OnGUI()
    {
        const float width = 420f;
        const float left = 40f;
        GUI.Box(new Rect(left, 35f, width, 220f), "RX-未知裂痕");
        GUI.Label(new Rect(left + 24f, 72f, width - 48f, 48f), "探索城市、与角色交谈、调整卡组，并参加卡牌赛事。");

        if (GUI.Button(new Rect(left + 24f, 130f, width - 48f, 38f), "Start new journey"))
        {
            SceneFlowService.StartNewGame();
        }

        GUI.enabled = CampaignSession.Instance.HasSave();
        if (GUI.Button(new Rect(left + 24f, 178f, width - 48f, 38f), "Continue journey"))
        {
            SceneFlowService.ContinueGame();
        }
        GUI.enabled = true;
    }
}
