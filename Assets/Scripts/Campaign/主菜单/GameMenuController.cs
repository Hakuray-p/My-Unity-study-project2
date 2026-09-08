using UnityEngine;

public class GameMenuController : MonoBehaviour
{
    private void Start()
    {
        CampaignSession.Instance.ToString();
    }

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
