
using TMPro;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
    public SpriteRenderer opponentPortrait; // 敌方头像框内的立绘
    private GameObject resultContent; // 结算奖励内容
    private GameObject rewardCardZoom; // 放大的奖励卡牌

    // 按当前赛事显示对手立绘，保留已有头像框与遮罩。
    public void ShowOpponentPortrait(MatchData match)
    {
        opponentPortrait.sprite = match.opponentPortrait;
        opponentPortrait.transform.localScale = Vector3.one * match.opponentPortraitScale;
        var position = new Vector3(match.opponentPortraitOffset.x, match.opponentPortraitOffset.y,
            opponentPortrait.transform.localPosition.z); // 立绘在头像框内的位置
        opponentPortrait.transform.localPosition = position;
    }

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
        ShowWin(title);
        ShowResultContent(result);
    }

    // 退出战斗结算并返回当前城市。
    public void OnClickQuit()
    {
        OnClickReturnToCity();
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

    // 创建结算奖励区域并显示积分、金币和新卡。
    private void ShowResultContent(BattleResult result)
    {
        if (resultContent != null) Destroy(resultContent);
        if (rewardCardZoom != null) Destroy(rewardCardZoom);
        winText.fontSize = 56;
        RectTransform titleRect = winText.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0f, 250f);
        titleRect.sizeDelta = new Vector2(900f, 100f);
        resultContent = CreateUiObject("BattleResultContent", WinPanel.transform);
        RectTransform contentRect = resultContent.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.anchoredPosition = new Vector2(0f, 20f);
        contentRect.sizeDelta = new Vector2(1000f, 430f);
        CreateRewardRow(resultContent.transform, "PointsReward", Resources.Load<Sprite>("UI/积分UI"), result.leaguePoints, -230f);
        CreateRewardRow(resultContent.transform, "GoldReward", Resources.Load<Sprite>("UI/金币UI"), result.gold, -120f);
        CreateRewardCards(resultContent.transform, result.rewardCardIds);
        if (result.outcome == BattleOutcome.PlayerLoss) CreateResultText(resultContent.transform, "已返回城市入口，可重新挑战", new Vector2(0f, -180f), 28);
    }

    // 创建带图标的积分或金币奖励。
    private void CreateRewardRow(Transform parent, string objectName, Sprite icon, int amount, float y)
    {
        if (amount <= 0) return;
        GameObject row = CreateUiObject(objectName, parent);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 0.5f);
        rowRect.anchorMax = new Vector2(0.5f, 0.5f);
        rowRect.anchoredPosition = new Vector2(0f, y);
        rowRect.sizeDelta = new Vector2(420f, 72f);
        GameObject iconObject = CreateUiObject("RewardIcon", row.transform);
        Image iconImage = iconObject.AddComponent<Image>();
        iconImage.sprite = icon;
        iconImage.preserveAspect = true;
        RectTransform iconRect = iconImage.rectTransform;
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(28f, 0f);
        iconRect.sizeDelta = new Vector2(58f, 58f);
        CreateResultText(row.transform, "+" + amount, new Vector2(160f, 0f), 34);
    }

    // 创建新卡展示区域并显示卡名。
    private void CreateRewardCards(Transform parent, List<int> cardIds)
    {
        if (cardIds == null || cardIds.Count == 0) return;
        GameObject cards = CreateUiObject("RewardCards", parent);
        RectTransform cardsRect = cards.GetComponent<RectTransform>();
        cardsRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardsRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardsRect.anchoredPosition = new Vector2(0f, 90f);
        cardsRect.sizeDelta = new Vector2(900f, 260f);
        float start = (cardIds.Count - 1) * -120f;
        for (int i = 0; i < cardIds.Count; i++)
        {
            CardData card = CampaignCatalog.GetCardData(cardIds[i]);
            CreateRewardCard(cards.transform, card, new Vector2(start + i * 240f, 0f));
        }
    }

    // 创建可以点击放大的奖励卡牌。
    private void CreateRewardCard(Transform parent, CardData card, Vector2 position)
    {
        GameObject cardObject = CreateUiObject("RewardCard_" + card.index, parent);
        RectTransform cardRect = cardObject.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = position;
        cardRect.sizeDelta = new Vector2(190f, 250f);
        Image background = cardObject.AddComponent<Image>();
        background.color = new Color(0.08f, 0.08f, 0.1f, 0.9f);
        Button button = cardObject.AddComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(() => ShowRewardCardZoom(card));
        GameObject imageObject = CreateUiObject("CardImage", cardObject.transform);
        Image image = imageObject.AddComponent<Image>();
        image.sprite = card.image;
        image.preserveAspect = true;
        RectTransform imageRect = image.rectTransform;
        imageRect.anchorMin = new Vector2(0.5f, 1f);
        imageRect.anchorMax = new Vector2(0.5f, 1f);
        imageRect.anchoredPosition = new Vector2(0f, -92f);
        imageRect.sizeDelta = new Vector2(150f, 170f);
        CreateResultText(cardObject.transform, card.name, new Vector2(0f, -205f), 22);
    }

    // 显示放大的卡图、卡名和效果说明。
    private void ShowRewardCardZoom(CardData card)
    {
        if (rewardCardZoom != null) Destroy(rewardCardZoom);
        rewardCardZoom = CreateUiObject("RewardCardZoom", WinPanel.transform);
        RectTransform zoomRect = rewardCardZoom.GetComponent<RectTransform>();
        zoomRect.anchorMin = Vector2.zero;
        zoomRect.anchorMax = Vector2.one;
        zoomRect.sizeDelta = Vector2.zero;
        Image overlay = rewardCardZoom.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.75f);
        GameObject cardObject = CreateUiObject("CardZoomButton", rewardCardZoom.transform);
        RectTransform cardRect = cardObject.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = new Vector2(-260f, 0f);
        cardRect.sizeDelta = new Vector2(320f, 460f);
        Image image = cardObject.AddComponent<Image>();
        image.sprite = card.image;
        image.preserveAspect = true;
        Button closeButton = cardObject.AddComponent<Button>();
        closeButton.targetGraphic = image;
        closeButton.onClick.AddListener(CloseRewardCardZoom);
        CreateResultText(rewardCardZoom.transform, card.name, new Vector2(260f, 145f), 38);
        CreateResultText(rewardCardZoom.transform, Regex.Unescape(card.effectDescription), new Vector2(260f, -20f), 24, new Vector2(420f, 220f));
        CreateResultText(rewardCardZoom.transform, "点击卡图关闭", new Vector2(260f, -190f), 20);
    }

    // 关闭放大的奖励卡牌。
    private void CloseRewardCardZoom()
    {
        Destroy(rewardCardZoom);
        rewardCardZoom = null;
    }

    // 创建结算界面的文字组件。
    private TMP_Text CreateResultText(Transform parent, string text, Vector2 position, float fontSize, Vector2 size = default)
    {
        GameObject textObject = CreateUiObject("ResultText", parent);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = position;
        textRect.sizeDelta = size == default ? new Vector2(500f, 70f) : size;
        TMP_Text textComponent = textObject.AddComponent<TextMeshProUGUI>();
        textComponent.font = winText.font;
        textComponent.fontSize = fontSize;
        textComponent.color = Color.white;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.enableWordWrapping = true;
        textComponent.text = text;
        return textComponent;
    }

    // 创建结算界面的空 UI 对象。
    private GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject uiObject = new GameObject(objectName, typeof(RectTransform));
        uiObject.transform.SetParent(parent, false);
        return uiObject;
    }

}
