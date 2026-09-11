using UnityEngine;
using UnityEngine.SceneManagement;

// 场景跳转与跨场景传参的统一入口，跳转前把传参写进静态字段。
public static class SceneFlowService
{
    private static bool hasCardPreviewRequest; // 是否有待处理的卡牌预览请求
    private static int cardPreviewCardId; // 待预览的卡牌 ID
    private static bool hasShopRestoreState; // 是否有待恢复的商店状态
    private static int shopRestoreCardId; // 离开商店时选中的卡牌
    private static CardRarity shopRestoreRarity; // 离开商店时选中的稀有度页签
    private static Vector2 shopRestoreScrollPosition; // 离开商店时列表的滚动位置

    // 开新档并进入冒险场景
    public static void StartNewGame()
    {
        CampaignSession.Instance.BeginNewGame();
        LoadAdventureScene();
    }

    // 继续游戏：读档后先看有没有没打完的战斗，有就回战斗场景，没有才回城市
    public static void ContinueGame()
    {
        CampaignSession.Instance.LoadOrCreate();
        if (CampaignSession.Instance.HasPendingBattle)
            SceneManager.LoadScene("BattleScene");
        else
            LoadAdventureScene();
    }

    // 城市场景的统一入口，所有"回城市"的路径都走这里，避免场景名散落在各处
    public static void LoadAdventureScene()
    {
        SceneManager.LoadScene("HD_2D_Day");
    }

    /// <summary>
    /// 保存商店状态并进入卡牌3D预览场景。
    /// </summary>
    // 保存商店状态并进入卡片预览场景，跳转前把 timeScale 恢复成 1
    public static void OpenCardPreview(int cardId, CardRarity rarity, Vector2 shopScrollPosition)
    {
        cardPreviewCardId = cardId;
        hasCardPreviewRequest = true;
        shopRestoreCardId = cardId;
        shopRestoreRarity = rarity;
        shopRestoreScrollPosition = shopScrollPosition;
        hasShopRestoreState = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene("CardPreviewScene");
    }

    /// <summary>
    /// 读取并消费卡牌预览请求。
    /// </summary>
    // 读取并消费卡牌预览请求，读完就清掉标记
    public static bool TryConsumeCardPreviewRequest(out int cardId)
    {
        cardId = cardPreviewCardId;
        bool hasRequest = hasCardPreviewRequest;
        hasCardPreviewRequest = false;
        return hasRequest;
    }

    /// <summary>
    /// 读取并消费商店恢复状态。
    /// </summary>
    // 读取并消费商店恢复状态，读完就清掉标记
    public static bool TryConsumeShopRestoreState(out int cardId, out CardRarity rarity, out Vector2 shopScrollPosition)
    {
        cardId = shopRestoreCardId;
        rarity = shopRestoreRarity;
        shopScrollPosition = shopRestoreScrollPosition;
        bool hasState = hasShopRestoreState;
        hasShopRestoreState = false;
        return hasState;
    }

    /// <summary>
    /// 从卡牌预览场景返回商店所在城市。
    /// </summary>
    // 从预览返回商店所在城市，返回前先恢复 timeScale
    public static void ReturnFromCardPreview()
    {
        Time.timeScale = 1f;
        LoadAdventureScene();
    }

    // 进入卡组编辑场景
    public static void OpenDeckEditor()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("DeckEditorScene");
    }

    // 从卡组编辑场景回到城市
    public static void ReturnFromDeckEditor()
    {
        Time.timeScale = 1f;
        LoadAdventureScene();
    }

    // 开一场赛事，校验通过后写入存档并进战斗场景
    public static void StartMatch(string matchId, UnityEngine.Vector3 returnPosition)
    {
        MatchData match = CampaignCatalog.GetMatch(matchId);
        if (!CampaignSession.Instance.CanStartMatch(match)) return;
        CampaignSession.Instance.BeginMatch(match, returnPosition);
        SceneManager.LoadScene("BattleScene");
    }

    // 重打上一场失败的比赛
    public static void RetryLastMatch()
    {
        CampaignSession session = CampaignSession.Instance;
        if (session.TryRetryLastMatch(session.State.playerPosition))
        {
            SceneManager.LoadScene("BattleScene");
        }
    }

    // 继续未结束的战斗，能建出战斗上下文才跳转
    public static void ResumePendingBattle()
    {
        if (CampaignSession.Instance.CreateBattleContext() != null)
        {
            SceneManager.LoadScene("BattleScene");
        }
    }

    // 回到当前城市
    public static void ReturnToCurrentCity()
    {
        LoadAdventureScene();
    }

    // 回到主菜单
    public static void ReturnToMenu()
    {
        SceneManager.LoadScene("GameMenu");
    }
}