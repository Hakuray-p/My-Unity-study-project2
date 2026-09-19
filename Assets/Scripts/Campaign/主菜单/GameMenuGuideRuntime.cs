using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 在主菜单显示当前项目的操作说明。
public static class GameMenuGuideRuntime
{
    // 注册主菜单场景载入回调。
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneLoaded()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // 创建操作说明页面并接入按键说明按钮。
    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "GameMenu") return;

        Canvas canvas = Object.FindObjectOfType<Canvas>();
        TMP_Text sourceText = Object.FindObjectOfType<TMP_Text>();
        GameObject guideObject = new GameObject("GameMenuGuide", typeof(RectTransform));
        guideObject.transform.SetParent(canvas.transform, false);
        GameMenuGuideView guideView = guideObject.AddComponent<GameMenuGuideView>();
        guideView.Initialize(SceneTool.Find<Button>("按键说明"), sourceText.font);
    }
}

// 显示主菜单的项目操作说明页面。
public sealed class GameMenuGuideView : MonoBehaviour
{
    private GameObject panel; // 操作说明面板

    // 创建操作说明页面并接入返回按钮。
    public void Initialize(Button guideButton, TMP_FontAsset fontAsset)
    {
        RectTransform root = GetComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
        Image background = gameObject.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.72f);
        panel = CreatePanel(fontAsset);
        guideButton.onClick.AddListener(Show);
        gameObject.SetActive(false);
    }

    // 关闭操作说明页面。
    private void Update()
    {
        if (gameObject.activeSelf && Input.GetKeyDown(KeyCode.Escape)) Hide();
    }

    // 显示操作说明页面。
    private void Show()
    {
        gameObject.SetActive(true);
        panel.SetActive(true);
    }

    // 隐藏操作说明页面。
    private void Hide()
    {
        gameObject.SetActive(false);
    }

    // 创建操作说明面板内容。
    private GameObject CreatePanel(TMP_FontAsset fontAsset)
    {
        GameObject panelObject = new GameObject("GuidePanel", typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(transform, false);
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(700f, 520f);
        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        panelImage.color = new Color(0.09f, 0.1f, 0.13f, 0.97f);
        CreateText(panelObject.transform, "Title", "操作说明", new Vector2(620f, 54f), new Vector2(0f, 214f), 32f, TextAlignmentOptions.Center, fontAsset);
        string guideText = "城市\n方向键 / WASD    移动角色\nE                 对话、调查或交互\nB                 打开卡组管理\nEsc               关闭当前面板或打开暂停菜单\n\n战斗\n鼠标左键          点击按钮、拖动卡牌、选择目标\n拖动卡牌          将卡牌打出到己方场地或选择目标\n拖动己方单位      选择攻击目标\n结束回合按钮      结束当前回合\n\n对话中点击对话框或继续按钮，可以显示下一句。";
        TMP_Text content = CreateText(panelObject.transform, "GuideText", guideText, new Vector2(590f, 350f), new Vector2(0f, 15f), 21f, TextAlignmentOptions.Left, fontAsset);
        content.enableWordWrapping = true;
        Button backButton = CreateButton(panelObject.transform, "BackButton", "返回", new Vector2(0f, -215f), fontAsset);
        backButton.onClick.AddListener(Hide);
        return panelObject;
    }

    // 创建说明文字。
    private TMP_Text CreateText(Transform parent, string objectName, string value, Vector2 size, Vector2 position, float fontSize, TextAlignmentOptions alignment, TMP_FontAsset fontAsset)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = size;
        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.font = fontAsset;
        text.text = value;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
    }

    // 创建返回按钮。
    private Button CreateButton(Transform parent, string objectName, string value, Vector2 position, TMP_FontAsset fontAsset)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = new Vector2(160f, 42f);
        Image image = buttonObject.GetComponent<Image>();
        image.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        image.color = new Color(0.22f, 0.28f, 0.36f, 1f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        CreateText(buttonObject.transform, "Text", value, new Vector2(150f, 38f), Vector2.zero, 19f, TextAlignmentOptions.Center, fontAsset);
        return button;
    }
}
