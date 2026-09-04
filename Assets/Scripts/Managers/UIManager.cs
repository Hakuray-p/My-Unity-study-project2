
    using TMPro;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    /// <summary>
    /// UI管理器
    /// </summary>
    public class UIManager: MonoBehaviour
    {
        [Header("UI")] 
        public GameObject WinPanel;// 胜利界面
        public TMP_Text winText;   // 胜利文字
        public TurnPanel turnPanel;// 回合切换面板
        public TMP_Text resultDetailText;

        public void ShowWin(string text)
        {
            if (winText != null) winText.text = text;
            if (WinPanel != null) WinPanel.SetActive(true);
        }

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
            if (result.outcome == BattleOutcome.PlayerLoss) detail += "\n本关已回到失败前节点，可重新挑战";
            ShowWin(title + detail);
            if (resultDetailText != null)
            {
                resultDetailText.text = detail;
            }
        }

        public void OnClickQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void OnClickRestart()
        {
            if (CampaignSession.Instance.LastBattleOutcome == BattleOutcome.PlayerLoss)
                SceneFlowService.RestartCurrentStage();
            else
                SceneFlowService.ReturnToCurrentCity();
        }

        public void OnClickRetryMatch()
        {
            SceneFlowService.RetryLastMatch();
        }

        public void OnClickReturnToCity()
        {
            SceneFlowService.ReturnToCurrentCity();
        }

        public void OnClickWorldMap()
        {
            SceneFlowService.ReturnToCurrentCity();
        }
    }
