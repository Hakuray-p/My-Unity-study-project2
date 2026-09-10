using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class CardPreviewSceneGM : MonoBehaviour
{
    [SerializeField] private Card3DPreviewController previewController; // 场景中的3D卡牌预览
    [SerializeField] private TMP_FontAsset previewFont; // 预览界面字体
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f); // UI参考分辨率

    private Camera previewCamera;
    private RectTransform canvasRoot; // 预览界面 Canvas 根节点

    /// <summary>
    /// 注册卡牌预览场景加载事件。
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneLoaded()
    {
        SceneManager.sceneLoaded -= CreateSceneManager;
        SceneManager.sceneLoaded += CreateSceneManager;
    }

    /// <summary>
    /// 为卡牌预览场景创建业务管理器。
    /// </summary>
    private static void CreateSceneManager(Scene loadedScene, LoadSceneMode loadMode)
    {
        if (loadedScene.name != "CardPreviewScene") return;
        if (FindObjectOfType<CardPreviewSceneGM>() == null)
            new GameObject("CardPreviewSceneGM").AddComponent<CardPreviewSceneGM>();
    }

    private void Awake()
    {
        Time.timeScale = 1f;
        previewController = FindObjectOfType<Card3DPreviewController>();
        previewCamera = Camera.main;
        ConfigureCamera();
        ConfigureLighting();
        if (previewFont == null) previewFont = previewController.GetComponentInChildren<TMP_Text>(true)?.font;
        CreateEventSystem();
        CreateCanvas();
    }

    private void Start()
    {
        if (!SceneFlowService.TryConsumeCardPreviewRequest(out var cardId)) cardId = 1001;
        var cardData = CampaignCatalog.GetCardData(cardId);
        previewController.SetPreviewCamera(previewCamera);
        previewController.SetCard(cardData);
        CreateHoloTuner(cardData);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) ReturnToShop();
    }

    private void ConfigureCamera()
    {
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = Color.black;
        previewCamera.fieldOfView = 35f;
        previewCamera.nearClipPlane = 0.01f;
        previewCamera.farClipPlane = 100f;
        previewCamera.allowHDR = true;
    }

    private void ConfigureLighting()
    {
        var keyLight = GameObject.Find("Directional Light")?.GetComponent<Light>();
        if (keyLight != null)
        {
            keyLight.type = LightType.Directional;
            keyLight.intensity = 1.15f;
            keyLight.color = new Color(0.86f, 0.91f, 1f, 1f);
            keyLight.transform.rotation = Quaternion.Euler(28f, -35f, 0f);
        }

        var fillLightObject = new GameObject("CardPreviewFillLight");
        var fillLight = fillLightObject.AddComponent<Light>();
        fillLight.type = LightType.Directional;
        fillLight.intensity = 0.35f;
        fillLight.color = new Color(1f, 0.55f, 0.42f, 1f);
        fillLightObject.transform.rotation = Quaternion.Euler(-20f, 145f, 0f);
    }

    private void CreateEventSystem()
    {
        if (EventSystem.current != null) return;
        var eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private void CreateCanvas()
    {
        var canvasObject = new GameObject("CardPreviewCanvas");
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var canvasScaler = canvasObject.AddComponent<CanvasScaler>();
        canvasScaler.referenceResolution = referenceResolution;
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.AddComponent<GraphicRaycaster>();
        canvasRoot = canvasObject.GetComponent<RectTransform>();

        var returnButton = CreateButton(canvasObject.transform, "返回商店Button", "返回商店");
        var returnRect = returnButton.GetComponent<RectTransform>();
        returnRect.anchorMin = new Vector2(0f, 1f);
        returnRect.anchorMax = new Vector2(0f, 1f);
        returnRect.pivot = new Vector2(0f, 1f);
        returnRect.anchoredPosition = new Vector2(32f, -32f);
        returnRect.sizeDelta = new Vector2(190f, 58f);
        returnButton.onClick.AddListener(ReturnToShop);

        var hintText = CreateText(canvasObject.transform, "操作提示Text", "左键拖拽旋转 · 滚轮缩放 · 右侧面板调节光影 · Esc 返回商店");
        var hintRect = hintText.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0.5f, 0f);
        hintRect.anchorMax = new Vector2(0.5f, 0f);
        hintRect.pivot = new Vector2(0.5f, 0f);
        hintRect.anchoredPosition = new Vector2(0f, 28f);
        hintRect.sizeDelta = new Vector2(980f, 42f);
        hintText.alignment = TextAlignmentOptions.Center;
        hintText.fontSize = 24f;
        hintText.raycastTarget = false;
    }

    private Button CreateButton(Transform targetParent, string objectName, string label)
    {
        var buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(targetParent, false);
        var background = buttonObject.GetComponent<Image>();
        background.color = new Color(0.12f, 0.17f, 0.28f, 0.95f);
        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = background;
        var text = CreateText(buttonObject.transform, "Label", label);
        var textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 22f;
        text.raycastTarget = false;
        return button;
    }

    private TMP_Text CreateText(Transform targetParent, string objectName, string value)
    {
        var textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(targetParent, false);
        var text = textObject.GetComponent<TMP_Text>();
        text.text = value;
        text.font = previewFont;
        text.color = Color.white;
        text.fontSize = 20f;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }

    /// <summary>
    /// 创建卡牌光影调参面板。
    /// </summary>
    private void CreateHoloTuner(CardData cardData)
    {
        var tunerObject = new GameObject("CardHoloTunerPanel", typeof(RectTransform));
        var tunerRect = tunerObject.GetComponent<RectTransform>();
        tunerRect.SetParent(canvasRoot, false);
        tunerRect.anchorMin = Vector2.zero;
        tunerRect.anchorMax = Vector2.one;
        tunerRect.offsetMin = Vector2.zero;
        tunerRect.offsetMax = Vector2.zero;
        tunerObject.AddComponent<CardHoloTunerPanel>().Initialize(previewController, previewFont, CardHoloTierMapper.FromRarity(cardData.rarity));
    }

    private void ReturnToShop()
    {
        SceneFlowService.ReturnFromCardPreview();
    }
}
