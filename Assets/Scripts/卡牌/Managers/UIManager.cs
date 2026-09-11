
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// UI管理器
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject WinPanel;// 胜利界面
    public TMP_Text winText;   // 胜利文字
    public TurnPanel turnPanel;// 回合切换面板
    public TMP_Text resultDetailText; // 结算详情文字

    // 弹出结算面板并写上标题
    public void ShowWin(string text)
    {
        if (winText != null) winText.text = text;
        if (WinPanel != null) WinPanel.SetActive(true);
    }

    // 把结算结果整理成文字显示
    public void ShowBattleResult(BattleResult result)
    {
        if (result == null)
        {
            ShowWin("对局结束");
            return;
        }

        string title = result.outcome == BattleOutcome.PlayerWin ? "比赛胜利" : "比赛失利";
        string detail = string.Empty;
        if (result.leaguePoints > 0) detail += $"\n联赛积分 +{result.leaguePoints}";
        if (result.gold > 0) detail += $"\n金币 +{result.gold}";
        if (result.rewardCardIds != null && result.rewardCardIds.Count > 0) detail += $"\n获得新卡 {result.rewardCardIds.Count} 张";
        if (result.badgeAwarded) detail += "\n获得第一城徽章";
        if (result.outcome == BattleOutcome.PlayerLoss) detail += "\n已返回城市入口，可重新挑战";
        ShowWin(title + detail);
        if (resultDetailText != null)
        {
            resultDetailText.text = detail;
        }
    }

    // 退出游戏，编辑器里就是停止运行
    public void OnClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }

    // 重开当前城市
    public void OnClickRestart()
    {
        SceneFlowService.ReturnToCurrentCity();
    }

    // 重打刚输掉的那场
    public void OnClickRetryMatch()
    {
        SceneFlowService.RetryLastMatch();
    }

    // 返回当前城市
    public void OnClickReturnToCity()
    {
        SceneFlowService.ReturnToCurrentCity();
    }

}
