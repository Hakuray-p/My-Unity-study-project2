using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// HD 城市探索场景的总管理器，负责相机、UI、商店、卡组编辑和世界交互的协调
public sealed class HD2DSceneGM : MonoBehaviour
{
    private static HD2DSceneGM instance; // 全局单例

    public static Camera GameplayCamera => instance != null ? instance.sceneCamera : null;

    private CampaignSession session; // 当前存档会话
    private Camera sceneCamera; // 场景主相机
    private HD2DCameraFollow cameraFollow; // 相机跟随组件
    private Quaternion cameraRotation; // 相机固定朝向
    private Vector3 cameraOffset; // 相机相对玩家的偏移
    private CharacterGM characterGM; // 角色管理器
    private DialogueGM dialogueGM; // 对话管理器
    private EventGM eventGM; // 事件管理器
    private PauseGM pauseGM; // 暂停管理器
    private bool playerInitialized; // 玩家是否已经初始化
    private bool worldInitialized; // 世界是否已经初始化
    private bool depthOfFieldDisabled; // 景深是否已经关掉
    private Canvas canvas; // HUD 画布
    private Text hudText; // HUD 文字
    private string statusMessage = string.Empty; // 当前提示文字
    private float statusUntil; // 提示显示到什么时候
    private bool shopOpen; // 商店是否打开
    private bool deckOpen; // 卡组编辑器是否打开
    private List<int> deckDraft = new List<int>(); // 卡组编辑器的草稿
    [SerializeField] private CardListSO cardListSO; // 卡牌数据库
    private GameObject shopCanvasObject; // 商店画布
    private ScrollRect shopScrollRect; // 商店列表的滚动区
    private RectTransform shopContent; // 商店列表的内容节点
    private Button shopRarityRButton; // R 页签按钮
    private Button shopRaritySRButton; // SR 页签按钮
    private Button shopRarityURButton; // UR 页签按钮
    private Button shopBuyButton; // 购买按钮
    private Button shopPreviewButton; // 卡牌预览按钮
    private Button shopExitButton; // 退出商店按钮
    private TMP_Text shopGoldText; // 金币显示
    private TMP_FontAsset shopFontAsset; // 商店统一字体
    private TMP_Text shopNameText; // 卡名显示
    private TMP_Text shopEffectText; // 效果描述显示
    private TMP_Text shopPreviewText; // 预览提示文字
    private RawImage shopPreviewRawImage; // 3D 预览画面
    private Camera shopPreviewCamera; // 商店里的 3D 预览相机
    private GameObject shopPreviewModel; // 商店里的 3D 卡牌模型
    private CardDisplay shopPreviewCardDisplay; // 预览模型的显示组件
    private CardController shopPreviewCardController; // 预览模型的卡牌组件
    private CardShopEntry selectedShopEntry; // 当前选中的商品
    private CardRarity selectedShopRarity = CardRarity.Common; // 当前选中的稀有度页签
    private bool restoringShopState; // 是否正在还原进预览前的商店状态
    private Vector2 restoreShopScrollPosition; // 进预览前列表的滚动位置
    private readonly List<GameObject> shopItems = new List<GameObject>(); // 生成出来的商品行
    private bool shopUiInitialized; // 商店 UI 是否已经初始化

    // 占住单例并初始化城市场景
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

    // 离开场景前存一次玩家位置
    private void OnDestroy()
    {
        SavePlayer();
        if (instance == this) instance = null;
    }

    // 每帧依次处理场景初始化、关商店、暂停和世界交互
    private void Update()
    {
        TryInitializeScene();
        if (TryCloseShop()) return;
        if (TickPause()) return;
        if (!IsGameplayReady()) return;
        UpdateHud();
        TickWorldInteractions();
    }

    // 商店打开时按 Esc 关掉，返回 true 表示这一帧被商店吃掉了
    private bool TryCloseShop()
    {
        if (!shopOpen || !Input.GetKeyDown(KeyCode.Escape)) return false;
        ClosePanels();
        return true;
    }

    // 把面板打开状态交给暂停系统，返回 true 表示暂停菜单开着
    private bool TickPause()
    {
        bool panelOpen = (dialogueGM != null && dialogueGM.IsOpen) || shopOpen || deckOpen;
        if (pauseGM != null) pauseGM.Tick(panelOpen);
        return pauseGM != null && pauseGM.IsOpen;
    }

    // 玩家和存档都就绪才算能开始交互
    private bool IsGameplayReady()
    {
        return session != null && session.State != null && characterGM != null && characterGM.Player != null;
    }

    // 商店或卡组编辑开着时不响应世界交互
    private void TickWorldInteractions()
    {
        if (shopOpen || deckOpen) return;
        if (eventGM != null) eventGM.Tick();
        if (dialogueGM != null) dialogueGM.Tick();
    }

    // 每帧补一次场景初始化和相机跟随
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

    // 场景对象上缺哪个管理器就补哪个
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

    // 分两步初始化，先玩家后世界，每次 Update 都可以安全重入
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
            TryRestoreShopState();
            worldInitialized = true;
        }
    }

    // 找到场景主摄像机并调好参数
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
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            sceneCamera = cameraObject.AddComponent<Camera>();
            sceneCamera.transform.position = new Vector3(0f, 12f, -10f);
            sceneCamera.transform.rotation = Quaternion.Euler(35f, 0f, 0f);
        }

        sceneCamera.enabled = true;
        sceneCamera.tag = "MainCamera";
        foreach (Camera candidate in cameras)
        {
            if (candidate == sceneCamera) continue;
            if (candidate.gameObject.name == "ShopCardPreviewCamera")
            {
                candidate.enabled = true;
                AudioListener previewListener = candidate.GetComponent<AudioListener>();
                if (previewListener != null) previewListener.enabled = false;
                continue;
            }
            candidate.enabled = false;
            AudioListener listener = candidate.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = false;
        }
        Animator animator = sceneCamera.GetComponent<Animator>();
        if (animator != null) animator.enabled = false;
        UnityEngine.Playables.PlayableDirector director = sceneCamera.GetComponent<UnityEngine.Playables.PlayableDirector>();
        if (director != null)
        {
            director.Stop();
            director.enabled = false;
        }
    }

    // 复制一份 Volume 配置并关掉景深，避免影响城市画面
    private void DisableDepthOfField()
    {
        if (depthOfFieldDisabled) return;
        Volume[] volumes = FindObjectsOfType<Volume>(true);
        foreach (Volume volume in volumes)
        {
            if (volume.sharedProfile == null) continue;
            if (!volume.sharedProfile.TryGet(out DepthOfField depthOfField)) continue;
            VolumeProfile runtimeProfile = Instantiate(volume.sharedProfile);
            if (runtimeProfile.TryGet(out DepthOfField runtimeDepthOfField))
                runtimeDepthOfField.active = false;
            volume.profile = runtimeProfile;
        }
        depthOfFieldDisabled = true;
    }

    // 按相机朝向算出跟随偏移
    private void ConfigureFollowOffset()
    {
        cameraRotation = sceneCamera.transform.rotation;
        Vector3 cameraForward = cameraRotation * Vector3.forward;
        cameraOffset = Vector3.up * 1.2f - cameraForward.normalized * 8f;
    }

    // 给摄像机挂上跟随组件
    private void ConfigureCameraFollow()
    {
        if (sceneCamera == null || characterGM == null || characterGM.Player == null) return;
        cameraFollow = sceneCamera.GetComponent<HD2DCameraFollow>();
        if (cameraFollow == null) cameraFollow = sceneCamera.gameObject.AddComponent<HD2DCameraFollow>();
        cameraFollow.Bind(characterGM.Player, cameraOffset, cameraRotation);
    }

    // 把相机贴到玩家身上
    private void ApplyCameraFollow()
    {
        if (characterGM == null || characterGM.Player == null || sceneCamera == null) return;
        if (cameraFollow == null) ConfigureCameraFollow();
        if (cameraFollow != null) cameraFollow.Bind(characterGM.Player, cameraOffset, cameraRotation);
        sceneCamera.transform.SetPositionAndRotation(characterGM.Player.position + cameraOffset, cameraRotation);
    }

    // 搭出 HUD 和商店、卡组要用的事件系统
    private void CreateUi()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        var canvasObject = new GameObject("HD2D UI");
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

    // 创建一个 HUD 用的文字
    private Text CreateText(string objectName, Transform parent, int size, Color color)
    {
        var textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);
        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    // 刷新顶部的城市、积分、金币信息
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

    // 打开商店并暂停世界
    private void OpenShopPanel()
    {
        shopOpen = true;
        deckOpen = false;
        Time.timeScale = 0f;
        InitializeShopUi();
        if (shopCanvasObject != null) shopCanvasObject.SetActive(true);
        SelectShopRarity(selectedShopRarity);
    }

    // 从卡牌预览返回时还原商店的页签、选中项和滚动位置
    private void TryRestoreShopState()
    {
        if (!SceneFlowService.TryConsumeShopRestoreState(out int cardId, out CardRarity rarity, out Vector2 scrollPosition)) return;
        restoringShopState = true;
        restoreShopScrollPosition = scrollPosition;
        selectedShopRarity = rarity;
        selectedShopEntry = new CardShopEntry { cardId = cardId, rarity = rarity };
        OpenShopPanel();
    }

    // 带着当前选中的卡进入 3D 预览
    private void OpenSelectedCardPreview()
    {
        if (selectedShopEntry == null) return;
        Vector2 scrollPosition = shopScrollRect == null ? Vector2.one : shopScrollRect.normalizedPosition;
        shopOpen = false;
        Time.timeScale = 1f;
        SceneFlowService.OpenCardPreview(selectedShopEntry.cardId, selectedShopRarity, scrollPosition);
    }

    // 玩家碰到交互点后按类型分派：比赛 / 商店 / 事件
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

    // 开一场比赛，已经有没打完的战斗时改为继续那场
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

    // 赛事不能打的原因，能打就返回空
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

    // 打开卡组编辑器并暂停世界
    private void OpenDeck()
    {
        deckDraft = new List<int>(session.State.deckDraftCardIds);
        deckOpen = true;
        Time.timeScale = 0f;
    }

    // 没有别的面板打开时才允许开卡组编辑器
    public void OpenDeckEditor()
    {
        if ((dialogueGM == null || !dialogueGM.IsOpen) && !shopOpen) OpenDeck();
    }

    // 关掉所有面板并把时间恢复过来
    private void ClosePanels()
    {
        if (dialogueGM != null) dialogueGM.Close();
        shopOpen = false;
        deckOpen = false;
        if (shopCanvasObject != null) shopCanvasObject.SetActive(false);
        if (shopPreviewModel != null) shopPreviewModel.SetActive(false);
        Time.timeScale = 1f;
    }

    // 把玩家位置写进存档
    private void SavePlayer()
    {
        if (characterGM == null || characterGM.Player == null || session == null || session.State == null) return;
        session.SetPlayerPosition(characterGM.Player.position);
        session.Save();
    }

    // 在 HUD 上弹一条限时提示
    private void ShowStatus(string message)
    {
        statusMessage = message;
        statusUntil = Time.unscaledTime + 3f;
    }

    // 卡组编辑器打开时用 IMGUI 画出来
    private void OnGUI()
    {
        if (session == null || session.State == null) return;
        if (deckOpen) DrawDeckEditor();
    }

    // 用 IMGUI 画一个临时的卡组编辑器
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

    // 第一次开商店时按对象名找到画布上的控件并接好事件
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
        shopExitButton = FindButtonByName(shopCanvasObject, "退出商店购买Buttom");
        shopGoldText = FindTextByName(shopCanvasObject, "金币数量Text (TMP)");
        shopNameText = FindTextByName(shopCanvasObject, "此处为卡片物品的名字预览");
        shopEffectText = FindTextByName(shopCanvasObject, "此处为卡片效果描述Text");
        shopPreviewText = FindTextContainsName(shopCanvasObject, "此处为预览整张3D卡详");
        shopPreviewButton = FindButtonByName(shopCanvasObject, "卡牌预览Buttom");
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

        var tabRaycastPadding = new Vector4(-16f, -22f, -16f, -26f); // 标签按钮热区向外扩一圈，覆盖美术标签的整个可见范围
        shopRarityRButton.image.raycastPadding = tabRaycastPadding;
        shopRaritySRButton.image.raycastPadding = tabRaycastPadding;
        shopRarityURButton.image.raycastPadding = tabRaycastPadding;

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
        if (shopPreviewButton != null) shopPreviewButton.onClick.AddListener(OpenSelectedCardPreview);
        if (shopExitButton != null) shopExitButton.onClick.AddListener(ClosePanels);
        shopUiInitialized = true;
        shopCanvasObject.SetActive(false);
        UpdateShopGold();
    }

    // 切商店页签并重建列表
    private void SelectShopRarity(CardRarity rarity)
    {
        selectedShopRarity = rarity;
        RebuildShopItems();
    }

    // 按当前页签重建商品列表，并尽量选中原来那张卡
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
        if (restoringShopState && shopScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            shopScrollRect.normalizedPosition = restoreShopScrollPosition;
            restoringShopState = false;
        }
        else if (shopContent != null)
        {
            shopContent.anchoredPosition = Vector2.zero;
        }
    }

    // 在商店列表里创建一条商品
    private void CreateShopItem(CardShopEntry entry)
    {
        var itemObject = new GameObject("CardShopItem_" + entry.cardId,
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

        var textObject = new GameObject("CardIdText", typeof(RectTransform), typeof(TextMeshProUGUI));
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

    // 选中商品并刷新右侧预览
    private void SelectShopCard(CardShopEntry entry)
    {
        selectedShopEntry = entry;
        UpdateShopPreview();
    }

    // 刷新商店右侧的卡牌预览和购买按钮
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
        if (shopPreviewButton != null) shopPreviewButton.interactable = cardData != null;

        string reason;
        bool canBuy = session.CanBuyCard(selectedShopEntry, out reason);
        shopBuyButton.interactable = canBuy;
        SetButtonText(shopBuyButton, canBuy ? "购买" : reason);
    }

    // 买下选中的卡，失败就把原因显示出来
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

    // 刷新商店里的金币显示
    private void UpdateShopGold()
    {
        if (shopGoldText != null && session != null && session.State != null)
            shopGoldText.text = "金币：" + session.State.currency;
    }

    // 清空商店预览并禁用购买按钮
    private void ClearShopPreview()
    {
        UpdateShopCardPreview(null);
        if (shopBuyButton != null)
        {
            shopBuyButton.interactable = false;
            SetButtonText(shopBuyButton, "购买");
        }
    }

    // 隐藏商店预览里的文字
    private void HideShopPreviewTexts()
    {
        if (shopNameText != null) shopNameText.gameObject.SetActive(false);
        if (shopEffectText != null) shopEffectText.gameObject.SetActive(false);
        if (shopPreviewText != null) shopPreviewText.gameObject.SetActive(false);
    }

    // 关掉预览模型的碰撞和遮罩，让 3D 卡面干净显示
    private void ConfigureShopCardPreview()
    {
        if (shopPreviewCamera != null)
        {
            shopPreviewCamera.enabled = true;
            shopPreviewCamera.clearFlags = CameraClearFlags.SolidColor;
            shopPreviewCamera.backgroundColor = Color.clear;
            AudioListener listener = shopPreviewCamera.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = false;
            if (shopPreviewRawImage != null) shopPreviewRawImage.texture = shopPreviewCamera.targetTexture;
        }

        if (shopPreviewModel == null) return;
        foreach (Collider collider in shopPreviewModel.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;
        foreach (SpriteMask mask in shopPreviewModel.GetComponentsInChildren<SpriteMask>(true))
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

    // 设置商店效果文字的换行和溢出方式
    private void ConfigureShopEffectText()
    {
        if (shopEffectText == null) return;
        shopEffectText.enableWordWrapping = true;
        shopEffectText.overflowMode = TextOverflowModes.Masking;
    }

    // 把卡面数据和 3D 模型同步到商店预览
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

    // 给商店里所有文字换字体
    private void ApplyShopFont()
    {
        if (shopFontAsset == null) return;
        foreach (TMP_Text text in shopCanvasObject.GetComponentsInChildren<TMP_Text>(true))
            text.font = shopFontAsset;
    }

    // 取卡名，查不到就用卡牌 id
    private static string GetShopCardName(int cardId)
    {
        CardData cardData = CampaignCatalog.GetCardData(cardId);
        return cardData == null ? cardId.ToString() : cardData.name;
    }

    // 把稀有度换成 R / SR / UR 文字
    private static string GetRarityLabel(CardRarity rarity)
    {
        return rarity == CardRarity.Common ? "R" : rarity == CardRarity.Rare ? "SR" : "UR";
    }

    // 按名字在场景里找物体
    private static GameObject FindSceneObject(string objectName)
    {
        foreach (Transform item in FindObjectsOfType<Transform>(true))
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

    // 改按钮上的文字
    private static void SetButtonText(Button button, string value)
    {
        if (button == null) return;
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        if (text != null) text.text = value;
    }

    // 数一下卡组里某张卡有几张
    private static int CountCard(IList<int> cards, int cardId)
    {
        int count = 0;
        if (cards == null) return count;
        foreach (int id in cards) if (id == cardId) count++;
        return count;
    }
}
