using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 商店面板，负责商店控件的查找、商品列表、卡牌预览和购买交互
public sealed class ShopPanel
{
    private readonly CampaignSession session; // 当前存档会话
    private readonly Action<string> showStatus; // 显示状态提示的回调
    private readonly Action closePanels; // 关闭面板的回调
    private GameObject canvasObject; // 商店画布
    private ScrollRect scrollRect; // 商店列表的滚动区
    private RectTransform content; // 商店列表的内容节点
    private Button rarityRButton; // R 页签按钮
    private Button raritySRButton; // SR 页签按钮
    private Button rarityURButton; // UR 页签按钮
    private Button buyButton; // 购买按钮
    private Button previewButton; // 卡牌预览按钮
    private Button exitButton; // 退出商店按钮
    private TMP_Text goldText; // 金币显示
    private TMP_FontAsset fontAsset; // 商店统一字体
    private TMP_Text nameText; // 卡名显示
    private TMP_Text effectText; // 效果描述显示
    private TMP_Text previewText; // 预览提示文字
    private RawImage previewRawImage; // 3D 预览画面
    private Camera previewCamera; // 商店里的 3D 预览相机
    private GameObject previewModel; // 商店里的 3D 卡牌模型
    private CardDisplay previewCardDisplay; // 预览模型的显示组件
    private CardController previewCardController; // 预览模型的卡牌组件
    private CardShopEntry selectedEntry; // 当前选中的商品
    private CardRarity selectedRarity = CardRarity.Common; // 当前选中的稀有度页签
    private bool restoringState; // 是否正在还原进预览前的商店状态
    private Vector2 restoreScrollPosition; // 进预览前列表的滚动位置
    private readonly List<GameObject> items = new List<GameObject>(); // 生成出来的商品行
    private bool uiInitialized; // 商店 UI 是否已经初始化

    public bool IsOpen { get; private set; } // 商店是否打开

    // 存下会话和两个回调，面板由场景 GM 创建
    public ShopPanel(CampaignSession campaignSession, Action<string> showStatusAction, Action closePanelsAction)
    {
        session = campaignSession;
        showStatus = showStatusAction;
        closePanels = closePanelsAction;
    }

    // 打开商店，把画布显示出来并重建当前页签的列表
    public void Open()
    {
        IsOpen = true;
        Initialize();
        if (canvasObject != null) canvasObject.SetActive(true);
        SelectRarity(selectedRarity);
    }

    // 关掉商店并收起预览模型
    public void Close()
    {
        IsOpen = false;
        if (canvasObject != null) canvasObject.SetActive(false);
        if (previewModel != null) previewModel.SetActive(false);
    }

    // 取出卡牌预览返回时留下的页签和滚动位置，返回 true 表示这次要还原
    public bool TryRestore()
    {
        if (!SceneFlowService.TryConsumeShopRestoreState(out int cardId, out CardRarity rarity, out Vector2 scrollPosition)) return false;
        restoringState = true;
        restoreScrollPosition = scrollPosition;
        selectedRarity = rarity;
        selectedEntry = new CardShopEntry { cardId = cardId, rarity = rarity };
        return true;
    }

    // 第一次打开时按对象名找到画布上的控件并接好事件
    public void Initialize()
    {
        if (uiInitialized)
        {
            UpdateGold();
            return;
        }

        canvasObject = FindSceneObject("商店Canvas") ?? FindSceneObject("商点Canvas");
        if (canvasObject == null) return;

        scrollRect = canvasObject.GetComponentInChildren<ScrollRect>(true);
        if (scrollRect != null) content = scrollRect.content;
        rarityRButton = FindButtonByName(canvasObject, "R卡牌Button");
        raritySRButton = FindButtonByName(canvasObject, "SR卡牌Button");
        rarityURButton = FindButtonByName(canvasObject, "UR卡牌Button");
        buyButton = FindButtonByName(canvasObject, "商店购买Buttom");
        exitButton = FindButtonByName(canvasObject, "退出商店购买Buttom");
        goldText = FindTextByName(canvasObject, "金币数量Text (TMP)");
        nameText = FindTextByName(canvasObject, "此处为卡片物品的名字预览");
        effectText = FindTextByName(canvasObject, "此处为卡片效果描述Text");
        previewText = FindTextContainsName(canvasObject, "此处为预览整张3D卡详");
        previewButton = FindButtonByName(canvasObject, "卡牌预览Buttom");
        previewRawImage = FindRawImageByName(canvasObject, "CardPreviewRawImage");
        previewCamera = FindSceneObject("ShopCardPreviewCamera")?.GetComponent<Camera>();
        previewModel = FindSceneObject("CarShopCardPreviewModel");
        previewCardDisplay = previewModel == null ? null : previewModel.GetComponentInChildren<CardDisplay>(true);
        previewCardController = previewModel == null ? null : previewModel.GetComponentInChildren<CardController>(true);
        ConfigureEffectText();
        ConfigureCardPreview();
        fontAsset = canvasObject.GetComponentInChildren<TMP_Text>(true)?.font;
        HidePreviewTexts();
        ApplyFont();

        if (scrollRect == null || content == null || rarityRButton == null ||
            raritySRButton == null || rarityURButton == null || buyButton == null)
            return;

        var tabRaycastPadding = new Vector4(-16f, -22f, -16f, -26f); // 标签按钮热区向外扩一圈，覆盖美术标签的整个可见范围
        rarityRButton.image.raycastPadding = tabRaycastPadding;
        raritySRButton.image.raycastPadding = tabRaycastPadding;
        rarityURButton.image.raycastPadding = tabRaycastPadding;

        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);

        VerticalLayoutGroup layoutGroup = content.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup == null) layoutGroup = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.spacing = 8f;

        ContentSizeFitter sizeFitter = content.GetComponent<ContentSizeFitter>();
        if (sizeFitter == null) sizeFitter = content.gameObject.AddComponent<ContentSizeFitter>();
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        rarityRButton.onClick.AddListener(() => SelectRarity(CardRarity.Common));
        raritySRButton.onClick.AddListener(() => SelectRarity(CardRarity.Rare));
        rarityURButton.onClick.AddListener(() => SelectRarity(CardRarity.Limited));
        buyButton.onClick.AddListener(BuySelectedCard);
        if (previewButton != null) previewButton.onClick.AddListener(OpenSelectedCardPreview);
        if (exitButton != null) exitButton.onClick.AddListener(() => closePanels());
        uiInitialized = true;
        canvasObject.SetActive(false);
        UpdateGold();
    }

    // 切商店页签并重建列表
    private void SelectRarity(CardRarity rarity)
    {
        selectedRarity = rarity;
        RebuildItems();
    }

    // 按当前页签重建商品列表，并尽量选中原来那张卡
    private void RebuildItems()
    {
        if (!uiInitialized) return;

        int selectedCardId = selectedEntry == null ? 0 : selectedEntry.cardId;
        foreach (GameObject item in items)
        {
            if (item != null) UnityEngine.Object.Destroy(item);
        }
        items.Clear();

        IReadOnlyList<CardShopEntry> entries = CampaignCatalog.GetFirstCityShop();
        CardShopEntry firstEntry = null;
        CardShopEntry restoredEntry = null;
        foreach (CardShopEntry entry in entries)
        {
            if (entry.rarity != selectedRarity) continue;
            if (firstEntry == null) firstEntry = entry;
            if (entry.cardId == selectedCardId) restoredEntry = entry;
            CreateItem(entry);
        }

        selectedEntry = restoredEntry ?? firstEntry;
        if (selectedEntry != null) UpdatePreview();
        else ClearPreview();
        if (restoringState && scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.normalizedPosition = restoreScrollPosition;
            restoringState = false;
        }
        else if (content != null)
        {
            content.anchoredPosition = Vector2.zero;
        }
    }

    // 在商店列表里创建一条商品
    private void CreateItem(CardShopEntry entry)
    {
        var itemObject = new GameObject("CardShopItem_" + entry.cardId,
            typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        itemObject.transform.SetParent(content, false);
        items.Add(itemObject);

        RectTransform itemRect = itemObject.GetComponent<RectTransform>();
        itemRect.sizeDelta = new Vector2(0f, 58f);
        Image background = itemObject.GetComponent<Image>();
        background.color = new Color(0.2f, 0.1f, 0.1f, 0.78f);
        Button button = itemObject.GetComponent<Button>();
        button.targetGraphic = background;
        LayoutElement layoutElement = itemObject.GetComponent<LayoutElement>();
        layoutElement.preferredHeight = 58f;

        var textObject = new GameObject("CardIdText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(itemObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(18f, 0f);
        textRect.offsetMax = new Vector2(-18f, 0f);
        TMP_Text text = textObject.GetComponent<TMP_Text>();
        if (fontAsset != null) text.font = fontAsset;
        CardData cardData = CampaignCatalog.GetCardData(entry.cardId);
        text.text = entry.cardId + "  " + (cardData == null ? "未命名卡牌" : cardData.name) +
            "  " + entry.price + "金币";
        text.fontSize = 24f;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.raycastTarget = false;

        CardShopEntry clickedEntry = entry;
        button.onClick.AddListener(() => SelectCard(clickedEntry));
    }

    // 选中商品并刷新右侧预览
    private void SelectCard(CardShopEntry entry)
    {
        selectedEntry = entry;
        UpdatePreview();
    }

    // 刷新商店右侧的卡牌预览和购买按钮
    private void UpdatePreview()
    {
        UpdateGold();
        if (selectedEntry == null)
        {
            ClearPreview();
            return;
        }

        CardData cardData = CampaignCatalog.GetCardData(selectedEntry.cardId);
        UpdateCardPreview(cardData);
        if (previewButton != null) previewButton.interactable = cardData != null;

        string reason;
        bool canBuy = session.CanBuyCard(selectedEntry, out reason);
        buyButton.interactable = canBuy;
        UiTool.SetButtonText(buyButton, canBuy ? "购买" : reason);
    }

    // 买下选中的卡，失败就把原因显示出来
    private void BuySelectedCard()
    {
        if (selectedEntry == null) return;
        string reason;
        if (!session.BuyCard(selectedEntry, out reason))
        {
            showStatus(reason);
            UpdatePreview();
            return;
        }

        showStatus("已购买卡牌《" + GetCardName(selectedEntry.cardId) + "》");
        UpdatePreview();
    }

    // 刷新商店里的金币显示
    private void UpdateGold()
    {
        if (goldText != null && session != null && session.State != null)
            goldText.text = "金币：" + session.State.currency;
    }

    // 清空商店预览并禁用购买按钮
    private void ClearPreview()
    {
        UpdateCardPreview(null);
        if (buyButton != null)
        {
            buyButton.interactable = false;
            UiTool.SetButtonText(buyButton, "购买");
        }
    }

    // 隐藏商店预览里的文字
    private void HidePreviewTexts()
    {
        if (nameText != null) nameText.gameObject.SetActive(false);
        if (effectText != null) effectText.gameObject.SetActive(false);
        if (previewText != null) previewText.gameObject.SetActive(false);
    }

    // 关掉预览模型的碰撞和遮罩，让 3D 卡面干净显示
    private void ConfigureCardPreview()
    {
        if (previewCamera != null)
        {
            previewCamera.enabled = true;
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = Color.clear;
            AudioListener listener = previewCamera.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = false;
            if (previewRawImage != null) previewRawImage.texture = previewCamera.targetTexture;
        }

        if (previewModel == null) return;
        foreach (Collider collider in previewModel.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;
        foreach (SpriteMask mask in previewModel.GetComponentsInChildren<SpriteMask>(true))
            mask.enabled = false;
        GameObject effectPanel = FindChildObjectByName(previewModel, "EffectIcon");
        if (effectPanel != null) effectPanel.SetActive(false);
        if (previewCardDisplay != null && previewCardDisplay.cardImage != null)
            previewCardDisplay.cardImage.maskInteraction = SpriteMaskInteraction.None;
        if (previewCardDisplay != null && previewCardDisplay.effectText != null)
            previewCardDisplay.effectText.gameObject.SetActive(false);
        if (previewCardController != null) previewCardController.enabled = false;
        previewModel.SetActive(false);
    }

    // 设置商店效果文字的换行和溢出方式
    private void ConfigureEffectText()
    {
        if (effectText == null) return;
        effectText.enableWordWrapping = true;
        effectText.overflowMode = TextOverflowModes.Masking;
    }

    // 把卡面数据和 3D 模型同步到商店预览
    private void UpdateCardPreview(CardData cardData)
    {
        bool hasCard = cardData != null && previewCardDisplay != null;
        if (previewRawImage != null) previewRawImage.gameObject.SetActive(hasCard);
        if (previewModel != null) previewModel.SetActive(hasCard);
        if (!hasCard)
        {
            if (effectText != null)
            {
                effectText.text = string.Empty;
                effectText.gameObject.SetActive(false);
            }
            return;
        }

        if (effectText != null)
        {
            effectText.text = Regex.Unescape(cardData.effectDescription);
            effectText.gameObject.SetActive(true);
        }

        if (previewCardController != null)
        {
            previewCardController.Init(cardData, null);
            previewCardController.enabled = false;
        }
        else
        {
            previewCardDisplay.cardImage.sprite = cardData.image;
        }
        previewCardDisplay.ShowBack(false);
    }

    // 带着当前选中的卡进入 3D 预览
    private void OpenSelectedCardPreview()
    {
        if (selectedEntry == null) return;
        Vector2 scrollPosition = scrollRect == null ? Vector2.one : scrollRect.normalizedPosition;
        IsOpen = false;
        Time.timeScale = 1f;
        SceneFlowService.OpenCardPreview(selectedEntry.cardId, selectedRarity, scrollPosition);
    }

    // 给商店里所有文字换字体
    private void ApplyFont()
    {
        if (fontAsset == null) return;
        foreach (TMP_Text text in canvasObject.GetComponentsInChildren<TMP_Text>(true))
            text.font = fontAsset;
    }

    // 取卡名，查不到就用卡牌 id
    private static string GetCardName(int cardId)
    {
        CardData cardData = CampaignCatalog.GetCardData(cardId);
        return cardData == null ? cardId.ToString() : cardData.name;
    }

    // 按名字在场景里找物体
    private static GameObject FindSceneObject(string objectName)
    {
        foreach (Transform item in UnityEngine.Object.FindObjectsOfType<Transform>(true))
        {
            if (item.gameObject.name == objectName) return item.gameObject;
        }

        return null;
    }

    // 在节点下按名字找按钮
    private static Button FindButtonByName(GameObject root, string objectName)
    {
        foreach (Button button in root.GetComponentsInChildren<Button>(true))
        {
            if (button.gameObject.name == objectName) return button;
        }

        return null;
    }

    // 在节点下按名字找文字
    private static TMP_Text FindTextByName(GameObject root, string objectName)
    {
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.gameObject.name == objectName) return text;
        }

        return null;
    }

    // 在节点下按名字片段找文字
    private static TMP_Text FindTextContainsName(GameObject root, string namePart)
    {
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.gameObject.name.Contains(namePart)) return text;
        }

        return null;
    }

    // 在节点下按名字找子物体
    private static GameObject FindChildObjectByName(GameObject root, string objectName)
    {
        foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
        {
            if (item.gameObject.name == objectName) return item.gameObject;
        }

        return null;
    }

    // 在节点下按名字找 RawImage
    private static RawImage FindRawImageByName(GameObject root, string objectName)
    {
        foreach (RawImage image in root.GetComponentsInChildren<RawImage>(true))
        {
            if (image.gameObject.name == objectName) return image;
        }

        return null;
    }

}
