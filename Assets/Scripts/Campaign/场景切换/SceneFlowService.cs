using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneFlowService
{
    private static bool hasCardPreviewRequest;
    private static int cardPreviewCardId;
    private static bool hasShopRestoreState;
    private static int shopRestoreCardId;
    private static CardRarity shopRestoreRarity;
    private static Vector2 shopRestoreScrollPosition;

    public static void StartNewGame()
    {
        CampaignSession.Instance.BeginNewGame();
        LoadAdventureScene();
    }

    public static void ContinueGame()
    {
        CampaignSession.Instance.LoadOrCreate();
        if (CampaignSession.Instance.HasPendingBattle)
            SceneManager.LoadScene("BattleScene");
        else
            LoadAdventureScene();
    }

    public static void LoadAdventureScene()
    {
        SceneManager.LoadScene("HD_2D_Day");
    }

    /// <summary>
    /// 保存商店状态并进入卡牌3D预览场景。
    /// </summary>
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
    public static void ReturnFromCardPreview()
    {
        Time.timeScale = 1f;
        LoadAdventureScene();
    }

    public static void StartMatch(string matchId, UnityEngine.Vector3 returnPosition)
    {
        MatchData match = CampaignCatalog.GetMatch(matchId);
        if (!CampaignSession.Instance.CanStartMatch(match)) return;
        CampaignSession.Instance.BeginMatch(match, returnPosition);
        SceneManager.LoadScene("BattleScene");
    }

    public static void RetryLastMatch()
    {
        CampaignSession session = CampaignSession.Instance;
        if (session.TryRetryLastMatch(session.State.playerPosition))
        {
            SceneManager.LoadScene("BattleScene");
        }
    }

    public static void ResumePendingBattle()
    {
        if (CampaignSession.Instance.CreateBattleContext() != null)
        {
            SceneManager.LoadScene("BattleScene");
        }
    }

    public static void ReturnToCurrentCity()
    {
        LoadAdventureScene();
    }

    public static void ReturnToMenu()
    {
        SceneManager.LoadScene("GameMenu");
    }
}
