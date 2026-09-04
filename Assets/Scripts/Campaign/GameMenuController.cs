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
        GUI.Box(new Rect(left, 35f, width, 220f), "CARD LEAGUE ADVENTURE");
        GUI.Label(new Rect(left + 24f, 72f, width - 48f, 48f), "Enter a city, open the route map, and clear each card challenge.");

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
