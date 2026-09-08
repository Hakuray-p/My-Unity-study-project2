using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class HD2DSceneGM : MonoBehaviour
{
    private static HD2DSceneGM instance;

    public static Camera GameplayCamera => instance != null ? instance.sceneCamera : null;

    private CampaignSession session;
    private Camera sceneCamera;
    private HD2DCameraFollow cameraFollow;
    private Quaternion cameraRotation;
    private Vector3 cameraOffset;
    private CharacterGM characterGM;
    private DialogueGM dialogueGM;
    private EventGM eventGM;
    private PauseGM pauseGM;
    private bool playerInitialized;
    private bool worldInitialized;
    private bool depthOfFieldDisabled;
    private Canvas canvas;
    private Text hudText;
    private string statusMessage = string.Empty;
    private float statusUntil;
    private bool shopOpen;
    private bool deckOpen;
    private List<int> deckDraft = new List<int>();
    [SerializeField] private CardListSO cardListSO; // 卡牌数据库
    private GameObject shopCanvasObject;
    private ScrollRect shopScrollRect;
    private RectTransform shopContent;
    private Button shopRarityRButton;
    private Button shopRaritySRButton;
    private Button shopRarityURButton;
    private Button shopBuyButton;
    private TMP_Text shopGoldText;
    private TMP_FontAsset shopFontAsset;
    private TMP_Text shopNameText;
    private TMP_Text shopEffectText;
    private TMP_Text shopPreviewText;
    private RawImage shopPreviewRawImage;
    private Camera shopPreviewCamera;
    private GameObject shopPreviewModel;
    private CardDisplay shopPreviewCardDisplay;
    private CardController shopPreviewCardController;
    private CardShopEntry selectedShopEntry;
    private CardRarity selectedShopRarity = CardRarity.Common;
    private readonly List<GameObject> shopItems = new List<GameObject>();
    private bool shopUiInitialized;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        Time.timeScale = 1f;
        CampaignCatalog.SetCardDatabase(cardListSO);
        session = CampaignSession.Instance;
        ConfigureCamera();
        DisableDepthOfField();
        EnsureManagers();
        TryInitializeScene();

    }

    private void OnDestroy()
    {
        SavePlayer();
        if (instance == this) instance = null;
    }

    private void Update()
    {
        TryInitializeScene();
        if (TryCloseShop()) return;
        if (TickPause()) return;
        if (!IsGameplayReady()) return;
        UpdateHud();
        TickWorldInteractions();
    }

    private bool TryCloseShop()
    {
        if (!shopOpen || !Input.GetKeyDown(KeyCode.Escape)) return false;
        ClosePanels();
        return true;
    }

    private bool TickPause()
    {
        bool panelOpen = (dialogueGM != null && dialogueGM.IsOpen) || shopOpen || deckOpen;
        if (pauseGM != null) pauseGM.Tick(panelOpen);
        return pauseGM != null && pauseGM.IsOpen;
    }

    private bool IsGameplayReady()
    {
        return session != null && session.State != null && characterGM != null && characterGM.Player != null;
    }

    private void TickWorldInteractions()
    {
        if (shopOpen || deckOpen) return;
        if (eventGM != null) eventGM.Tick();
        if (dialogueGM != null) dialogueGM.Tick();
    }

    private void LateUpdate()
    {
        TryInitializeScene();
        if (characterGM != null && characterGM.Player != null)
        {
            AmiyaCharacter character = characterGM.Player.GetComponent<AmiyaCharacter>();
            if (character != null && character.gameplayCamera != sceneCamera)
                character.gameplayCamera = sceneCamera;
        }
        ApplyCameraFollow();
    }

    private void EnsureManagers()
    {
        characterGM = gameObject.GetComponent<CharacterGM>();
        if (characterGM == null) characterGM = gameObject.AddComponent<CharacterGM>();
        dialogueGM = gameObject.GetComponent<DialogueGM>();
        if (dialogueGM == null) dialogueGM = gameObject.AddComponent<DialogueGM>();
        eventGM = gameObject.GetComponent<EventGM>();
        if (eventGM == null) eventGM = gameObject.AddComponent<EventGM>();
        pauseGM = gameObject.GetComponent<PauseGM>();
        if (pauseGM == null) pauseGM = gameObject.AddComponent<PauseGM>();
    }

    private void TryInitializeScene()
    {
        if (session == null) session = CampaignSession.Instance;
        if (sceneCamera == null) ConfigureCamera();
        if (characterGM == null || dialogueGM == null || eventGM == null || pauseGM == null) EnsureManagers();
        characterGM.Initialize(session, sceneCamera);
        if (!characterGM.IsReady) return;

        if (!playerInitialized)
        {
            characterGM.RestorePlayer();
            ConfigureFollowOffset();
            ConfigureCameraFollow();
            playerInitialized = true;
        }

        if (!worldInitialized)
        {
            CreateUi();
            characterGM.CreateWorldActors();
            eventGM.Initialize(session, characterGM, ShowStatus);
            dialogueGM.Initialize(characterGM, session, eventGM, StartMatch, OpenShopPanel, ShowStatus, ClosePanels);
            pauseGM.Initialize(characterGM, session, SavePlayer, ClosePanels);
            InitializeShopUi();
            worldInitialized = true;
        }
    }

    private void ConfigureCamera()
    {
        Camera[] cameras = FindObjectsOfType<Camera>(true);
        foreach (Camera candidate in cameras)
        {
            if (candidate != null && candidate.gameObject.name == "Main Camera")
            {
                sceneCamera = candidate;
                break;
            }
        }
        if (sceneCamera == null)
            foreach (Camera candidate in cameras)
                if (candidate != null && candidate.CompareTag("MainCamera"))
                {
                    sceneCamera = candidate;
                    break;
                }
        if (sceneCamera == null && cameras.Length > 0) sceneCamera = cameras[0];
        if (sceneCamera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            sceneCamera = cameraObject.AddComponent<Camera>();
            sceneCamera.transform.position = new Vector3(0f, 12f, -10f);
            sceneCamera.transform.rotation = Quaternion.Euler(35f, 0f, 0f);
        }

        sceneCamera.enabled = true;
        sceneCamera.tag = "MainCamera";
        foreach (Camera candidate in cameras)
        {
            if (candidate == null || candidate == sceneCamera) continue;
            if (candidate.gameObject.name == "ShopCardPreviewCamera")
            {
                candidate.enabled = true;
                var previewListener = candidate.GetComponent<AudioListener>();
                if (previewListener != null) previewListener.enabled = false;
                continue;
            }
            candidate.enabled = false;
            var listener = candidate.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = false;
        }
        Animator animator = sceneCamera.GetComponent<Animator>();
        if (animator != null) animator.enabled = false;
        var director = sceneCamera.GetComponent<UnityEngine.Playables.PlayableDirector>();
        if (director != null)
        {
            director.Stop();
            director.enabled = false;
        }
    }

    private void DisableDepthOfField()
    {
        if (depthOfFieldDisabled) return;
        Volume[] volumes = FindObjectsOfType<Volume>(true);
        foreach (Volume volume in volumes)
        {
            if (volume == null || volume.sharedProfile == null) continue;
            if (!volume.sharedProfile.TryGet(out DepthOfField depthOfField)) continue;
            VolumeProfile runtimeProfile = Instantiate(volume.sharedProfile);
            if (runtimeProfile.TryGet(out DepthOfField runtimeDepthOfField))
                runtimeDepthOfField.active = false;
            volume.profile = runtimeProfile;
        }
        depthOfFieldDisabled = true;
    }

    private void ConfigureFollowOffset()
    {
        cameraRotation = sceneCamera.transform.rotation;
        Vector3 cameraForward = cameraRotation * Vector3.forward;
        cameraOffset = Vector3.up * 1.2f - cameraForward.normalized * 8f;
    }

    private void ConfigureCameraFollow()
    {
        if (sceneCamera == null || characterGM == null || characterGM.Player == null) return;
        cameraFollow = sceneCamera.GetComponent<HD2DCameraFollow>();
        if (cameraFollow == null) cameraFollow = sceneCamera.gameObject.AddComponent<HD2DCameraFollow>();
        cameraFollow.Bind(characterGM.Player, cameraOffset, cameraRotation);
    }

    private void ApplyCameraFollow()
    {
        if (characterGM == null || characterGM.Player == null || sceneCamera == null) return;
        if (cameraFollow == null) ConfigureCameraFollow();
        if (cameraFollow != null) cameraFollow.Bind(characterGM.Player, cameraOffset, cameraRotation);
        sceneCamera.transform.SetPositionAndRotation(characterGM.Player.position + cameraOffset, cameraRotation);
    }

    private void CreateUi()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        GameObject canvasObject = new GameObject("HD2D UI");
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();
        hudText = CreateText("HD2D HUD", canvas.transform, 22, Color.white);
        hudText.rectTransform.anchorMin = new Vector2(0f, 1f);
        hudText.rectTransform.anchorMax = new Vector2(0f, 1f);
        hudText.rectTransform.pivot = new Vector2(0f, 1f);
        hudText.rectTransform.anchoredPosition = new Vector2(28f, -24f);
        hudText.rectTransform.sizeDelta = new Vector2(850f, 150f);
    }

    private Text CreateText(string objectName, Transform parent, int size, Color color)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);
        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private void UpdateHud()
    {
        CityData city = CampaignCatalog.GetCity(session.State.currentCityId);
        CitySaveData state = session.GetCityState(session.State.currentCityId);
        if (hudText == null) return;
        string status = Time.unscaledTime < statusUntil ? "\n" + statusMessage : string.Empty;
        hudText.text = (city?.displayName ?? "第一城") + " | 积分：" + state.leaguePoints + "/" + city?.requiredPoints +
            " | 金币：" + session.State.currency + " | 收藏：" + session.State.collectedCardIds.Count +
            " 张 | 徽章：" + session.State.badgeIds.Count + status;
    }

    private void OpenShopPanel()
    {
        shopOpen = true;
        deckOpen = false;
        Time.timeScale = 0f;
        InitializeShopUi();
        if (shopCanvasObject != null) shopCanvasObject.SetActive(true);
        SelectShopRarity(selectedShopRarity);
    }

    internal void Activate(WorldInteractionActor actor)
    {
        if (actor.type == WorldInteractionType.Match)
        {
            StartMatch(actor.id);
            return;
        }
        if (actor.type == WorldInteractionType.Shop)
        {
            OpenShopPanel();
            return;
        }
        dialogueGM.Close();
        if (eventGM != null) eventGM.StartEvent(actor.id);
    }

    private void StartMatch(string matchId)
    {
        MatchData match = CampaignCatalog.GetMatch(matchId);
        if (match == null)
        {
            ShowStatus("赛事不存在");
            return;
        }
        if (session.HasPendingBattle)
        {
            Time.timeScale = 1f;
            ClosePanels();
            SceneFlowService.ResumePendingBattle();
            return;
        }
        if (!session.HasLegalDeck)
        {
            ShowStatus("当前卡组不合法，请先完成卡组编辑");
            OpenDeck();
            return;
        }
        if (!session.CanStartMatch(match))
        {
            ShowStatus(GetMatchLockReason(match));
            return;
        }
        SavePlayer();
        ClosePanels();
        SceneFlowService.StartMatch(match.matchId, characterGM.Player.position);
    }

    private string GetMatchLockReason(MatchData match)
    {
        if (match == null) return "赛事不存在";
        if (session.HasPendingBattle) return "当前已有一场未结束的战斗";
        if (!session.IsCityUnlocked(match.cityId)) return "当前城市尚未解锁";
        if (match.matchType == MatchType.Champion && session.GetLeaguePoints(match.cityId) < CampaignCatalog.GetCity(match.cityId).requiredPoints)
            return $"城市冠军需要 {CampaignCatalog.GetCity(match.cityId).requiredPoints} 积分";
        if (!string.IsNullOrEmpty(match.prerequisiteMatchId) && !session.IsMatchComplete(match.prerequisiteMatchId))
            return $"需要先完成：{CampaignCatalog.GetMatch(match.prerequisiteMatchId).displayName}";
        if (!session.HasLegalDeck) return "当前卡组不合法，请按 B 编辑卡组";
        return "当前无法开始赛事";
    }

    private void OpenDeck()
    {
        deckDraft = new List<int>(session.State.deckDraftCardIds);
        deckOpen = true;
        Time.timeScale = 0f;
    }

    public void OpenDeckEditor()
    {
        if ((dialogueGM == null || !dialogueGM.IsOpen) && !shopOpen) OpenDeck();
    }

    private void ClosePanels()
    {
        if (dialogueGM != null) dialogueGM.Close();
        shopOpen = false;
        deckOpen = false;
        if (shopCanvasObject != null) shopCanvasObject.SetActive(false);
        if (shopPreviewModel != null) shopPreviewModel.SetActive(false);
        Time.timeScale = 1f;
    }

    private void SavePlayer()
    {
        if (characterGM == null || characterGM.Player == null || session == null || session.State == null) return;
        session.SetPlayerPosition(characterGM.Player.position);
        session.Save();
    }

    private void ShowStatus(string message)
    {
        statusMessage = message;
        statusUntil = Time.unscaledTime + 3f;
    }

    private void OnGUI()
    {
        if (session == null || session.State == null) return;
        if (deckOpen) DrawDeckEditor();
    }

    private void DrawDeckEditor()
    {
        GUI.Box(new Rect(230f, 100f, Screen.width - 460f, Screen.height - 200f), "卡组编辑器");
        GUI.Label(new Rect(260f, 140f, 700f, 30f), $"草稿：{deckDraft.Count}/20    {session.GetDeckValidationError(deckDraft)}");
        IReadOnlyList<CardShopEntry> cards = CampaignCatalog.GetFirstCityShop();
        for (int i = 0; i < cards.Count; i++)
        {
            int cardId = cards[i].cardId;
            int count = CountCard(deckDraft, cardId);
            float y = 190f + i * 28f;
            GUI.Label(new Rect(270f, y, 170f, 24f), $"{cardId}  x{count}");
            GUI.enabled = session.State.collectedCardIds.Contains(cardId) && count < 2;
            if (GUI.Button(new Rect(445f, y, 42f, 23f), "+")) deckDraft.Add(cardId);
            GUI.enabled = count > 0;
            if (GUI.Button(new Rect(492f, y, 42f, 23f), "-")) deckDraft.Remove(cardId);
            GUI.enabled = true;
        }
        if (GUI.Button(new Rect(270f, Screen.height - 155f, 180f, 42f), "保存草稿"))
        {
            session.SaveDeckDraft(deckDraft);
            ShowStatus("草稿已保存");
        }
        if (GUI.Button(new Rect(470f, Screen.height - 155f, 180f, 42f), "恢复最近合法卡组")) deckDraft = session.GetBattleDeck();
        if (GUI.Button(new Rect(Screen.width - 470f, Screen.height - 155f, 150f, 42f), "关闭")) ClosePanels();
    }

    private void InitializeShopUi()
    {
        if (shopUiInitialized)
        {
            UpdateShopGold();
            return;
        }

        shopCanvasObject = FindSceneObject("商店Canvas") ?? FindSceneObject("商点Canvas");
        if (shopCanvasObject == null) return;

        shopScrollRect = shopCanvasObject.GetComponentInChildren<ScrollRect>(true);
        if (shopScrollRect != null) shopContent = shopScrollRect.content;
        shopRarityRButton = FindButtonByName(shopCanvasObject, "R卡牌Button");
        shopRaritySRButton = FindButtonByName(shopCanvasObject, "SR卡牌Button");
        shopRarityURButton = FindButtonByName(shopCanvasObject, "UR卡牌Button");
        shopBuyButton = FindButtonByName(shopCanvasObject, "商店购买Buttom");
        shopGoldText = FindTextByName(shopCanvasObject, "金币数量Text (TMP)");
        shopNameText = FindTextByName(shopCanvasObject, "此处为卡片物品的名字预览");
        shopEffectText = FindTextByName(shopCanvasObject, "此处为卡片效果描述Text");
        shopPreviewText = FindTextContainsName(shopCanvasObject, "此处为预览整张3D卡详");
        shopPreviewRawImage = FindRawImageByName(shopCanvasObject, "CardPreviewRawImage");
        shopPreviewCamera = FindSceneObject("ShopCardPreviewCamera")?.GetComponent<Camera>();
        shopPreviewModel = FindSceneObject("CarShopCardPreviewModel");
        shopPreviewCardDisplay = shopPreviewModel == null ? null : shopPreviewModel.GetComponentInChildren<CardDisplay>(true);
        shopPreviewCardController = shopPreviewModel == null ? null : shopPreviewModel.GetComponentInChildren<CardController>(true);
        ConfigureShopEffectText();
        ConfigureShopCardPreview();
        shopFontAsset = shopCanvasObject.GetComponentInChildren<TMP_Text>(true)?.font;
        HideShopPreviewTexts();
        ApplyShopFont();

        if (shopScrollRect == null || shopContent == null || shopRarityRButton == null ||
            shopRaritySRButton == null || shopRarityURButton == null || shopBuyButton == null)
            return;

        shopScrollRect.horizontal = false;
        shopScrollRect.vertical = true;
        shopContent.anchorMin = new Vector2(0f, 1f);
        shopContent.anchorMax = new Vector2(1f, 1f);
        shopContent.pivot = new Vector2(0.5f, 1f);

        VerticalLayoutGroup layoutGroup = shopContent.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup == null) layoutGroup = shopContent.gameObject.AddComponent<VerticalLayoutGroup>();
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.spacing = 8f;

        ContentSizeFitter sizeFitter = shopContent.GetComponent<ContentSizeFitter>();
        if (sizeFitter == null) sizeFitter = shopContent.gameObject.AddComponent<ContentSizeFitter>();
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        shopRarityRButton.onClick.AddListener(() => SelectShopRarity(CardRarity.Common));
        shopRaritySRButton.onClick.AddListener(() => SelectShopRarity(CardRarity.Rare));
        shopRarityURButton.onClick.AddListener(() => SelectShopRarity(CardRarity.Limited));
        shopBuyButton.onClick.AddListener(BuySelectedShopCard);
        shopUiInitialized = true;
        shopCanvasObject.SetActive(false);
        UpdateShopGold();
    }

    private void SelectShopRarity(CardRarity rarity)
    {
        selectedShopRarity = rarity;
        RebuildShopItems();
    }

    private void RebuildShopItems()
    {
        if (!shopUiInitialized) return;

        int selectedCardId = selectedShopEntry == null ? 0 : selectedShopEntry.cardId;
        foreach (GameObject item in shopItems)
        {
            if (item != null) Destroy(item);
        }
        shopItems.Clear();

        IReadOnlyList<CardShopEntry> entries = CampaignCatalog.GetFirstCityShop();
        CardShopEntry firstEntry = null;
        CardShopEntry restoredEntry = null;
        foreach (CardShopEntry entry in entries)
        {
            if (entry.rarity != selectedShopRarity) continue;
            if (firstEntry == null) firstEntry = entry;
            if (entry.cardId == selectedCardId) restoredEntry = entry;
            CreateShopItem(entry);
        }

        selectedShopEntry = restoredEntry ?? firstEntry;
        if (selectedShopEntry != null) UpdateShopPreview();
        else ClearShopPreview();
        if (shopContent != null) shopContent.anchoredPosition = Vector2.zero;
    }

    private void CreateShopItem(CardShopEntry entry)
    {
        GameObject itemObject = new GameObject("CardShopItem_" + entry.cardId,
            typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        itemObject.transform.SetParent(shopContent, false);
        shopItems.Add(itemObject);

        RectTransform itemRect = itemObject.GetComponent<RectTransform>();
        itemRect.sizeDelta = new Vector2(0f, 58f);
        Image background = itemObject.GetComponent<Image>();
        background.color = new Color(0.2f, 0.1f, 0.1f, 0.78f);
        Button button = itemObject.GetComponent<Button>();
        button.targetGraphic = background;
        LayoutElement layoutElement = itemObject.GetComponent<LayoutElement>();
        layoutElement.preferredHeight = 58f;

        GameObject textObject = new GameObject("CardIdText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(itemObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(18f, 0f);
        textRect.offsetMax = new Vector2(-18f, 0f);
        TMP_Text text = textObject.GetComponent<TMP_Text>();
        if (shopFontAsset != null) text.font = shopFontAsset;
        CardData cardData = CampaignCatalog.GetCardData(entry.cardId);
        text.text = entry.cardId + "  " + (cardData == null ? "未命名卡牌" : cardData.name) +
            "  " + entry.price + "金币";
        text.fontSize = 24f;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.raycastTarget = false;

        CardShopEntry clickedEntry = entry;
        button.onClick.AddListener(() => SelectShopCard(clickedEntry));
    }

    private void SelectShopCard(CardShopEntry entry)
    {
        selectedShopEntry = entry;
        UpdateShopPreview();
    }

    private void UpdateShopPreview()
    {
        UpdateShopGold();
        if (selectedShopEntry == null)
        {
            ClearShopPreview();
            return;
        }

        CardData cardData = CampaignCatalog.GetCardData(selectedShopEntry.cardId);
        UpdateShopCardPreview(cardData);

        string reason;
        bool canBuy = session.CanBuyCard(selectedShopEntry, out reason);
        shopBuyButton.interactable = canBuy;
        SetButtonText(shopBuyButton, canBuy ? "购买" : reason);
    }

    private void BuySelectedShopCard()
    {
        if (selectedShopEntry == null) return;
        string reason;
        if (!session.BuyCard(selectedShopEntry, out reason))
        {
            ShowStatus(reason);
            UpdateShopPreview();
            return;
        }

        ShowStatus("已购买卡牌《" + GetShopCardName(selectedShopEntry.cardId) + "》");
        UpdateShopPreview();
    }

    private void UpdateShopGold()
    {
        if (shopGoldText != null && session != null && session.State != null)
            shopGoldText.text = "金币：" + session.State.currency;
    }

    private void ClearShopPreview()
    {
        UpdateShopCardPreview(null);
        if (shopBuyButton != null)
        {
            shopBuyButton.interactable = false;
            SetButtonText(shopBuyButton, "购买");
        }
    }

    private void HideShopPreviewTexts()
    {
        if (shopNameText != null) shopNameText.gameObject.SetActive(false);
        if (shopEffectText != null) shopEffectText.gameObject.SetActive(false);
        if (shopPreviewText != null) shopPreviewText.gameObject.SetActive(false);
    }

    private void ConfigureShopCardPreview()
    {
        if (shopPreviewCamera != null)
        {
            shopPreviewCamera.enabled = true;
            shopPreviewCamera.clearFlags = CameraClearFlags.SolidColor;
            shopPreviewCamera.backgroundColor = Color.clear;
            var listener = shopPreviewCamera.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = false;
            if (shopPreviewRawImage != null) shopPreviewRawImage.texture = shopPreviewCamera.targetTexture;
        }

        if (shopPreviewModel == null) return;
        foreach (var collider in shopPreviewModel.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;
        foreach (var mask in shopPreviewModel.GetComponentsInChildren<SpriteMask>(true))
            mask.enabled = false;
        GameObject effectPanel = FindChildObjectByName(shopPreviewModel, "EffectIcon");
        if (effectPanel != null) effectPanel.SetActive(false);
        if (shopPreviewCardDisplay != null && shopPreviewCardDisplay.cardImage != null)
            shopPreviewCardDisplay.cardImage.maskInteraction = SpriteMaskInteraction.None;
        if (shopPreviewCardDisplay != null && shopPreviewCardDisplay.effectText != null)
            shopPreviewCardDisplay.effectText.gameObject.SetActive(false);
        if (shopPreviewCardController != null) shopPreviewCardController.enabled = false;
        shopPreviewModel.SetActive(false);
    }

    private void ConfigureShopEffectText()
    {
        if (shopEffectText == null) return;
        shopEffectText.enableWordWrapping = true;
        shopEffectText.overflowMode = TextOverflowModes.Masking;
    }

    private void UpdateShopCardPreview(CardData cardData)
    {
        bool hasCard = cardData != null && shopPreviewCardDisplay != null;
        if (shopPreviewRawImage != null) shopPreviewRawImage.gameObject.SetActive(hasCard);
        if (shopPreviewModel != null) shopPreviewModel.SetActive(hasCard);
        if (!hasCard)
        {
            if (shopEffectText != null)
            {
                shopEffectText.text = string.Empty;
                shopEffectText.gameObject.SetActive(false);
            }
            return;
        }

        if (shopEffectText != null)
        {
            shopEffectText.text = Regex.Unescape(cardData.effectDescription);
            shopEffectText.gameObject.SetActive(true);
        }

        if (shopPreviewCardController != null)
        {
            shopPreviewCardController.Init(cardData, null);
            shopPreviewCardController.enabled = false;
        }
        else
        {
            shopPreviewCardDisplay.cardImage.sprite = cardData.image;
        }
        shopPreviewCardDisplay.ShowBack(false);
    }

    private void ApplyShopFont()
    {
        if (shopFontAsset == null) return;
        foreach (TMP_Text text in shopCanvasObject.GetComponentsInChildren<TMP_Text>(true))
            text.font = shopFontAsset;
    }

    private static string GetShopCardName(int cardId)
    {
        CardData cardData = CampaignCatalog.GetCardData(cardId);
        return cardData == null ? cardId.ToString() : cardData.name;
    }

    private static string GetRarityLabel(CardRarity rarity)
    {
        return rarity == CardRarity.Common ? "R" : rarity == CardRarity.Rare ? "SR" : "UR";
    }

    private static GameObject FindSceneObject(string objectName)
    {
        foreach (Transform item in FindObjectsOfType<Transform>(true))
        {
            if (item.gameObject.name == objectName) return item.gameObject;
        }

        return null;
    }

    private static Button FindButtonByName(GameObject root, string objectName)
    {
        foreach (Button button in root.GetComponentsInChildren<Button>(true))
        {
            if (button.gameObject.name == objectName) return button;
        }

        return null;
    }

    private static TMP_Text FindTextByName(GameObject root, string objectName)
    {
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.gameObject.name == objectName) return text;
        }

        return null;
    }

    private static TMP_Text FindTextContainsName(GameObject root, string namePart)
    {
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.gameObject.name.Contains(namePart)) return text;
        }

        return null;
    }

    private static GameObject FindChildObjectByName(GameObject root, string objectName)
    {
        foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
        {
            if (item.gameObject.name == objectName) return item.gameObject;
        }

        return null;
    }

    private static RawImage FindRawImageByName(GameObject root, string objectName)
    {
        foreach (RawImage image in root.GetComponentsInChildren<RawImage>(true))
        {
            if (image.gameObject.name == objectName) return image;
        }

        return null;
    }

    private static void SetButtonText(Button button, string value)
    {
        if (button == null) return;
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        if (text != null) text.text = value;
    }

    private static int CountCard(IList<int> cards, int cardId)
    {
        int count = 0;
        if (cards == null) return count;
        foreach (int id in cards) if (id == cardId) count++;
        return count;
    }
}
