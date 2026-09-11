using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 卡牌预览场景的卡牌光影调参面板。
/// </summary>
public sealed class CardHoloTunerPanel : MonoBehaviour
{
    private const float PanelWidth = 420f; // 面板宽度
    private const float PanelHeight = 720f; // 面板高度
    private const float PanelMargin = 24f; // 面板与屏幕边缘的距离
    private const float RowHeight = 34f; // 单行参数高度
    private const float LabelWidth = 150f; // 参数名称宽度
    private const float ValueWidth = 62f; // 参数数值宽度

    private static readonly CardHoloTier[] TierOrder = { CardHoloTier.R, CardHoloTier.SR, CardHoloTier.UR }; // 档位页签顺序
    private static readonly Color PanelColor = new Color(0.12f, 0.17f, 0.28f, 0.94f); // 面板底色
    private static readonly Color ButtonColor = new Color(0.18f, 0.24f, 0.36f, 1f); // 按钮底色
    private static readonly Color SelectedColor = new Color(0.32f, 0.48f, 0.78f, 1f); // 选中页签底色
    private static readonly Color TrackColor = new Color(0.08f, 0.11f, 0.18f, 1f); // 滑条轨道底色
    private static readonly Color FillColor = new Color(0.42f, 0.62f, 0.95f, 1f); // 滑条填充底色
    private static readonly Color HandleColor = new Color(0.92f, 0.96f, 1f, 1f); // 滑条手柄底色

    private Card3DPreviewController previewController; // 预览控制器
    private CardHoloProfile holoProfile; // 光影配置资源
    private TMP_FontAsset panelFont; // 面板字体
    private CardHoloTier editingTier; // 当前编辑的光影档位
    private CardHoloProfile.TierSettings editingSettings; // 当前档位的编辑副本
    private readonly List<Image> tierButtonImages = new List<Image>(); // 档位页签底色

    private GameObject panelBody; // 面板主体
    private TMP_Text collapseLabel; // 收起按钮文字
    private RectTransform rowRoot; // 参数行容器

    /// <summary>
    /// 构建面板并绑定预览控制器。
    /// </summary>
    public void Initialize(Card3DPreviewController controller, TMP_FontAsset font, CardHoloTier tier)
    {
        previewController = controller;
        holoProfile = controller.HoloProfile;
        panelFont = font;
        BuildCollapseButton();
        BuildPanelBody();
        SelectTier(tier);
    }

    /// <summary>
    /// 切换编辑档位，并让预览卡牌按该档显示。
    /// </summary>
    private void SelectTier(CardHoloTier tier)
    {
        editingTier = tier;
        editingSettings = holoProfile.GetSettings(tier);
        previewController.HoloVisual.SetTier(tier);
        previewController.RefreshTextTint();
        for (int index = 0; index < tierButtonImages.Count; index++)
        {
            if (tierButtonImages[index] != null)
                tierButtonImages[index].color = TierOrder[index] == tier ? SelectedColor : ButtonColor;
        }

        RebuildRows();
    }

    /// <summary>
    /// 按当前编辑副本刷新参数行。
    /// </summary>
    private void RebuildRows()
    {
        foreach (Transform child in rowRoot) Destroy(child.gameObject);

        AddFloatRow("卡框反光强度", 0f, 1.5f, editingSettings.frameIntensity, value => editingSettings.frameIntensity = value);
        AddFloatRow("卡面纹理强度", 0f, 1.5f, editingSettings.surfaceIntensity, value => editingSettings.surfaceIntensity = value);
        AddFloatRow("卡图反光强度", 0f, 0.75f, editingSettings.artIntensity, value => editingSettings.artIntensity = value);
        AddFloatRow("纹理强度", 0f, 1f, editingSettings.textureStrength, value => editingSettings.textureStrength = value);
        AddFloatRow("纹理密度", 4f, 80f, editingSettings.textureScale, value => editingSettings.textureScale = value);
        AddFloatRow("光带宽度", 0.05f, 0.65f, editingSettings.bandWidth, value => editingSettings.bandWidth = value);
        AddFloatRow("彩谱频率", 0.25f, 3f, editingSettings.spectrumFrequency, value => editingSettings.spectrumFrequency = value);
        AddFloatRow("反光锐度", 0.5f, 6f, editingSettings.reflectionSharpness, value => editingSettings.reflectionSharpness = value);
        AddFloatRow("静止光影强度", 0f, 0.75f, editingSettings.idleStrength, value => editingSettings.idleStrength = value);
        AddFloatRow("字体变色强度", 0f, 1f, editingSettings.textIntensity, value => editingSettings.textIntensity = value);
        AddFloatRow("珍珠色 R", 0f, 1f, editingSettings.pearlTint.r, value => editingSettings.pearlTint.r = value);
        AddFloatRow("珍珠色 G", 0f, 1f, editingSettings.pearlTint.g, value => editingSettings.pearlTint.g = value);
        AddFloatRow("珍珠色 B", 0f, 1f, editingSettings.pearlTint.b, value => editingSettings.pearlTint.b = value);
        AddFloatRow("珍珠色 A", 0f, 1f, editingSettings.pearlTint.a, value => editingSettings.pearlTint.a = value);
        AddFloatRow("虹彩色 R", 0f, 1f, editingSettings.rainbowTint.r, value => editingSettings.rainbowTint.r = value);
        AddFloatRow("虹彩色 G", 0f, 1f, editingSettings.rainbowTint.g, value => editingSettings.rainbowTint.g = value);
        AddFloatRow("虹彩色 B", 0f, 1f, editingSettings.rainbowTint.b, value => editingSettings.rainbowTint.b = value);
        AddFloatRow("虹彩色 A", 0f, 1f, editingSettings.rainbowTint.a, value => editingSettings.rainbowTint.a = value);
        AddFloatRow("字体色 R", 0f, 1f, editingSettings.textTint.r, value => editingSettings.textTint.r = value);
        AddFloatRow("字体色 G", 0f, 1f, editingSettings.textTint.g, value => editingSettings.textTint.g = value);
        AddFloatRow("字体色 B", 0f, 1f, editingSettings.textTint.b, value => editingSettings.textTint.b = value);
        AddFloatRow("字体色 A", 0f, 1f, editingSettings.textTint.a, value => editingSettings.textTint.a = value);
    }

    /// <summary>
    /// 追加一行带名称与数值的滑条。
    /// </summary>
    private void AddFloatRow(string label, float minValue, float maxValue, float currentValue, UnityAction<float> onValueChanged)
    {
        GameObject row = CreateLayoutObject(rowRoot, label + "Row", RowHeight);
        HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 6f;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = true;

        TMP_Text labelText = CreateText(row.transform, "名称Text", label);
        labelText.alignment = TextAlignmentOptions.Left;
        labelText.GetComponent<LayoutElement>().preferredWidth = LabelWidth;
        labelText.GetComponent<LayoutElement>().minWidth = LabelWidth;

        TMP_Text valueText = CreateText(row.transform, "数值Text", currentValue.ToString("F3"));
        valueText.alignment = TextAlignmentOptions.Right;
        valueText.GetComponent<LayoutElement>().preferredWidth = ValueWidth;
        valueText.GetComponent<LayoutElement>().minWidth = ValueWidth;

        Slider slider = CreateSlider(row.transform, minValue, maxValue, currentValue, value =>
        {
            valueText.text = value.ToString("F3");
            onValueChanged(value);
            ApplyEditingSettings();
        });
        slider.GetComponent<LayoutElement>().flexibleWidth = 1f;
    }

    /// <summary>
    /// 把当前编辑副本写回配置资源并刷新预览光影。
    /// </summary>
    private void ApplyEditingSettings()
    {
        holoProfile.SetSettings(editingTier, editingSettings);
        previewController.HoloVisual.Refresh();
        previewController.RefreshTextTint();
    }

    /// <summary>
    /// 把当前档位恢复为脚本默认值。
    /// </summary>
    private void ResetEditingTier()
    {
        editingSettings = editingTier switch
        {
            CardHoloTier.R => CardHoloProfile.CreateRDefault(),
            CardHoloTier.SR => CardHoloProfile.CreateSrDefault(),
            CardHoloTier.UR => CardHoloProfile.CreateUrDefault(),
            _ => editingSettings
        };
        ApplyEditingSettings();
        RebuildRows();
    }

    /// <summary>
    /// 把当前配置写入 Project 中的光影资源。
    /// </summary>
    private void SaveProfile()
    {
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(holoProfile);
        UnityEditor.AssetDatabase.SaveAssets();
#endif
    }

    /// <summary>
    /// 收起或展开面板主体。
    /// </summary>
    private void TogglePanel()
    {
        panelBody.SetActive(!panelBody.activeSelf);
        collapseLabel.text = panelBody.activeSelf ? "收起面板" : "展开面板";
    }

    /// <summary>
    /// 创建右上角的收起按钮。
    /// </summary>
    private void BuildCollapseButton()
    {
        Button button = CreateButton(transform, "收起Button", "收起面板");
        collapseLabel = button.GetComponentInChildren<TMP_Text>();
        RectTransform buttonRect = button.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 1f);
        buttonRect.anchorMax = new Vector2(1f, 1f);
        buttonRect.pivot = new Vector2(1f, 1f);
        buttonRect.anchoredPosition = new Vector2(-PanelMargin, -PanelMargin);
        buttonRect.sizeDelta = new Vector2(190f, 48f);
        button.onClick.AddListener(TogglePanel);
    }

    /// <summary>
    /// 创建右侧面板主体。
    /// </summary>
    private void BuildPanelBody()
    {
        panelBody = CreateImageObject(transform, "光影调参面板", PanelColor);
        RectTransform bodyRect = panelBody.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(1f, 0f);
        bodyRect.anchorMax = new Vector2(1f, 0f);
        bodyRect.pivot = new Vector2(1f, 0f);
        bodyRect.anchoredPosition = new Vector2(-PanelMargin, PanelMargin);
        bodyRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

        VerticalLayoutGroup bodyLayout = panelBody.AddComponent<VerticalLayoutGroup>();
        bodyLayout.padding = new RectOffset(12, 12, 12, 12);
        bodyLayout.spacing = 8f;
        bodyLayout.childControlWidth = true;
        bodyLayout.childControlHeight = true;
        bodyLayout.childForceExpandWidth = true;
        bodyLayout.childForceExpandHeight = false;

        TMP_Text title = CreateText(panelBody.transform, "标题Text", "卡牌光影调节");
        title.alignment = TextAlignmentOptions.Center;
        title.fontSize = 24f;
        title.GetComponent<LayoutElement>().preferredHeight = 34f;

        BuildTierButtons();
        BuildRowsView();
        BuildFooterButtons();
    }

    /// <summary>
    /// 创建 R / SR / UR 档位页签。
    /// </summary>
    private void BuildTierButtons()
    {
        GameObject row = CreateLayoutObject(panelBody.transform, "档位行", 44f);
        HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 6f;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = true;
        rowLayout.childForceExpandHeight = true;

        foreach (CardHoloTier tier in TierOrder)
        {
            Button button = CreateButton(row.transform, tier + "页签Button", tier.ToString());
            tierButtonImages.Add(button.targetGraphic as Image);
            CardHoloTier capturedTier = tier;
            button.onClick.AddListener(() => SelectTier(capturedTier));
        }
    }

    /// <summary>
    /// 创建可滚动的参数列表。
    /// </summary>
    private void BuildRowsView()
    {
        var scrollObject = new GameObject("参数ScrollView", typeof(RectTransform), typeof(ScrollRect));
        scrollObject.transform.SetParent(panelBody.transform, false);
        LayoutElement scrollElement = scrollObject.AddComponent<LayoutElement>();
        scrollElement.flexibleHeight = 1f;
        scrollElement.minHeight = 200f;

        GameObject viewport = CreateImageObject(scrollObject.transform, "Viewport", new Color(0f, 0f, 0f, 0.18f));
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewport.AddComponent<RectMask2D>();

        var content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        rowRoot = content.GetComponent<RectTransform>();
        rowRoot.anchorMin = new Vector2(0f, 1f);
        rowRoot.anchorMax = new Vector2(1f, 1f);
        rowRoot.pivot = new Vector2(0.5f, 1f);
        rowRoot.anchoredPosition = Vector2.zero;
        rowRoot.sizeDelta = Vector2.zero;

        VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(8, 8, 8, 8);
        contentLayout.spacing = 4f;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scrollRect = scrollObject.GetComponent<ScrollRect>();
        scrollRect.viewport = viewportRect;
        scrollRect.content = rowRoot;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 30f;
    }

    /// <summary>
    /// 创建保存与重置按钮。
    /// </summary>
    private void BuildFooterButtons()
    {
        GameObject row = CreateLayoutObject(panelBody.transform, "操作行", 46f);
        HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 6f;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = true;
        rowLayout.childForceExpandHeight = true;

        CreateButton(row.transform, "保存Button", "保存到资源").onClick.AddListener(SaveProfile);
        CreateButton(row.transform, "重置Button", "重置该档").onClick.AddListener(ResetEditingTier);
    }

    /// <summary>
    /// 创建一行带固定高度的布局对象。
    /// </summary>
    private GameObject CreateLayoutObject(Transform targetParent, string objectName, float height)
    {
        var layoutObject = new GameObject(objectName, typeof(RectTransform), typeof(LayoutElement));
        layoutObject.transform.SetParent(targetParent, false);
        LayoutElement element = layoutObject.GetComponent<LayoutElement>();
        element.minHeight = height;
        element.preferredHeight = height;
        return layoutObject;
    }

    /// <summary>
    /// 创建纯色图片对象。
    /// </summary>
    private GameObject CreateImageObject(Transform targetParent, string objectName, Color color)
    {
        var imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(targetParent, false);
        imageObject.GetComponent<Image>().color = color;
        return imageObject;
    }

    /// <summary>
    /// 创建带文字的按钮。
    /// </summary>
    private Button CreateButton(Transform targetParent, string objectName, string label)
    {
        var buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(targetParent, false);
        Image background = buttonObject.GetComponent<Image>();
        background.color = ButtonColor;
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = background;
        TMP_Text text = CreateText(buttonObject.transform, "Label", label);
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 20f;
        return button;
    }

    /// <summary>
    /// 创建面板文字。
    /// </summary>
    private TMP_Text CreateText(Transform targetParent, string objectName, string value)
    {
        var textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        textObject.transform.SetParent(targetParent, false);
        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.text = value;
        text.font = panelFont;
        text.color = Color.white;
        text.fontSize = 18f;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        return text;
    }

    /// <summary>
    /// 创建一条带轨道、填充和手柄的滑条。
    /// </summary>
    private Slider CreateSlider(Transform targetParent, float minValue, float maxValue, float currentValue, UnityAction<float> onValueChanged)
    {
        var sliderObject = new GameObject("Slider", typeof(RectTransform), typeof(Slider), typeof(LayoutElement));
        sliderObject.transform.SetParent(targetParent, false);
        Slider slider = sliderObject.GetComponent<Slider>();
        slider.minValue = minValue;
        slider.maxValue = maxValue;
        slider.value = currentValue;

        GameObject background = CreateImageObject(sliderObject.transform, "Background", TrackColor);
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0f, 0.5f);
        backgroundRect.anchorMax = new Vector2(1f, 0.5f);
        backgroundRect.anchoredPosition = Vector2.zero;
        backgroundRect.sizeDelta = new Vector2(0f, 8f);

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObject.transform, false);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.5f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.5f);
        fillAreaRect.anchoredPosition = Vector2.zero;
        fillAreaRect.sizeDelta = new Vector2(-16f, 8f);

        GameObject fill = CreateImageObject(fillArea.transform, "Fill", FillColor);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.sizeDelta = Vector2.zero;
        slider.fillRect = fillRect;

        var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(sliderObject.transform, false);
        RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(8f, 0f);
        handleAreaRect.offsetMax = new Vector2(-8f, 0f);

        GameObject handle = CreateImageObject(handleArea.transform, "Handle", HandleColor);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(14f, 0f);
        slider.handleRect = handleRect;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.onValueChanged.AddListener(onValueChanged);
        return slider;
    }
}
