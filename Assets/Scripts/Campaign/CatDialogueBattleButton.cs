using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 猫姬对话框的独立挑战按钮绑定。
/// 只负责把猫姬的“挑战Button”（也兼容旧的 ChallengeButton）连接到第一城练习赛。
/// </summary>
public sealed class CatDialogueBattleButton : MonoBehaviour
{
    private Button challengeButton;
    private bool bound;

    private void Update()
    {
        if (!bound) Bind();
    }

    public void Bind()
    {
        if (bound) return;
        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        foreach (Canvas canvas in canvases)
        {
            if (canvas == null || !canvas.gameObject.name.Contains("猫姬")) continue;
            Button[] buttons = canvas.GetComponentsInChildren<Button>(true);

            // 优先匹配当前使用的中文名称；同时兼容之前的英文名称。
            foreach (Button button in buttons)
            {
                if (button == null || !IsChallengeButtonName(button.gameObject.name)) continue;
                challengeButton = button;
                break;
            }
            if (challengeButton != null) break;
        }

        if (challengeButton == null)
        {
            Debug.LogWarning("没有找到猫姬 Canvas 下的“挑战Button”（或旧名称 ChallengeButton）。");
            return;
        }

        Graphic[] graphics = challengeButton.GetComponentsInChildren<Graphic>(true);
        foreach (Graphic graphic in graphics)
            if (graphic != null && graphic.gameObject != challengeButton.gameObject)
                graphic.raycastTarget = false;

        challengeButton.onClick.RemoveListener(StartPracticeMatch);
        challengeButton.onClick.AddListener(StartPracticeMatch);
        bound = true;
        Debug.Log("猫姬 挑战Button 已绑定 first_light_practice。");
    }

    private static bool IsChallengeButtonName(string objectName)
    {
        if (string.IsNullOrEmpty(objectName)) return false;

        return objectName.IndexOf("挑战Button", System.StringComparison.OrdinalIgnoreCase) >= 0
            || objectName.IndexOf("ChallengeButton", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void StartPracticeMatch()
    {
        CampaignSession session = CampaignSession.Instance;
        MatchData match = CampaignCatalog.GetMatch("first_light_practice");
        AmiyaCharacter amiya = FindObjectOfType<AmiyaCharacter>(true);
        Vector3 returnPosition = amiya != null ? amiya.transform.position : Vector3.zero;

        if (session == null || match == null)
        {
            Debug.LogError("无法启动练习赛：CampaignSession 或 first_light_practice 不存在。");
            return;
        }

        // 已有战斗时，“挑战”按钮承担继续对局的入口，不能再按新赛事的解锁规则拦截。
        if (session.HasPendingBattle)
        {
            Debug.Log("猫姬挑战按钮继续未结束的战斗：" + session.State.pendingBattle.matchId);
            Time.timeScale = 1f;
            SceneFlowService.ResumePendingBattle();
            return;
        }

        if (!session.HasLegalDeck)
        {
            Debug.LogWarning("无法启动练习赛：当前卡组不合法，请先保存合法的 20 张卡组。");
            return;
        }
        if (!session.CanStartMatch(match))
        {
            Debug.LogWarning("无法启动练习赛：" + GetStartBlockReason(session, match));
            return;
        }

        Debug.Log("猫姬挑战按钮进入 BattleScene：first_light_practice");
        Time.timeScale = 1f;
        session.SetPlayerPosition(returnPosition);
        SceneFlowService.StartMatch(match.matchId, returnPosition);
    }

    private static string GetStartBlockReason(CampaignSession session, MatchData match)
    {
        if (session == null || session.State == null) return "CampaignSession 尚未初始化";
        if (!session.IsCityUnlocked(match.cityId)) return "城市尚未解锁：" + match.cityId;
        if (session.HasPendingBattle) return "已有一场未结束的战斗，请先继续或结算它";
        if (!session.HasLegalDeck) return "当前没有合法卡组";
        if (!string.IsNullOrEmpty(match.prerequisiteMatchId) && !session.IsMatchComplete(match.prerequisiteMatchId))
            return "需要先完成赛事：" + match.prerequisiteMatchId;
        return "赛事数据或存档状态不一致";
    }
}
