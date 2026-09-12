using UnityEngine;
using UnityEngine.UI;


public class GameMenuGM : MonoBehaviour
{

    private void Start()
    {
        Button startButton = SceneTool.Find<Button>("开始游戏");
        Button continueButton = SceneTool.Find<Button>("继续游戏");

        startButton.onClick.AddListener(SceneFlowService.StartNewGame);
        continueButton.onClick.AddListener(SceneFlowService.ContinueGame);
        UiTool.SetButtonInteractable(continueButton, CampaignSession.Instance.HasSave());
    }
}
